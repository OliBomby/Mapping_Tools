using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.TimeLine;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.AutoFailDetector {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class AutoFailDetectorView : IQuickRun {
        private List<double> _autoFailTimes;
        private List<double> _autoFailingObjects;
        private double _endTimeMonitor;
        private TimeLine _tl;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler RunFinished;

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolName = "Auto-fail Detector";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string ToolDescription = $"Detects cases of incorrect object loading in a beatmap which makes osu! unable to calculate scores correctly.{Environment.NewLine} Auto-fail is most often caused by placing other hit objects during sliders, so there are multiple hit objects going on at the same time also known as \"2B\" patterns.";

        /// <summary>
        /// Initializes the Map Cleaner view to <see cref="MainWindow"/>
        /// </summary>
        public AutoFailDetectorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new AutoFailDetectorVm();

            // It's important to see the results
            Verbose = true;
        }

        public AutoFailDetectorVm ViewModel => (AutoFailDetectorVm) DataContext;

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        /// <summary>
        /// 
        /// </summary>
        public void QuickRun() {
            RunTool(new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }, quick: true);
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            //BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Run_Program((AutoFailDetectorVm) e.Argument, bgw, e);
        }

        protected override void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Error == null) {
                FillTimeLine();
            }
            base.BackgroundWorker_RunWorkerCompleted(sender, e);
        }

        private string Run_Program(AutoFailDetectorVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            // Reset the timeline lists
            _autoFailTimes = new List<double>();
            _autoFailingObjects = new List<double>();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);

            // Hit objects sorted by start time
            var hitObjects = editor.Beatmap.HitObjects;
            hitObjects = hitObjects.OrderBy(ho => ho.Time).ToList();

            var ar = args.ApproachRateOverride == -1
                ? editor.Beatmap.Difficulty["ApproachRate"].DoubleValue
                : args.ApproachRateOverride;
            var approachTime = (int) Beatmap.ApproachRateToMs(ar);

            var od = args.OverallDifficultyOverride == -1
                ? editor.Beatmap.Difficulty["OverallDifficulty"].DoubleValue
                : args.OverallDifficultyOverride;
            var window50 = (int) Math.Ceiling(200 - 10 * od);

            var endTime = (int) hitObjects.Max(ho => ho.EndTime) + args.PhysicsUpdateLeniency;
            _endTimeMonitor = endTime;

            SortedSet<int> timesToCheck = new SortedSet<int>(hitObjects.Select(ho => (int)ho.EndTime + approachTime)
                .Concat(hitObjects.Select(ho => (int)ho.EndTime + approachTime + 1))
                .Concat(hitObjects.Select(ho => (int)ho.EndTime + approachTime - 1)));
            //var timesToCheck = Enumerable.Range(0, endTime);

            bool autoFail = false;
            List<HitObject> lastHitObjects = new List<HitObject>();

            foreach (var time in timesToCheck) {
                var minimalLeft = time - approachTime;
                var minimalRight = time + approachTime;

                var startIndex = OsuBinarySearch(hitObjects, minimalLeft);
                var endIndex = hitObjects.FindIndex(startIndex, ho => ho.Time > minimalRight);
                if (endIndex < 0) {
                    endIndex = hitObjects.Count - 1;
                }

                var hitObjectsMinimal = hitObjects.GetRange(startIndex, 1 + endIndex - startIndex);

                var removedObjects = lastHitObjects.Except(hitObjectsMinimal);

                var badUnload = removedObjects.FirstOrDefault(ho => GetTrueEndTime(ho, window50, args.PhysicsUpdateLeniency) > time);
                if (badUnload != null) {
                    autoFail = true;
                    if (args.ShowAutoFailTimes)
                        _autoFailTimes.Add(time);
                    if (args.ShowUnloadingObjects)
                        _autoFailingObjects.Add(badUnload.Time);
                }

                lastHitObjects = hitObjectsMinimal;
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            if (args.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, false));

            return autoFail ? $"{Math.Max(_autoFailTimes.Count, _autoFailingObjects.Count)} cases of auto-fail detected!" : "No autofail detected.";
        }

        private static int GetTrueEndTime(HitObject ho, int window50, int physicsUpdateTime) {
            if (ho.IsCircle) {
                return (int) ho.Time + window50 + physicsUpdateTime;
            }
            if (ho.IsSlider || ho.IsSpinner) {
                return (int) ho.EndTime + physicsUpdateTime;
            }

            return (int) Math.Max(ho.Time + window50 + physicsUpdateTime, ho.EndTime + physicsUpdateTime);
        }

        private static int OsuBinarySearch(IReadOnlyList<HitObject> hitObjects, int time) {
            var n = hitObjects.Count;
            var min = 0;
            var max = n - 1;
            while (min <= max) {
                var mid = min + (max - min) / 2;
                var t = hitObjects[mid].EndTime;

                if (time == t) {
                    return mid;
                }
                if (time > t) {
                    min = mid + 1;
                } else {
                    max = mid - 1;
                }
            }

            return min;
        }

        private void FillTimeLine() {
            _tl?.mainCanvas.Children.Clear();
            try {
                _tl = new TimeLine(MainWindow.AppWindow.ActualWidth, 100.0, _endTimeMonitor);
                foreach (double timingS in _autoFailingObjects) {
                    _tl.AddElement(timingS, 1);
                }
                foreach (double timingS in _autoFailTimes) {
                    _tl.AddElement(timingS, 3);
                }
                tl_host.Children.Clear();
                tl_host.Children.Add(_tl);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
