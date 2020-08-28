using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.TimeLine;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.AutoFailDetector {
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class AutoFailDetectorView : IQuickRun {
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
        public static readonly string ToolDescription = $"Detects cases of incorrect object loading in a beatmap which makes osu! unable to calculate scores correctly.{Environment.NewLine} Auto-fail is most often caused by placing other hit objects during sliders, so there are multiple hit objects going on at the same time also known as \"2B\" patterns.{Environment.NewLine} Use the AR and OD override options to see what would happen when you use hardrock mod on the map.";

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
            _unloadingObjects = new List<double>();
            _potentialUnloadingObjects = new List<double>();
            _potentialDisruptors = new List<double>();

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);
            var beatmap = editor.Beatmap;

            // Hit objects sorted by start time
            var hitObjects = beatmap.HitObjects;
            hitObjects = hitObjects.OrderBy(ho => ho.Time).ToList();

            // Get approach time and radius of the 50 score hit window
            var ar = args.ApproachRateOverride == -1
                ? editor.Beatmap.Difficulty["ApproachRate"].DoubleValue
                : args.ApproachRateOverride;
            var approachTime = (int) Beatmap.ApproachRateToMs(ar);

            var od = args.OverallDifficultyOverride == -1
                ? editor.Beatmap.Difficulty["OverallDifficulty"].DoubleValue
                : args.OverallDifficultyOverride;
            var window50 = (int) Math.Ceiling(200 - 10 * od);

            // Set end time for the timeline
            var endTime = (int) hitObjects.Max(ho => ho.EndTime);
            _endTimeMonitor = endTime;

            // Get times to check
            SortedSet<int> allTimesToCheck = new SortedSet<int>(hitObjects.Select(ho => (int) ho.EndTime + approachTime));

            // Find all problematic areas which could cause auto-fail depending on the binary search
            // A problem area consists of one object and the objects which can unload it
            // An object B can unload another object A if it has a later index than A and an end time earlier than A's end time - approach time.
            var problemAreas = new List<ProblemArea>();
            for (int i = 0; i < hitObjects.Count; i++) {
                var ho = hitObjects[i];
                var adjEndTime = GetAdjustedEndTime(ho, window50, args.PhysicsUpdateLeniency);

                // Ignore all problem areas which are contained by another unloadable object,
                // because fixing the outer problem area will also fix all of the problems inside
                if (problemAreas.Count > 0 && adjEndTime <=
                    GetAdjustedEndTime(problemAreas.Last().unloadableHitObject, window50, args.PhysicsUpdateLeniency)) {
                    continue;
                }

                // Check all later objects for any which have an early enough end time
                var disruptors = new HashSet<HitObject>();
                for (int j = i + 1; j < hitObjects.Count; j++) {
                    var ho2 = hitObjects[j];
                    if (ho2.EndTime < adjEndTime - approachTime) {
                        disruptors.Add(ho2);

                        if (args.ShowPotentialDisruptors) {
                            _potentialDisruptors.Add(ho2.Time);
                        }
                    }
                }

                if (disruptors.Count == 0)
                    continue;

                var timesToCheck = new HashSet<int>(allTimesToCheck.GetViewBetween((int) ho.Time, adjEndTime));

                // A problem area can also be ignored if the times-to-check is a subset of the last times-to-check,
                // because if thats the case that implies this problem is contained in the last.
                if (!(problemAreas.Count > 0 && timesToCheck.IsSubsetOf(problemAreas.Last().timesToCheck))) {
                    problemAreas.Add(new ProblemArea {index = i, unloadableHitObject = ho, disruptors = disruptors, timesToCheck = timesToCheck});

                    if (args.ShowPotentialUnloadingObjects) {
                        _potentialUnloadingObjects.Add(ho.Time);
                    }
                }
            }

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(33);

            // Make a solution
            if (args.GetAutoFailFix) {

                int[] solution = SolveAutoFailPadding(hitObjects, problemAreas, approachTime);
                int paddingCount = solution.Sum();
                bool acceptedSolution = false;
                int solutionCount = 0;

                foreach (var sol in SolveAutoFailPaddingEnumerableInfinite(hitObjects, problemAreas, approachTime, paddingCount)) {
                    solution = sol;

                    StringBuilder guideBuilder = new StringBuilder();
                    AddFixGuide(guideBuilder, sol, problemAreas, approachTime, window50, args.PhysicsUpdateLeniency);
                    guideBuilder.AppendLine("\nDo you want to use this solution?");

                    var result = MessageBox.Show(guideBuilder.ToString(), $"Solution {++solutionCount}", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes) {
                        acceptedSolution = true;
                        break;
                    }
                    if (result == MessageBoxResult.Cancel) {
                        break;
                    }
                }


                if (args.AutoPlaceFix && acceptedSolution) {
                    int lastTime = 0;
                    for (int i = 0; i < problemAreas.Count; i++) {
                        if (solution[i] > 0) {
                            var t = GetSafePlacementTime(hitObjects, lastTime, problemAreas[i].GetStartTime(),
                                approachTime,
                                window50, args.PhysicsUpdateLeniency);
                            for (int j = 0; j < solution[i]; j++) {
                                beatmap.HitObjects.Add(new HitObject
                                    {Pos = Vector2.Zero, Time = t, ObjectType = 8, EndTime = t - 1});
                            }
                        }

                        lastTime = problemAreas[i].GetEndTime(approachTime, window50, args.PhysicsUpdateLeniency);
                    }

                    if (solution.Last() > 0) {
                        var t = GetSafePlacementTime(hitObjects, lastTime, endTime, approachTime, window50,
                            args.PhysicsUpdateLeniency);
                        for (int i = 0; i < solution.Last(); i++) {
                            beatmap.HitObjects.Add(new HitObject
                                {Pos = Vector2.Zero, Time = t, ObjectType = 8, EndTime = t - 1});
                        }
                    }
                }

                if (args.AutoPlaceFix) {
                    editor.SaveFile();
                    hitObjects = beatmap.HitObjects.OrderBy(ho => ho.Time).ToList();
                }
            }

            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(67);

            int autoFails = 0;
            // Use osu!'s object loading algorithm to find out which objects are actually unloaded
            foreach (var problemArea in problemAreas) {
                foreach (var time in problemArea.timesToCheck) {
                    var minimalLeft = time - approachTime;
                    var minimalRight = time + approachTime;

                    var startIndex = OsuBinarySearch(hitObjects, minimalLeft);
                    var endIndex = hitObjects.FindIndex(startIndex, ho => ho.Time > minimalRight);
                    if (endIndex < 0) {
                        endIndex = hitObjects.Count - 1;
                    }

                    var hitObjectsMinimal = hitObjects.GetRange(startIndex, 1 + endIndex - startIndex);

                    if (!hitObjectsMinimal.Contains(problemArea.unloadableHitObject)) {
                        if (args.ShowUnloadingObjects)
                            _unloadingObjects.Add(problemArea.unloadableHitObject.Time);
                        autoFails++;
                        break;
                    }
                }
            }

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            if (args.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, false));

            return autoFails > 0 ? $"{autoFails} unloading objects detected and {problemAreas.Count} potential unloading objects detected!" : 
                problemAreas.Count > 0 ? $"{problemAreas.Count} potential unloading objects detected." : 
                "No auto-fail detected.";
        }

        private static int GetSafePlacementTime(List<HitObject> hitObjects, int start, int end, int approachTime, int window50, int physicsUpdateLeniency) {
            var rangeObjects = hitObjects.FindAll(o => o.EndTime >= start && o.Time <= end);

            for (int i = end - 1; i >= start; i--) {
                if (!rangeObjects.Any(ho =>
                    i >= (int) ho.Time &&
                    i <= GetAdjustedEndTime(ho, window50, physicsUpdateLeniency) - approachTime)) {
                    return i;
                }
            }

            throw new Exception($"Can't find a safe place to place objects between {start} and {end}.");
        }

        private static int GetAdjustedEndTime(HitObject ho, int window50, int physicsUpdateTime) {
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
                var t = (int) hitObjects[mid].EndTime;

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
                    _tl.AddElement(timingS, 3);
                }
                tl_host.Children.Clear();
                tl_host.Children.Add(_tl);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private class ProblemArea {
            public int index;
            public HitObject unloadableHitObject;
            public HashSet<HitObject> disruptors;
            public HashSet<int> timesToCheck;

            public int GetStartTime() {
                return (int) unloadableHitObject.Time;
            }

            public int GetEndTime(int approachTime, int window50, int physicsTime) {
                return GetAdjustedEndTime(unloadableHitObject, window50, physicsTime) - approachTime;
            }

            public int GetTimeToCheck(int window50, int physicsTime) {
                return GetAdjustedEndTime(unloadableHitObject, window50, physicsTime);
            }
        }

        private static void AddFixGuide(StringBuilder guideBuilder, IReadOnlyList<int> paddingSolution, IReadOnlyList<ProblemArea> problemAreas, int approachTime, int window50, int physicsTime) {
            guideBuilder.AppendLine("Auto-fail fix guide. Place these extra objects to fix auto-fail:\n");
            int lastTime = 0;
            for (int i = 0; i < problemAreas.Count; i++) {
                guideBuilder.AppendLine(i == 0
                    ? $"Extra objects before {problemAreas[i].GetStartTime()}: {paddingSolution[i]}"
                    : $"Extra objects between {lastTime} - {problemAreas[i].GetStartTime()}: {paddingSolution[i]}");
                lastTime = problemAreas[i].GetEndTime(approachTime, window50, physicsTime);
            }
            guideBuilder.AppendLine($"Extra objects after {lastTime}: {paddingSolution.Last()}");
        }

        private static int[] SolveAutoFailPadding(IReadOnlyList<HitObject> hitObjects, IReadOnlyList<ProblemArea> problemAreas, int approachTime, int startPaddingCount = 0) {
            int padding = startPaddingCount;
            int[] solution;
            while (!SolveAutoFailPadding(hitObjects, problemAreas, approachTime, padding++, out solution)) { }

            return solution;
        }

        private static bool SolveAutoFailPadding(IReadOnlyList<HitObject> hitObjects, IReadOnlyList<ProblemArea> problemAreas, int approachTime, int paddingCount, out int[] solution) {
            solution = new int[problemAreas.Count + 1];

            int leftPadding = 0;
            for (var i = 0; i < problemAreas.Count; i++) {
                var problemAreaSolution =
                    SolveSingleProblemAreaPadding(problemAreas[i], hitObjects, approachTime, paddingCount, leftPadding);

                if (problemAreaSolution.Count == 0 || problemAreaSolution.Max() < leftPadding) {
                    return false;
                }

                var lowest = problemAreaSolution.First(o => o >= leftPadding);
                solution[i] = lowest - leftPadding;
                leftPadding = lowest;
            }

            solution[problemAreas.Count] = paddingCount - leftPadding;

            return true;
        }

        private static IEnumerable<int[]> SolveAutoFailPaddingEnumerableInfinite(IReadOnlyList<HitObject> hitObjects,
            IReadOnlyList<ProblemArea> problemAreas, int approachTime, int initialPaddingCount) {
            int paddingCount = initialPaddingCount;
            while (true) {
                foreach (var solution in SolveAutoFailPaddingEnumerable(hitObjects, problemAreas, approachTime, paddingCount)) {
                    yield return solution;
                }

                paddingCount++;
            }
        }

        private static IEnumerable<int[]> SolveAutoFailPaddingEnumerable(IReadOnlyList<HitObject> hitObjects, 
            IReadOnlyList<ProblemArea> problemAreas, int approachTime, int paddingCount) {
            List<int>[] allSolutions = new List<int>[problemAreas.Count];

            int minimalLeft = 0;
            for (var i = 0; i < problemAreas.Count; i++) {
                var problemAreaSolution =
                    SolveSingleProblemAreaPadding(problemAreas[i], hitObjects, approachTime, paddingCount, minimalLeft);

                if (problemAreaSolution.Count == 0 || problemAreaSolution.Last() < minimalLeft) {
                    yield break;
                }

                allSolutions[i] = problemAreaSolution;
                minimalLeft = problemAreaSolution.First();
            }

            // Remove impossible max padding
            int maximalLeft = paddingCount;
            for (int i = allSolutions.Length - 1; i >= 0; i--) {
                allSolutions[i].RemoveAll(o => o > maximalLeft);
                maximalLeft = allSolutions[i].Last();
            }

            foreach (var leftPadding in EnumerateSolutions(allSolutions)) {
                int[] pads = new int[leftPadding.Length + 1];
                int left = 0;
                for (int i = 0; i < leftPadding.Length; i++) {
                    pads[i] = leftPadding[i] - left;
                    left = leftPadding[i];
                }

                pads[pads.Length - 1] = paddingCount - left;
                yield return pads;
            }
        }

        private static IEnumerable<int[]> EnumerateSolutions(IReadOnlyList<List<int>> allSolutions, int depth = 0, int minimum = 0) {
            if (depth == allSolutions.Count - 1) {
                foreach (var i in allSolutions[depth].Where(o => o >= minimum)) {
                    var s = new int[allSolutions.Count];
                    s[depth] = i;
                    yield return s;
                }
                yield break;
            }
            foreach (var i in allSolutions[depth].Where(o => o >= minimum)) {
                foreach (var j in EnumerateSolutions(allSolutions, depth + 1, minimum = i)) {
                    j[depth] = i;
                    yield return j;
                }
            }
        }

        private static List<int> SolveSingleProblemAreaPadding(ProblemArea problemArea, IReadOnlyList<HitObject> hitObjects, int approachTime, int paddingCount, int minimalLeft = 0) {
            var solution = new List<int>(paddingCount - minimalLeft + 1);

            for (int left = minimalLeft; left <= paddingCount; left++) {
                var right = paddingCount - left;

                if (ProblemAreaPaddingWorks(problemArea, hitObjects, approachTime, left, right)) {
                    solution.Add(left);
                }
            }

            return solution;
        }

        private static bool ProblemAreaPaddingWorks(ProblemArea problemArea, IReadOnlyList<HitObject> hitObjects, int approachTime, int left, int right) {
            return problemArea.timesToCheck.All(t =>
                PaddedOsuBinarySearch(hitObjects, t - approachTime, left, right) <= problemArea.index);
        }

        private static int PaddedOsuBinarySearch(IReadOnlyList<HitObject> hitObjects, int time, int left, int right) {
            var n = hitObjects.Count;
            var min = -left;
            var max = n - 1 + right;
            while (min <= max) {
                var mid = min + (max - min) / 2;
                var t = mid < 0 ? int.MinValue : mid > hitObjects.Count - 1 ? int.MaxValue : (int) hitObjects[mid].EndTime;

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
    }
}
