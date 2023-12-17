using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.ToolHelpers.Sliders;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderCompletionator {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.AnySelection)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class SliderCompletionatorView : IQuickRun, ISavable<SliderCompletionatorVm> {
        public event EventHandler RunFinished;

        public static readonly string ToolName = "Slider Completionator";

        public static readonly string ToolDescription = "Change the length and duration of selected sliders and this tool will automatically handle the slider velocity for you." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "Input a value of -1 anywhere to indicate that you want to keep that variable unchanged." +
                                                        Environment.NewLine +
                                                        "For example, 1 duration and -1 length will change the duration to 1 beat while keeping the length the same." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "Check the tooltips for more information about extra features.";

        /// <inheritdoc />
        public SliderCompletionatorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
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
            double endTime = arg.EndTime;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var editorReaderException1);

            if (arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Selected && editorReaderException1 != null) {
                throw new Exception("Could not fetch selected hit objects.", editorReaderException1);
            }

            if (arg.UseCurrentEditorTime) {
                if (editorReaderException1 != null)
                    throw new Exception("Could not fetch current editor time.", editorReaderException1);

                endTime = reader.EditorTime();
            }

            foreach (string path in arg.Paths) {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader, out var selected, out var editorReaderException2);

                if (arg.ImportModeSetting == SliderCompletionatorVm.ImportMode.Selected && editorReaderException2 != null) {
                    throw new Exception("Could not fetch selected hit objects.", editorReaderException2);
                }

                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;

                List<HitObject> markedObjects = arg.ImportModeSetting switch {
                    SliderCompletionatorVm.ImportMode.Selected => selected,
                    SliderCompletionatorVm.ImportMode.Bookmarked => beatmap.GetBookmarkedObjects(),
                    SliderCompletionatorVm.ImportMode.Time => beatmap.QueryTimeCode(arg.TimeCode).ToList(),
                    SliderCompletionatorVm.ImportMode.Everything => beatmap.HitObjects,
                    _ => throw new ArgumentException("Unexpected import mode.")
                };

                for (int i = 0; i < markedObjects.Count; i++) {
                    HitObject ho = markedObjects[i];
                    if (ho.IsSlider) {
                        double mpb = timing.GetMpBAtTime(ho.Time);

                        double oldDuration = timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                        double oldLength = ho.PixelLength;
                        double oldSv = timing.GetSvAtTime(ho.Time);

                        double newDuration = arg.UseEndTime ? endTime == -1 && !arg.UseCurrentEditorTime ? oldDuration : endTime - ho.Time :
                                                              arg.Duration == -1 ? oldDuration : timing.WalkBeatsInMillisecondTime(arg.Duration, ho.Time) - ho.Time;
                        double newLength = arg.Length == -1 ? oldLength : ho.GetSliderPath(fullLength: true).Distance * arg.Length;
                        double newSv = arg.SliderVelocity == -1 ? oldSv : -100 / arg.SliderVelocity;

                        switch (arg.FreeVariableSetting) {
                            case SliderCompletionatorVm.FreeVariable.Velocity:
                                newSv = -10000 * timing.SliderMultiplier * newDuration / (newLength * mpb);
                                break;
                            case SliderCompletionatorVm.FreeVariable.Duration:
                                // This actually doesn't get used anymore because the .osu doesn't store the duration
                                newDuration = newLength * newSv * mpb / (-10000 * timing.SliderMultiplier);
                                break;
                            case SliderCompletionatorVm.FreeVariable.Length:
                                newLength = -10000 * timing.SliderMultiplier * newDuration / (newSv * mpb);
                                break;
                            default:
                                throw new ArgumentException("Unexpected free variable setting.");
                        }

                        if (double.IsNaN(newSv)) {
                            throw new Exception("Encountered NaN slider velocity. Make sure none of the inputs are zero.");
                        }

                        if (newDuration < 0) {
                            throw new Exception("Encountered slider with negative duration. Make sure the end time is greater than the end time of all selected sliders.");
                        }

                        ho.SliderVelocity = newSv;
                        ho.PixelLength = newLength;

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
                    // SliderVelocity changes
                    if (ho.IsSlider) {
                        if (markedObjects.Contains(ho) && arg.DelegateToBpm) {
                            var tpAfter = timing.GetRedlineAtTime(ho.Time).Copy();
                            var tpOn = tpAfter.Copy();

                            tpAfter.Offset = ho.Time;
                            tpOn.Offset = ho.Time - 1;  // This one will be on the slider

                            tpAfter.OmitFirstBarLine = true;
                            tpOn.OmitFirstBarLine = true;

                            // Express velocity in BPM
                            tpOn.MpB *= ho.SliderVelocity / -100;
                            // NaN SV results in removal of slider ticks
                            ho.SliderVelocity = arg.RemoveSliderTicks ? double.NaN : -100;

                            // Add redlines
                            timingPointsChanges.Add(new TimingPointsChange(tpOn, mpb: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));
                            timingPointsChanges.Add(new TimingPointsChange(tpAfter, mpb: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));

                            ho.Time -= 1;
                        }

                        TimingPoint tp = ho.TimingPoint.Copy();
                        tp.Offset = ho.Time;
                        tp.MpB = ho.SliderVelocity;
                        timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, fuzzyness: Precision.DoubleEpsilon));
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
