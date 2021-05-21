using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderCompletionator {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.AnySelection)]
    public partial class SliderCompletionatorView : IQuickRun, ISavable<SliderCompletionatorVm> {
        public event EventHandler RunFinished;

        public static readonly string ToolName = "Slider Completionator";

        public static readonly string ToolDescription = $@"Change the length and duration of marked sliders and this tool will automatically handle the SliderVelocity for you.";

        /// <inheritdoc />
        public SliderCompletionatorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new SliderCompletionatorVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public SliderCompletionatorVm ViewModel => (SliderCompletionatorVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Complete_Sliders((SliderCompletionatorVm) e.Argument, bgw, e);
        }

       
        private void Start_Click(object sender, RoutedEventArgs e) {
            // Get the current beatmap if the selection mode is 'Selected' because otherwise the selection would always fail
            RunTool(SelectionModeBox.SelectedIndex == 0
                ? new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}
                : MainWindow.AppWindow.GetCurrentMaps());
        }

        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }, quick: true);
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

        private string Complete_Sliders(SliderCompletionatorVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int slidersCompleted = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

            if (arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Selected && editorReaderException1 != null) {
                throw new Exception("Could not fetch selected hit objects.", editorReaderException1);
            }

            foreach (string path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Selected && editorReaderException2 != null) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                }

                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;
                List<HitObject> markedObjects = arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Selected ? selected :
                                                arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Bookmarked ? beatmap.GetBookmarkedObjects() :
                                                arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Time ? beatmap.QueryTimeCode(arg.TimeCode).ToList() :
                                                                         beatmap.HitObjects;

                for (int i = 0; i < markedObjects.Count; i++) {
                    HitObject ho = markedObjects[i];
                    if (ho.IsSlider) {
                        double oldSpatialLength = ho.PixelLength;
                        double newSpatialLength = arg.SpatialLength != -1 ? ho.GetSliderPath(fullLength: true).Distance * arg.SpatialLength : oldSpatialLength;

                        double oldTemporalLength = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                        double newTemporalLength = arg.TemporalLength != -1 ? timing.GetMpBAtTime(ho.Time) * arg.TemporalLength : oldTemporalLength;

                        double oldSv = timing.GetSvAtTime(ho.Time);
                        double newSv = oldSv / ((newSpatialLength / oldSpatialLength) / (newTemporalLength / oldTemporalLength));

                        if (double.IsNaN(newSv)) {
                            throw new Exception("Encountered NaN slider velocity. Make sure none of the inputs are zero.");
                        }

                        ho.SliderVelocity = newSv;
                        ho.PixelLength = newSpatialLength;

                        // Scale anchors to completion
                        if (arg.MoveAnchors) {
                            ho.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                                ho.GetAllCurvePoints(), ho.SliderType, ho.PixelLength, out var pathType));
                            ho.SliderType = pathType;
                        }

                        slidersCompleted++;
                    }
                    if (worker != null && worker.WorkerReportsProgress) {
                        worker.ReportProgress(i / markedObjects.Count);
                    }
                }

                // Reconstruct SliderVelocity
                List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
                // Add Hitobject stuff
                foreach (HitObject ho in beatmap.HitObjects) {
                    if (ho.IsSlider) // SliderVelocity changes
                    {
                        TimingPoint tp = ho.TimingPoint.Copy();
                        tp.Offset = ho.Time;
                        tp.MpB = ho.SliderVelocity;
                        timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                    }
                }

                // Add the new SliderVelocity changes
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
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));

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
        public SliderCompletionatorVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(SliderCompletionatorVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "slidercompletionatorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Slider Completionator Projects");
    }
}
