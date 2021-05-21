using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Mapping_Tools.Classes.Tools {
    public class AutoFailDetector {
        private class ProblemArea {
            public int index;
            public HitObject unloadableHitObject;
            public HashSet<HitObject> disruptors;
            public HashSet<int> timesToCheck;

            public int GetStartTime() {
                return (int)unloadableHitObject.Time;
            }

            public int GetEndTime() {
                return (int)unloadableHitObject.EndTime;
            }
        }

        private const int maxPaddingCount = 2000;

        private readonly int mapStartTime;
        private readonly int mapEndTime;
        private readonly int autoFailCheckTime;
        private readonly int approachTime;
        private readonly int window50;
        private readonly int physicsTime;
        private List<HitObject> hitObjects;
        private List<ProblemArea> problemAreas;

        private SortedSet<int> timesToCheckStartIndex;
        private int?[] placementTimes;

        public List<double> UnloadingObjects;
        public List<double> PotentialUnloadingObjects;
        public List<double> Disruptors;

        public AutoFailDetector(List<HitObject> hitObjects, int mapStartTime, int mapEndTime, int autoFailCheckTime, int approachTime, int window50, int physicsTime) {
            // Sort the hitobjects
            SetHitObjects(hitObjects);

            this.mapStartTime = mapStartTime;
            this.mapEndTime = mapEndTime;
            this.autoFailCheckTime = autoFailCheckTime;
            this.approachTime = approachTime;
            this.window50 = window50;
            this.physicsTime = physicsTime;
        }

        private void SortHitObjects() {
            hitObjects.Sort();
        }

        public void SetHitObjects(List<HitObject> hitObjects2) {
            hitObjects = hitObjects2;
            SortHitObjects();
        }

        public bool DetectAutoFail() {
            // Initialize lists
            UnloadingObjects = new List<double>();
            PotentialUnloadingObjects = new List<double>();
            Disruptors = new List<double>();

            // Get times to check
            // These are all the times at which the startIndex can change in the object loading system.
            timesToCheckStartIndex = new SortedSet<int>(hitObjects.SelectMany(ho => new[] {
                (int)ho.EndTime + approachTime,
                (int)ho.EndTime + approachTime + 1
            }));

            // Find all problematic areas which could cause auto-fail depending on the binary search
            // A problem area consists of one object and the objects which can unload it
            // An object B can unload another object A if it has a later index than A and an end time earlier than A's end time - approach time.
            // A loaded object has to be loaded after its end time for any period long enough for the physics update tick to count the judgement.
            // I ignore all unloadable objects B for which at least one unloadable object A is loaded implies B is loaded. In that case I say A contains B.
            problemAreas = new List<ProblemArea>();
            for (int i = 0; i < hitObjects.Count; i++) {
                var ho = hitObjects[i];
                var adjEndTime = GetAdjustedEndTime(ho);
                var negative = adjEndTime < ho.Time - approachTime;

                // Ignore all problem areas which are contained by another unloadable object,
                // because fixing the outer problem area will also fix all of the problems inside.
                // Added a check for the end time to prevent weird situations with the endIndex caused by negative duration.
                if (problemAreas.Count > 0 && !negative) {
                    // Lower end time means that it will be loaded alongside the previous problem area.
                    var lastAdjEndTime = GetAdjustedEndTime(problemAreas.Last().unloadableHitObject);
                    if (adjEndTime <= lastAdjEndTime) {
                        continue;
                    }

                    // If the end time is greater but there has been no time to change the start index yet,
                    // then it is still contained in the previous problem area.
                    if (timesToCheckStartIndex.GetViewBetween(lastAdjEndTime, adjEndTime + physicsTime).Count == 0) {
                        continue;
                    }
                }

                // Check all later objects for any which have an early enough end time
                var disruptors = new HashSet<HitObject>();
                for (int j = i + 1; j < hitObjects.Count; j++) {
                    var ho2 = hitObjects[j];
                    if (ho2.EndTime < adjEndTime + physicsTime - approachTime) {
                        disruptors.Add(ho2);

                        Disruptors.Add(ho2.Time);
                    }
                }

                if (disruptors.Count == 0 && !negative)
                    continue;

                // The first time after the end time where the object could be loaded
                var firstRequiredLoadTime = adjEndTime;
                if (i > 0)
                    firstRequiredLoadTime = Math.Max(adjEndTime, (int)hitObjects[i - 1].Time - approachTime + 1);
                // It cant load before the map has started
                firstRequiredLoadTime = Math.Max(firstRequiredLoadTime, mapStartTime);

                // These are all the times to check. If the object is loaded at all these times, then it will not cause auto-fail. (terms and conditions apply)
                var timesToCheck = new HashSet<int>(timesToCheckStartIndex.GetViewBetween(
                    firstRequiredLoadTime, firstRequiredLoadTime + physicsTime)) {firstRequiredLoadTime + physicsTime};

                problemAreas.Add(new ProblemArea { index = i, unloadableHitObject = ho, disruptors = disruptors, timesToCheck = timesToCheck });
                PotentialUnloadingObjects.Add(ho.Time);
            }

            int autoFails = 0;
            // Use osu!'s object loading algorithm to find out which objects are actually loaded
            foreach (var problemArea in problemAreas) {
                foreach (var time in problemArea.timesToCheck) {
                    var minimalLeft = time - approachTime;
                    var minimalRight = time + approachTime;

                    var startIndex = OsuBinarySearch(minimalLeft);
                    var endIndex = hitObjects.FindIndex(startIndex, ho => ho.Time > minimalRight);
                    if (endIndex < 0) {
                        endIndex = hitObjects.Count - 1;
                    }

                    var hitObjectsMinimal = hitObjects.GetRange(startIndex, 1 + endIndex - startIndex);

                    if (!hitObjectsMinimal.Contains(problemArea.unloadableHitObject) || time > autoFailCheckTime) {
                        UnloadingObjects.Add(problemArea.unloadableHitObject.Time);
                        autoFails++;
                        break;
                    }
                }
            }

            return autoFails > 0;
        }

        private int GetAdjustedEndTime(HitObject ho) {
            if (ho.IsCircle) {
                return (int)ho.Time + window50;
            }
            if (ho.IsSlider || ho.IsSpinner) {
                return (int)ho.EndTime;
            }

            return (int)Math.Max(ho.Time + window50, ho.EndTime);
        }

        public bool AutoFailFixDialogue(bool autoPlaceFix) {
            if (problemAreas.Count == 0)
                return false;

            placementTimes = GetAllSafePlacementTimes();

            int[] solution = SolveAutoFailPadding();
            int paddingCount = solution.Sum();
            bool acceptedSolution = false;
            int solutionCount = 0;

            foreach (var sol in SolveAutoFailPaddingEnumerableInfinite(paddingCount)) {
                solution = sol;

                StringBuilder guideBuilder = new StringBuilder();
                AddFixGuide(guideBuilder, sol);
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


            if (autoPlaceFix && acceptedSolution) {
                PlaceFixGuide(solution);
                return true;
            }

            return false;
        }

        private void AddFixGuide(StringBuilder guideBuilder, IReadOnlyList<int> paddingSolution) {
            guideBuilder.AppendLine("Auto-fail fix guide. Place these extra objects to fix auto-fail:\n");
            int lastTime = 0;
            for (int i = 0; i < problemAreas.Count; i++) {
                if (!(placementTimes != null && !placementTimes[i].HasValue)) {
                    guideBuilder.AppendLine(i == 0
                        ? $"Extra objects before {problemAreas[i].GetStartTime()}: {paddingSolution[i]}"
                        : $"Extra objects between {lastTime} - {problemAreas[i].GetStartTime()}: {paddingSolution[i]}");
                }
                lastTime = GetAdjustedEndTime(problemAreas[i].unloadableHitObject) - approachTime;
            }

            if (!(placementTimes != null && !placementTimes[placementTimes.Length - 1].HasValue)) {
                guideBuilder.AppendLine($"Extra objects after {lastTime}: {paddingSolution.Last()}");
            }
        }

        private void PlaceFixGuide(IReadOnlyList<int> paddingSolution) {
            int lastTime = mapStartTime;
            for (int i = 0; i < problemAreas.Count; i++) {
                if (paddingSolution[i] > 0) {
                    var t = placementTimes != null ?
                        placementTimes[i] :
                        GetSafePlacementTime(lastTime, problemAreas[i].GetStartTime());
                    if (t.HasValue) {
                        for (int j = 0; j < paddingSolution[i]; j++) {
                            hitObjects.Add(
                                new HitObject {Pos = Vector2.Zero, Time = t.Value, ObjectType = 8, EndTime = t.Value - 1});
                        }
                    } else {
                        throw new Exception($"Can't find a safe place to place objects between {lastTime} and {problemAreas[i].GetStartTime()}.");
                    }
                }

                lastTime = GetAdjustedEndTime(problemAreas[i].unloadableHitObject) - approachTime;
            }

            if (paddingSolution.Last() > 0) {
                var t = placementTimes != null ?
                    placementTimes.Last() : 
                    GetSafePlacementTime(lastTime, autoFailCheckTime - physicsTime);
                if (t.HasValue) {
                    for (int i = 0; i < paddingSolution.Last(); i++) {
                        hitObjects.Add(new HitObject {Pos = Vector2.Zero, Time = t.Value, ObjectType = 8, EndTime = t.Value - 1});
                    }
                } else {
                    throw new Exception($"Can't find a safe place to place objects between {lastTime} and {mapEndTime}.");
                }
            }

            SortHitObjects();
        }

        private int?[] GetAllSafePlacementTimes() {
            int?[] allSafePlacementTimes = new int?[problemAreas.Count + 1];

            int lastTime = mapStartTime;
            for (int i = 0; i < problemAreas.Count; i++) {
                var t = GetSafePlacementTime(lastTime, problemAreas[i].GetStartTime());
                allSafePlacementTimes[i] = t;

                lastTime = GetAdjustedEndTime(problemAreas[i].unloadableHitObject) - approachTime;
            }
            allSafePlacementTimes[allSafePlacementTimes.Length - 1] = GetSafePlacementTime(lastTime, autoFailCheckTime - physicsTime);

            return allSafePlacementTimes;
        }

        private int? GetSafePlacementTime(int start, int end) {
            var rangeObjects = hitObjects.FindAll(o => o.EndTime >= start && o.Time <= end);

            for (int i = end - 1; i >= start; i--) {
                if (!rangeObjects.Any(ho =>
                    i >= (int)ho.Time &&
                    i <= GetAdjustedEndTime(ho) - approachTime)) {
                    return i;
                }
            }

            return null;
        }

        private int[] SolveAutoFailPadding(int startPaddingCount = 0) {
            int padding = startPaddingCount;
            int[] solution;
            while (!SolveAutoFailPadding(padding++, out solution)) {
                if (padding > maxPaddingCount) {
                    throw new Exception("No auto-fail fix padding solution found.");
                }
            }

            return solution;
        }

        private bool SolveAutoFailPadding(int paddingCount, out int[] solution) {
            solution = new int[problemAreas.Count + 1];

            int leftPadding = 0;
            for (var i = 0; i < problemAreas.Count; i++) {
                var problemAreaSolution =
                    SolveSingleProblemAreaPadding(problemAreas[i], paddingCount, leftPadding);

                if (problemAreaSolution.Count == 0 || problemAreaSolution.Max() < leftPadding) {
                    return false;
                }

                // The first element is always the lowest element equal or greater than leftPadding,
                // because the single problem solver started iterating from leftPadding.
                var lowest = problemAreaSolution.First();

                // Check if placement is possible for this area and if not, assert 0 padding
                if (placementTimes != null && !placementTimes[i].HasValue && lowest != leftPadding) {
                    return false;
                }

                solution[i] = lowest - leftPadding;
                leftPadding = lowest;
            }

            // Check if placement is possible for the last area and if not, assert 0 padding
            if (placementTimes != null && !placementTimes[placementTimes.Length - 1].HasValue && paddingCount != leftPadding) {
                return false;
            }

            solution[solution.Length - 1] = paddingCount - leftPadding;

            return true;
        }

        private IEnumerable<int[]> SolveAutoFailPaddingEnumerableInfinite(int initialPaddingCount) {
            int paddingCount = initialPaddingCount;
            while (true) {
                foreach (var solution in SolveAutoFailPaddingEnumerable(paddingCount)) {
                    yield return solution;
                }

                paddingCount++;
            }
        }

        private IEnumerable<int[]> SolveAutoFailPaddingEnumerable(int paddingCount) {
            List<int>[] allSolutions = new List<int>[problemAreas.Count];

            int minimalLeft = 0;
            for (var i = 0; i < problemAreas.Count; i++) {
                var problemAreaSolution =
                    SolveSingleProblemAreaPadding(problemAreas[i], paddingCount, minimalLeft);

                if (problemAreaSolution.Count == 0 || problemAreaSolution.Last() < minimalLeft) {
                    yield break;
                }

                // The first element is always the lowest element equal or greater than minimalLeft,
                // because the single problem solver started iterating from minimalLeft.
                var lowest = problemAreaSolution.First();

                // Check if placement is possible for this area and if not, assert 0 padding
                if (placementTimes != null && !placementTimes[i].HasValue && lowest != minimalLeft) {
                    yield break;
                }

                allSolutions[i] = problemAreaSolution;
                minimalLeft = lowest;
            }

            // Check if placement is possible for the last area and if not, assert 0 padding
            if (placementTimes != null && !placementTimes[placementTimes.Length - 1].HasValue && paddingCount != minimalLeft) {
                yield break;
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

                // If there is no placement for the last area, assert 0 padding.
                if (placementTimes != null && !placementTimes[placementTimes.Length - 1].HasValue && left != paddingCount) {
                    continue;
                }

                pads[pads.Length - 1] = paddingCount - left;
                yield return pads;
            }
        }

        private IEnumerable<int[]> EnumerateSolutions(IReadOnlyList<List<int>> allSolutions, int depth = 0, int minimum = 0) {
            if (depth == allSolutions.Count - 1) {
                // Loop through all solutions which are greater or equal to the minimum or assert equal to paddingCount if there is no placement spot.
                foreach (var i in allSolutions[depth].Where(o => o == minimum ||
                                                                 !(placementTimes != null && !placementTimes[depth].HasValue) && o > minimum)) {
                    var s = new int[allSolutions.Count];
                    s[depth] = i;
                    yield return s;
                }
                yield break;
            }
            // Loop through all solutions which are greater or equal to the minimum or assert equal to minimum if there is no placement spot.
            foreach (var i in allSolutions[depth].Where(o => o == minimum || 
                                                             !(placementTimes != null && !placementTimes[depth].HasValue) && o > minimum)) {
                foreach (var j in EnumerateSolutions(allSolutions, depth + 1, minimum = i)) {
                    j[depth] = i;
                    yield return j;
                }
            }
        }

        private List<int> SolveSingleProblemAreaPadding(ProblemArea problemArea, int paddingCount, int minimalLeft = 0) {
            var solution = new List<int>(paddingCount - minimalLeft + 1);

            for (int left = minimalLeft; left <= paddingCount; left++) {
                var right = paddingCount - left;

                if (ProblemAreaPaddingWorks(problemArea, left, right)) {
                    solution.Add(left);
                }
            }

            return solution;
        }

        private bool ProblemAreaPaddingWorks(ProblemArea problemArea, int left, int right) {
            foreach (var time in problemArea.timesToCheck) {
                var minimalLeft = time - approachTime;
                var minimalRight = time + approachTime;

                var startIndex = PaddedOsuBinarySearch(minimalLeft, left, right);
                var endIndex = hitObjects.FindIndex(startIndex, ho => ho.Time > minimalRight);
                if (endIndex < 0) {
                    endIndex = hitObjects.Count - 1;
                }

                if (startIndex > problemArea.index || endIndex < problemArea.index || time > autoFailCheckTime) {
                    return false;
                }
            }

            return true;
        }

        private int OsuBinarySearch(int time) {
            var n = hitObjects.Count;
            var min = 0;
            var max = n - 1;
            while (min <= max) {
                var mid = min + (max - min) / 2;
                var t = (int)hitObjects[mid].EndTime;

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

        private int PaddedOsuBinarySearch(int time, int left, int right) {
            var n = hitObjects.Count;
            var min = -left;
            var max = n - 1 + right;
            while (min <= max) {
                var mid = min + (max - min) / 2;
                var t = mid < 0 ? int.MinValue : mid > hitObjects.Count - 1 ? int.MaxValue : (int)hitObjects[mid].EndTime;

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