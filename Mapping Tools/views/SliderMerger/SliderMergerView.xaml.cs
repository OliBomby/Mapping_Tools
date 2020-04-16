using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderMerger {
    /// <summary>
    ///     Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.MultipleSelection)]
    public partial class SliderMergerView : IQuickRun {
        public static readonly string ToolName = "Slider Merger";

        public static readonly string ToolDescription =
            $@"Merge 2 or more sliders and circles into one big slider.{Environment.NewLine}This program will automatically convert any type of slider into a Beziér slider for the purpose of merging.{Environment.NewLine}Circles can be merged too and will always use the linear connection mode.";

        public SliderMergerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new SliderMergerVM();
        }

        public event EventHandler RunFinished;

        public void QuickRun() {
            RunTool(new[] {IOHelper.GetCurrentBeatmap()}, true);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Merge_Sliders((Arguments) e.Argument, bgw);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            RunTool(SelectionModeBox.SelectedIndex == 0
                ? new[] {IOHelper.GetCurrentBeatmap()}
                : MainWindow.AppWindow.GetCurrentMaps());
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            IOHelper.SaveMapBackup(paths);

            BackgroundWorker.RunWorkerAsync(new Arguments(paths, LeniencyBox.GetDouble(0),
                SelectionModeBox.SelectedIndex, (ConnectionMode) ConnectionModeBox.SelectedItem,
                LinearOnLinearBox.IsChecked.GetValueOrDefault(), MergeOnSliderEndBox.IsChecked.GetValueOrDefault(), quick));
            CanRun = false;
        }

        private string Merge_Sliders(Arguments arg, BackgroundWorker worker) {
            var slidersMerged = 0;

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var path in arg.Paths) {
                var editor = EditorReaderStuff.GetBeatmapEditor(path, reader, editorRead, out var selected, out var editorActuallyRead);

                if (arg.SelectionMode == 0 && !editorActuallyRead) {
                    return EditorReaderStuff.SelectedObjectsReadFailText;
                }

                var beatmap = editor.Beatmap;
                var markedObjects = arg.SelectionMode == 0 ? selected :
                    arg.SelectionMode == 1 ? beatmap.GetBookmarkedObjects() :
                    beatmap.HitObjects;

                var mergeLast = false;
                for (var i = 0; i < markedObjects.Count - 1; i++) {
                    var ho1 = markedObjects[i];
                    var ho2 = markedObjects[i + 1];

                    double dist;
                    if (arg.MergeOnSliderEnd) {
                        var sliderEnd = ho1.GetSliderPath().PositionAt(1);
                        dist = (sliderEnd - ho2.Pos).Length;
                    } else {
                        dist = (ho1.CurvePoints.Last() - ho2.Pos).Length;
                    }

                    if (ho1.IsSlider && ho2.IsSlider && dist <= arg.Leniency) {
                        if (arg.MergeOnSliderEnd) {
                            // In order to merge on the slider end we first move the anchors such that the last anchor is exactly on the slider end
                            // After that merge as usual
                            ho1.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                                ho1.GetAllCurvePoints(), ho1.SliderType, ho1.PixelLength, out var pathType));
                            ho1.SliderType = pathType;
                        }

                        var sp1 = BezierConverter.ConvertToBezier(ho1.SliderPath).ControlPoints;
                        var sp2 = BezierConverter.ConvertToBezier(ho2.SliderPath).ControlPoints;

                        double extraLength = 0;
                        switch (arg.ConnectionMode) {
                            case ConnectionMode.Move:
                                Move(sp2, sp1.Last() - sp2.First());
                                break;
                            case ConnectionMode.Linear:
                                sp1.Add(sp1.Last());
                                sp1.Add(sp2.First());
                                extraLength = (ho1.CurvePoints.Last() - ho2.Pos).Length;
                                break;
                        }

                        var mergedAnchors = sp1.Concat(sp2).ToList();
                        mergedAnchors.Round();

                        var linearLinear = arg.LinearOnLinear && IsLinearBezier(sp1) && IsLinearBezier(sp2);
                        if (linearLinear) {
                            for (var j = 0; j < mergedAnchors.Count - 1; j++) {
                                if (mergedAnchors[j] != mergedAnchors[j + 1]) continue;
                                mergedAnchors.RemoveAt(j);
                                j--;
                            }
                        }

                        var mergedPath = new SliderPath(linearLinear ? PathType.Linear : PathType.Bezier, mergedAnchors.ToArray(),
                            ho1.PixelLength + ho2.PixelLength + extraLength);
                        ho1.SliderPath = mergedPath;

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else if (ho1.IsSlider && ho2.IsCircle && (ho1.CurvePoints.Last() - ho2.Pos).Length <= arg.Leniency) {
                        var sp1 = BezierConverter.ConvertToBezier(ho1.SliderPath).ControlPoints;

                        sp1.Add(sp1.Last());
                        sp1.Add(ho2.Pos);
                        var extraLength = (ho1.CurvePoints.Last() - ho2.Pos).Length;

                        var mergedAnchors = sp1;
                        mergedAnchors.Round();

                        var linearLinear = arg.LinearOnLinear && IsLinearBezier(sp1);
                        if (linearLinear) {
                            for (var j = 0; j < mergedAnchors.Count - 1; j++) {
                                if (mergedAnchors[j] != mergedAnchors[j + 1]) continue;
                                mergedAnchors.RemoveAt(j);
                                j--;
                            }
                        }

                        var mergedPath = new SliderPath(linearLinear ? PathType.Linear : PathType.Bezier, mergedAnchors.ToArray(), ho1.PixelLength + extraLength);
                        ho1.SliderPath = mergedPath;

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else if (ho1.IsCircle && ho2.IsSlider && (ho1.Pos - ho2.Pos).Length <= arg.Leniency) {
                        var sp2 = BezierConverter.ConvertToBezier(ho2.SliderPath).ControlPoints;

                        sp2.Insert(0, sp2.First());
                        sp2.Insert(0, ho1.Pos);
                        var extraLength = (ho1.Pos - ho2.Pos).Length;

                        var mergedAnchors = sp2;
                        mergedAnchors.Round();

                        var linearLinear = arg.LinearOnLinear && IsLinearBezier(sp2);
                        if (linearLinear) {
                            for (var j = 0; j < mergedAnchors.Count - 1; j++) {
                                if (mergedAnchors[j] != mergedAnchors[j + 1]) continue;
                                mergedAnchors.RemoveAt(j);
                                j--;
                            }
                        }

                        var mergedPath = new SliderPath(linearLinear ? PathType.Linear : PathType.Bezier, mergedAnchors.ToArray(), ho2.PixelLength + extraLength);
                        ho2.SliderPath = mergedPath;

                        beatmap.HitObjects.Remove(ho1);
                        markedObjects.Remove(ho1);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else if (ho1.IsCircle && ho2.IsCircle && (ho1.Pos - ho2.Pos).Length <= arg.Leniency) {
                        var mergedAnchors = new List<Vector2> {ho1.Pos, ho2.Pos};

                        var mergedPath = new SliderPath(arg.LinearOnLinear ? PathType.Linear : PathType.Bezier, mergedAnchors.ToArray(), (ho1.Pos - ho2.Pos).Length);
                        ho1.SliderPath = mergedPath;
                        ho1.IsCircle = false;
                        ho1.IsSlider = true;
                        ho1.Repeat = 1;
                        ho1.EdgeHitsounds = new List<int> {ho1.GetHitsounds(), ho2.GetHitsounds()};
                        ho1.EdgeSampleSets = new List<SampleSet> {ho1.SampleSet, ho2.SampleSet};
                        ho1.EdgeAdditionSets = new List<SampleSet> {ho1.AdditionSet, ho2.AdditionSet};

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    }
                    else {
                        mergeLast = false;
                    }

                    if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(i / markedObjects.Count);
                }

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            if (arg.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, editorRead));

            // Make an accurate message
            var message = "";
            if (Math.Abs(slidersMerged) == 1)
                message += "Successfully merged " + slidersMerged + " slider!";
            else
                message += "Successfully merged " + slidersMerged + " sliders!";
            return arg.Quick ? "" : message;
        }

        public static bool IsLinearBezier(List<Vector2> points) {
            // Every point at not the endpoints must have an anchor before or after it at the same position
            for (var i = 1; i < points.Count-1; i++) {
                if (points[i] != points[i - 1] && points[i] != points[i + 1]) {
                    return false;
                }
            }

            return true;
        }

        public static void Move(List<Vector2> points,  Vector2 delta) {
            for (var i = 0; i < points.Count; i++) {
                points[i] = points[i] + delta;
            }
        }

        private struct Arguments {
            public readonly string[] Paths;
            public readonly double Leniency;
            public readonly int SelectionMode;
            public ConnectionMode ConnectionMode;
            public bool LinearOnLinear;
            public bool MergeOnSliderEnd;
            public readonly bool Quick;

            public Arguments(string[] paths, double leniency, int selectionMode, ConnectionMode connectionMode,
                bool linearOnLinear, bool mergeOnSliderEnd, bool quick) {
                Paths = paths;
                Leniency = leniency;
                SelectionMode = selectionMode;
                ConnectionMode = connectionMode;
                LinearOnLinear = linearOnLinear;
                MergeOnSliderEnd = mergeOnSliderEnd;
                Quick = quick;
            }
        }
    }
}