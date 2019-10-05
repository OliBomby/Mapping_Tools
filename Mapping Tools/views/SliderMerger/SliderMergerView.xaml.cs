using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views {
    /// <summary>
    ///     Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.MultipleSelection)]
    public partial class SliderMergerView : IQuickRun {
        public static readonly string ToolName = "Slider Merger";

        public static readonly string ToolDescription =
            $@"Merge 2 or more sliders into one big slider. The pixel length of the resulting slider is the sum of the pixel lengths of the sliders that made it up.{Environment.NewLine}This program will automatically convert any type of slider into a Beziér slider for the purpose of merging.{Environment.NewLine}In order for 2 sliders to merge, place the second slider on top of the last anchor of the first slider.";

        private readonly BackgroundWorker backgroundWorker;
        private bool canRun = true;

        public SliderMergerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker");
            DataContext = new SliderMergerVM();
        }

        public event EventHandler RunFinished;

        public void QuickRun() {
            RunTool(new[] {IOHelper.GetCurrentBeatmap()}, true);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Merge_Sliders((Arguments) e.Argument, bgw);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                MessageBox.Show($"{e.Error.Message}{Environment.NewLine}{e.Error.StackTrace}",
                    "Error");
            } else {
                if (e.Result.ToString() != "")
                    MessageBox.Show(e.Result.ToString());
                progress.Value = 0;
            }

            start.IsEnabled = true;
            canRun = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps());
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!canRun) return;

            IOHelper.SaveMapBackup(paths);

            backgroundWorker.RunWorkerAsync(new Arguments(paths, LeniencyBox.GetDouble(0),
                SelectionModeBox.SelectedIndex, (ConnectionMode) ConnectionModeBox.SelectedItem,
                LinearOnLinearBox.IsChecked.GetValueOrDefault(), quick));
            start.IsEnabled = false;
            canRun = false;
        }

        private string Merge_Sliders(Arguments arg, BackgroundWorker worker) {
            var slidersMerged = 0;

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var path in arg.Paths) {
                var selected = new List<HitObject>();
                var editor = editorRead
                    ? EditorReaderStuff.GetNewestVersion(path, out selected, reader)
                    : new BeatmapEditor(path);
                var beatmap = editor.Beatmap;
                var markedObjects = arg.SelectionMode == 0 ? selected :
                    arg.SelectionMode == 1 ? beatmap.GetBookmarkedObjects() :
                    beatmap.HitObjects;

                var mergeLast = false;
                for (var i = 0; i < markedObjects.Count - 1; i++) {
                    var ho1 = markedObjects[i];
                    var ho2 = markedObjects[i + 1];
                    if (ho1.IsSlider && ho2.IsSlider && (ho1.CurvePoints.Last() - ho2.Pos).Length <= arg.Leniency) {
                        var sp1 = BezierConverter.ConvertToBezier(ho1.SliderPath).ControlPoints;
                        var sp2 = BezierConverter.ConvertToBezier(ho2.SliderPath).ControlPoints;

                        switch (arg.ConnectionMode) {
                            case ConnectionMode.Move:
                                Move(sp2, sp1.Last() - sp2.First());
                                break;
                            case ConnectionMode.Linear:
                                sp1.Add(sp1.Last());
                                sp1.Add(sp2.First());
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
                            ho1.PixelLength + ho2.PixelLength);
                        ho1.SliderPath = mergedPath;

                        beatmap.HitObjects.Remove(ho2);
                        markedObjects.Remove(ho2);
                        i--;

                        slidersMerged++;
                        if (!mergeLast) slidersMerged++;
                        mergeLast = true;
                    } else {
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
            public readonly bool Quick;

            public Arguments(string[] paths, double leniency, int selectionMode, ConnectionMode connectionMode,
                bool linearOnLinear, bool quick) {
                Paths = paths;
                Leniency = leniency;
                SelectionMode = selectionMode;
                ConnectionMode = connectionMode;
                LinearOnLinear = linearOnLinear;
                Quick = quick;
            }
        }
    }
}