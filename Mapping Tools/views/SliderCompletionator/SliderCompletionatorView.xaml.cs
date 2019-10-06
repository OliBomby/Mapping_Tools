using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.AnySelection)]
    public partial class SliderCompletionatorView : IQuickRun {
        private readonly BackgroundWorker backgroundWorker;
        private bool canRun = true;

        public event EventHandler RunFinished;

        public static readonly string ToolName = "Slider Completionator";

        public static readonly string ToolDescription = $@"Change the length and duration of marked sliders and this tool will automatically handle the SV for you.";

        public SliderCompletionatorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker = (BackgroundWorker) FindResource("backgroundWorker") ;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Complete_Sliders((Arguments) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error != null) {
                //new MessageWindow(ErrorType.Error, eventArg: e);
                MessageBox.Show(e.Error.Message);
            } else {
                if (e.Result.ToString() != "")
                    //new MessageWindow(ErrorType.Success, e.Result.ToString());
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
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmap() }, quick: true);
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!canRun) return;

            IOHelper.SaveMapBackup(paths);

            backgroundWorker.RunWorkerAsync(new Arguments(paths, TemporalBox.GetDouble(), SpatialBox.GetDouble(), SelectionModeBox.SelectedIndex, quick));
            start.IsEnabled = false;
            canRun = false;
        }

        private struct Arguments {
            public string[] Paths;
            public double TemporalLength;
            public double SpatialLength;
            public int SelectionMode;
            public bool Quick;
            public Arguments(string[] paths, double temporal, double spatial, int selectionMode, bool quick)
            {
                Paths = paths;
                TemporalLength = temporal;
                SpatialLength = spatial;
                SelectionMode = selectionMode;
                Quick = quick;
            }
        }

        private string Complete_Sliders(Arguments arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int slidersCompleted = 0;

            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (string path in arg.Paths) {
                var selected = new List<HitObject>();
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, out selected, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;
                List<HitObject> markedObjects = arg.SelectionMode == 0 ? selected :
                                                arg.SelectionMode == 1 ? beatmap.GetBookmarkedObjects() :
                                                                         beatmap.HitObjects;

                for (int i = 0; i < markedObjects.Count; i++) {
                    HitObject ho = markedObjects[i];
                    if (ho.IsSlider) {
                        double oldSpatialLength = ho.PixelLength;
                        double newSpatialLength = arg.SpatialLength != -1 ? ho.GetSliderPath(fullLength: true).Distance * arg.SpatialLength : oldSpatialLength;
                        double oldTemporalLength = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                        double newTemporalLength = arg.TemporalLength != -1 ? timing.GetMpBAtTime(ho.Time) * arg.TemporalLength : oldTemporalLength;
                        double oldSV = timing.GetSVAtTime(ho.Time);
                        double newSV = oldSV / ((newSpatialLength / oldSpatialLength) / (newTemporalLength / oldTemporalLength));
                        ho.SV = newSV;
                        ho.PixelLength = newSpatialLength;
                        slidersCompleted++;
                    }
                    if (worker != null && worker.WorkerReportsProgress) {
                        worker.ReportProgress(i / markedObjects.Count);
                    }
                }

                // Reconstruct SV
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                // Add Hitobject stuff
                foreach (HitObject ho in beatmap.HitObjects) {
                    if (ho.IsSlider) // SV changes
                    {
                        TimingPoint tp = ho.TP.Copy();
                        tp.Offset = ho.Time;
                        tp.MpB = ho.SV;
                        timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                    }
                }

                // Add the new SV changes
                TimingPointsChange.ApplyChanges(timing, timingPointsChanges);

                // Save the file
                editor.SaveFile();
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress)
            {
                worker.ReportProgress(100);
            }

            // Do stuff
            if (arg.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, editorRead));

            // Make an accurate message
            string message = "";
            if (Math.Abs(slidersCompleted) == 1)
            {
                message += "Successfully completed " + slidersCompleted + " slider!";
            }
            else
            {
                message += "Successfully completed " + slidersCompleted + " sliders!";
            }
            return arg.Quick ? "" : message;
        }
    }
}
