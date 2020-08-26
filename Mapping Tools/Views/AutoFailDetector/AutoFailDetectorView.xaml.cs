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
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Views.AutoFailDetector {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class AutoFailDetectorView : IQuickRun {
        private List<double> _autoFailTimes;
        private List<double> _unloadingObjects;
        private List<double> _potentialUnloadingObjects;
        private List<double> _potentialDisruptors;
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
            _unloadingObjects = new List<double>();
            _potentialUnloadingObjects = new List<double>();
            _potentialDisruptors = new List<double>();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);
            var beatmap = editor.Beatmap;

            //var indices = new[] {0, 2, 6, 13, 27, 55, 111, 223};
            /*foreach (var index in indices) {
                beatmap.HitObjects.Insert(index, new HitObject {Pos = Vector2.Zero, Time = beatmap.HitObjects[index].Time - 1, ObjectType = 8, EndTime = 138797});
            }

            editor.SaveFile();
            return "";*/

            // Hit objects sorted by start time
            var hitObjects = beatmap.HitObjects;
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

            // Find all problematic areas which could cause auto-fail depending on the binary search
            var problemAreas = new List<ProblemArea>();
            for (int i = 0; i < hitObjects.Count; i++) {
                var ho = hitObjects[i];
                var disruptors = new List<HitObject>();
                for (int j = i + 1; j < hitObjects.Count; j++) {
                    var ho2 = hitObjects[j];
                    if (ho2.EndTime < GetTrueEndTime(ho, window50, args.PhysicsUpdateLeniency) - approachTime) {
                        disruptors.Add(ho2);

                        if (args.ShowPotentialDisruptors) {
                            _potentialDisruptors.Add(ho2.Time);
                        }
                    }
                }

                if (disruptors.Count > 0) {
                    problemAreas.Add(new ProblemArea {unloadableHitObject = ho, disruptors = disruptors});

                    if (args.ShowPotentialUnloadingObjects) {
                        _potentialUnloadingObjects.Add(ho.Time);
                    }
                }
            }

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(50);

            var allDisruptors = problemAreas.SelectMany(o => o.disruptors).ToArray();
            SortedSet<int> timesToCheck = new SortedSet<int>(allDisruptors.Select(ho => (int)ho.EndTime + approachTime)
                .Concat(allDisruptors.Select(ho => (int)ho.EndTime + approachTime + 1))
                .Concat(allDisruptors.Select(ho => (int)ho.EndTime + approachTime - 1)));
            //var timesToCheck = Enumerable.Range(0, endTime);

            int autoFails = 0;
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
                    autoFails++;
                    if (args.ShowAutoFailTimes)
                        _autoFailTimes.Add(time);
                    if (args.ShowUnloadingObjects)
                        _unloadingObjects.Add(badUnload.Time);
                }

                lastHitObjects = hitObjectsMinimal;
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            if (args.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, false));

            return autoFails > 0 ? $"{autoFails} cases of auto-fail detected and {problemAreas.Count} potential unloading objects detected!" : problemAreas.Count > 0 ? $"{problemAreas.Count} potential unloading objects detected." : "No auto-fail detected.";
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
                //Console.WriteLine($"index {mid}");
                //Console.WriteLine($"time {hitObjects[mid].Time}");
                //Console.WriteLine($"end time {hitObjects[mid].EndTime}");
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
                foreach (double timingS in _potentialUnloadingObjects) {
                    _tl.AddElement(timingS, 1);
                }
                foreach (double timingS in _potentialDisruptors) {
                    _tl.AddElement(timingS, 4);
                }
                foreach (double timingS in _unloadingObjects) {
                    _tl.AddElement(timingS, 2);
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

        private class ProblemArea {
            public HitObject unloadableHitObject;
            public List<HitObject> disruptors;

            public int GetStartTime() {
                return (int) unloadableHitObject.Time;
            }

            public int GetEndTime() {
                return (int) unloadableHitObject.EndTime;
            }
        }
    }
}
