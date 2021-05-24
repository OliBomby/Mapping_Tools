using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderMerger {
    /// <summary>
    ///     Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.MultipleSelection)]
    public partial class SliderMergerView : IQuickRun, ISavable<SliderMergerVm> {
        public static readonly string ToolName = "Slider Merger";

        public static readonly string ToolDescription =
            $@"Merge 2 or more sliders and circles into one big slider.{Environment.NewLine}This program will automatically convert any type of slider into a Beziér slider for the purpose of merging.{Environment.NewLine}Circles can be merged too and will always use the linear connection mode.";

        public SliderMergerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new SliderMergerVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public SliderMergerVm ViewModel => (SliderMergerVm) DataContext;

        public event EventHandler RunFinished;

        public void QuickRun() {
            RunTool(new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}, true);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Merge_Sliders((SliderMergerVm) e.Argument, bgw);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            RunTool(SelectionModeBox.SelectedIndex == 0
                ? new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}
                : MainWindow.AppWindow.GetCurrentMaps());
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        private string Merge_Sliders(SliderMergerVm arg, BackgroundWorker worker) {
            var slidersMerged = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

            if (arg.ImportModeSetting == 0 && editorReaderException1 != null) {
                throw new Exception("Could not fetch selected hit objects.", editorReaderException1);
            }

            foreach (var path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (arg.ImportModeSetting == SliderMergerVm.ImportMode.Selected && editorReaderException2 != null) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                }

                var beatmap = editor.Beatmap;
                var markedObjects = arg.ImportModeSetting == 0 ? selected :
                    arg.ImportModeSetting == SliderMergerVm.ImportMode.Bookmarked ? beatmap.GetBookmarkedObjects() :
                    arg.ImportModeSetting == SliderMergerVm.ImportMode.Time ? beatmap.QueryTimeCode(arg.TimeCode).ToList() :
                    beatmap.HitObjects;

                var mergeLast = false;
                for (var i = 0; i < markedObjects.Count - 1; i++) {
                    if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(i / markedObjects.Count);

                    var ho1 = markedObjects[i];
                    var ho2 = markedObjects[i + 1];

                    var lastPos1 = ho1.IsSlider
                        ? arg.MergeOnSliderEnd ? ho1.GetSliderPath().PositionAt(1) : ho1.CurvePoints.Last()
                        : ho1.Pos;

                    double dist = Vector2.Distance(lastPos1, ho2.Pos);

                    if (dist > arg.Leniency) {
                        mergeLast = false;
                        continue;
                    }

                    if (ho1.IsSlider && ho2.IsSlider) {
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
                        switch (arg.ConnectionModeSetting) {
                            case SliderMergerVm.ConnectionMode.Move:
                                Move(sp2, sp1.Last() - sp2.First());
                                break;
                            case SliderMergerVm.ConnectionMode.Linear:
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
                    } else if (ho1.IsSlider && ho2.IsCircle) {
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
                    } else if (ho1.IsCircle && ho2.IsSlider) {
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
                    } else if (ho1.IsCircle && ho2.IsCircle) {
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
                }

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));

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
        public SliderMergerVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(SliderMergerVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "slidermergerproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Slider Merger Projects");
    }
}