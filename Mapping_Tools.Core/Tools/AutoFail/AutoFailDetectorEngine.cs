using System.Text;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.AutoFail;

/// <summary>Contains one immutable auto-fail analysis result.</summary>
/// <param name="HasAutoFail">Whether at least one object unloads incorrectly.</param>
/// <param name="UnloadingObjects">The timestamps of confirmed unloading objects.</param>
/// <param name="PotentialUnloadingObjects">The timestamps of objects that may unload.</param>
/// <param name="Disruptors">The timestamps of objects that disrupt loading.</param>
public sealed record AutoFailAnalysis(
    bool HasAutoFail,
    IReadOnlyList<double> UnloadingObjects,
    IReadOnlyList<double> PotentialUnloadingObjects,
    IReadOnlyList<double> Disruptors);

/// <summary>Describes one candidate distribution of padding objects and its human-readable guide.</summary>
/// <param name="Padding">The number of objects inserted around each problem area.</param>
/// <param name="Guide">The mapper-facing instructions for reproducing the repair.</param>
public sealed record AutoFailFixPlan(IReadOnlyList<int> Padding, string Guide);

/// <summary>Reproduces osu!'s object-loading search and plans optional padding without UI dependencies.</summary>
public sealed class AutoFailDetectorEngine
{
    private const int max_padding_count = 2000;
    private readonly int approachTime;
    private readonly int autoFailCheckTime;
    private readonly int mapEndTime;
    private readonly int mapStartTime;
    private readonly int physicsTime;
    private readonly int window50;
    private List<HitObject> hitObjects;
    private int?[]? placementTimes;
    private List<ProblemArea>? problemAreas;
    private SortedSet<int>? timesToCheckStartIndex;

    /// <summary>Creates an analyzer for one beatmap and one set of difficulty windows.</summary>
    /// <param name="hitObjects">The mutable hit-object collection to inspect and optionally repair.</param>
    /// <param name="mapStartTime">The first relevant map timestamp.</param>
    /// <param name="mapEndTime">The final map timestamp.</param>
    /// <param name="autoFailCheckTime">The latest timestamp at which unloading causes auto-fail.</param>
    /// <param name="approachTime">The simulated object preempt duration.</param>
    /// <param name="window50">The simulated 50 judgement window.</param>
    /// <param name="physicsTime">The tolerated physics-update delay.</param>
    public AutoFailDetectorEngine(
        List<HitObject> hitObjects,
        int mapStartTime,
        int mapEndTime,
        int autoFailCheckTime,
        int approachTime,
        int window50,
        int physicsTime)
    {
        ArgumentNullException.ThrowIfNull(hitObjects);
        if (physicsTime < 0) throw new ArgumentOutOfRangeException(nameof(physicsTime));
        this.hitObjects = hitObjects;
        this.mapStartTime = mapStartTime;
        this.mapEndTime = mapEndTime;
        this.autoFailCheckTime = autoFailCheckTime;
        this.approachTime = approachTime;
        this.window50 = window50;
        this.physicsTime = physicsTime;

        // Sort the hitobjects
        SortHitObjects();
    }

    /// <summary>Replaces the mutable object collection and invalidates prior analysis state.</summary>
    /// <param name="hitObjects">The new hit-object collection.</param>
    public void SetHitObjects(List<HitObject> hitObjects)
    {
        this.hitObjects = hitObjects ?? throw new ArgumentNullException(nameof(hitObjects));
        problemAreas = null;
        SortHitObjects();
    }

    /// <summary>Detects confirmed and potential unloading conditions.</summary>
    /// <param name="cancellationToken">Cancels the object-loading simulation.</param>
    /// <returns>The detected unloading objects and disruptors.</returns>
    public AutoFailAnalysis Analyze(CancellationToken cancellationToken = default)
    {
        // Initialize lists
        List<double> unloading = [];
        List<double> potential = [];
        List<double> disruptorTimes = [];
        // Get times to check
        // These are all the times at which the startIndex can change in the object loading system.
        timesToCheckStartIndex = new SortedSet<int>(hitObjects.SelectMany(hitObject => new[]
        {
            (int)hitObject.EndTime + approachTime,
            (int)hitObject.EndTime + approachTime + 1,
        }));

        // Find all problematic areas which could cause auto-fail depending on the binary search
        // A problem area consists of one object and the objects which can unload it
        // An object B can unload another object A if it has a later index than A and an end time earlier than A's end time - approach time.
        // A loaded object has to be loaded after its end time for any period long enough for the physics update tick to count the judgement.
        // I ignore all unloadable objects B for which at least one unloadable object A is loaded implies B is loaded. In that case I say A contains B.
        problemAreas = [];
        for (int index = 0; index < hitObjects.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hitObject = hitObjects[index];
            int adjustedEndTime = GetAdjustedEndTime(hitObject);
            bool negative = adjustedEndTime < hitObject.Time - approachTime;
            // Ignore all problem areas which are contained by another unloadable object,
            // because fixing the outer problem area will also fix all of the problems inside.
            // Added a check for the end time to prevent weird situations with the endIndex caused by negative duration.
            if (problemAreas.Count > 0 && !negative)
            {
                // Lower end time means that it will be loaded alongside the previous problem area.
                int lastAdjustedEndTime = GetAdjustedEndTime(problemAreas[^1].UnloadableHitObject);
                // If the end time is greater but there has been no time to change the start index yet,
                // then it is still contained in the previous problem area.
                if (adjustedEndTime <= lastAdjustedEndTime
                    || timesToCheckStartIndex.GetViewBetween(
                        lastAdjustedEndTime,
                        adjustedEndTime + physicsTime).Count
                    == 0)
                    continue;
            }

            // Check all later objects for any which have an early enough end time
            HashSet<HitObject> disruptors = [];
            for (int otherIndex = index + 1; otherIndex < hitObjects.Count; otherIndex++)
            {
                var other = hitObjects[otherIndex];
                if (other.EndTime < adjustedEndTime + physicsTime - approachTime)
                {
                    disruptors.Add(other);
                    disruptorTimes.Add(other.Time);
                }
            }

            if (disruptors.Count == 0 && !negative) continue;

            // The first time after the end time where the object could be loaded
            int firstRequiredLoadTime = adjustedEndTime;
            if (index > 0)
                firstRequiredLoadTime = Math.Max(
                    adjustedEndTime,
                    (int)hitObjects[index - 1].Time - approachTime + 1);
            // It cant load before the map has started
            firstRequiredLoadTime = Math.Max(firstRequiredLoadTime, mapStartTime);
            // These are all the times to check. If the object is loaded at all these times, then it will not cause auto-fail. (terms and conditions apply)
            HashSet<int> timesToCheck = new(
                timesToCheckStartIndex.GetViewBetween(
                    firstRequiredLoadTime,
                    firstRequiredLoadTime + physicsTime))
            {
                firstRequiredLoadTime + physicsTime,
            };
            problemAreas.Add(new ProblemArea(index, hitObject, disruptors, timesToCheck));
            potential.Add(hitObject.Time);
        }

        // Use osu!'s object loading algorithm to find out which objects are actually loaded
        foreach (var problemArea in problemAreas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (int time in problemArea.TimesToCheck)
            {
                int startIndex = OsuBinarySearch(time - approachTime);
                int endIndex = hitObjects.FindIndex(
                    startIndex,
                    hitObject => hitObject.Time > time + approachTime);
                if (endIndex < 0) endIndex = hitObjects.Count - 1;

                var loaded = hitObjects.GetRange(
                    startIndex,
                    1 + endIndex - startIndex);
                if (!loaded.Contains(problemArea.UnloadableHitObject) || time > autoFailCheckTime)
                {
                    unloading.Add(problemArea.UnloadableHitObject.Time);
                    break;
                }
            }
        }

        return new AutoFailAnalysis(
            unloading.Count > 0,
            unloading.ToArray(),
            potential.ToArray(),
            disruptorTimes.ToArray());
    }

    /// <summary>Lazily enumerates valid padding distributions after analysis.</summary>
    /// <param name="cancellationToken">Cancels solution enumeration.</param>
    /// <returns>Repair plans ordered from fewer to more padding objects.</returns>
    public IEnumerable<AutoFailFixPlan> GetFixPlans(
        CancellationToken cancellationToken = default)
    {
        EnsureAnalyzed();
        if (problemAreas!.Count == 0) yield break;

        placementTimes = GetAllSafePlacementTimes();
        int[] firstSolution = SolveAutoFailPadding();
        int paddingCount = firstSolution.Sum();
        foreach (int[] solution in SolveAutoFailPaddingEnumerableInfinite(paddingCount))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new AutoFailFixPlan(solution.ToArray(), BuildFixGuide(solution));
        }
    }

    /// <summary>Applies one repair plan to the mutable hit-object collection.</summary>
    /// <param name="plan">The plan produced by <see cref="GetFixPlans" />.</param>
    public void ApplyFix(AutoFailFixPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        EnsureAnalyzed();
        if (placementTimes is null) placementTimes = GetAllSafePlacementTimes();
        if (plan.Padding.Count != problemAreas!.Count + 1) throw new ArgumentException("The fix plan does not match this analysis.", nameof(plan));
        PlaceFixGuide(plan.Padding);
    }

    private string BuildFixGuide(IReadOnlyList<int> paddingSolution)
    {
        StringBuilder guide = new();
        guide.AppendLine("Auto-fail fix guide. Place these extra objects to fix auto-fail:");
        guide.AppendLine();
        int lastTime = 0;
        for (int index = 0; index < problemAreas!.Count; index++)
        {
            if (!(placementTimes is not null && !placementTimes[index].HasValue) && paddingSolution[index] > 0)
                guide.AppendLine(index == 0
                    ? $"Extra objects before {problemAreas[index].StartTime}: {paddingSolution[index]}"
                    : $"Extra objects between {lastTime} - {problemAreas[index].StartTime}: {paddingSolution[index]}");
            lastTime = GetAdjustedEndTime(problemAreas[index].UnloadableHitObject) - approachTime;
        }

        if (!(placementTimes is not null && !placementTimes[^1].HasValue) && paddingSolution[^1] > 0)
            guide.AppendLine($"Extra objects after {lastTime}: {paddingSolution[^1]}");
        return guide.ToString().TrimEnd();
    }

    private void PlaceFixGuide(IReadOnlyList<int> paddingSolution)
    {
        int lastTime = mapStartTime;
        for (int index = 0; index < problemAreas!.Count; index++)
        {
            if (paddingSolution[index] > 0)
            {
                int? time = placementTimes is not null
                    ? placementTimes[index]
                    : GetSafePlacementTime(lastTime, problemAreas[index].StartTime);
                if (!time.HasValue)
                    throw new InvalidOperationException(
                        $"Can't find a safe place to place objects between {lastTime} and {problemAreas[index].StartTime}.");
                for (int count = 0; count < paddingSolution[index]; count++) hitObjects.Add(PaddingObject(time.Value));
            }

            lastTime = GetAdjustedEndTime(problemAreas[index].UnloadableHitObject) - approachTime;
        }

        if (paddingSolution[^1] > 0)
        {
            int? time = placementTimes is not null
                ? placementTimes[^1]
                : GetSafePlacementTime(lastTime, autoFailCheckTime - physicsTime);
            if (!time.HasValue)
                throw new InvalidOperationException(
                    $"Can't find a safe place to place objects between {lastTime} and {mapEndTime}.");
            for (int count = 0; count < paddingSolution[^1]; count++) hitObjects.Add(PaddingObject(time.Value));
        }

        SortHitObjects();
    }

    private static HitObject PaddingObject(int time)
    {
        return new HitObject
        {
            Pos = Vector2.Zero,
            Time = time,
            ObjectType = 8,
            EndTime = time - 1,
        };
    }

    private int?[] GetAllSafePlacementTimes()
    {
        int?[] result = new int?[problemAreas!.Count + 1];
        int lastTime = mapStartTime;
        for (int index = 0; index < problemAreas.Count; index++)
        {
            result[index] = GetSafePlacementTime(lastTime, problemAreas[index].StartTime);
            lastTime = GetAdjustedEndTime(problemAreas[index].UnloadableHitObject) - approachTime;
        }

        result[^1] = GetSafePlacementTime(lastTime, autoFailCheckTime - physicsTime);
        return result;
    }

    private int? GetSafePlacementTime(int start, int end)
    {
        var rangeObjects = hitObjects.FindAll(hitObject => hitObject.EndTime >= start && hitObject.Time <= end);
        for (int time = end - 1; time >= start; time--)
            if (!rangeObjects.Any(hitObject =>
                    time >= (int)hitObject.Time && time <= GetAdjustedEndTime(hitObject) - approachTime))
                return time;

        return null;
    }

    private int[] SolveAutoFailPadding(int startPaddingCount = 0)
    {
        int padding = startPaddingCount;
        int[] solution;
        while (!SolveAutoFailPadding(padding++, out solution))
            if (padding > max_padding_count)
                throw new InvalidOperationException("No auto-fail fix padding solution found.");

        return solution;
    }

    private bool SolveAutoFailPadding(int paddingCount, out int[] solution)
    {
        solution = new int[problemAreas!.Count + 1];
        int leftPadding = 0;
        for (int index = 0; index < problemAreas.Count; index++)
        {
            var choices = SolveSingleProblemAreaPadding(
                problemAreas[index],
                paddingCount,
                leftPadding);
            if (choices.Count == 0 || choices.Max() < leftPadding) return false;
            // The first element is always the lowest element equal or greater than leftPadding,
            // because the single problem solver started iterating from leftPadding.
            int lowest = choices[0];
            // Check if placement is possible for this area and if not, assert 0 padding
            if (placementTimes is not null && !placementTimes[index].HasValue && lowest != leftPadding)
                return false;
            solution[index] = lowest - leftPadding;
            leftPadding = lowest;
        }

        // Check if placement is possible for the last area and if not, assert 0 padding
        if (placementTimes is not null && !placementTimes[^1].HasValue && paddingCount != leftPadding)
            return false;
        solution[^1] = paddingCount - leftPadding;
        return true;
    }

    private IEnumerable<int[]> SolveAutoFailPaddingEnumerableInfinite(int initialPaddingCount)
    {
        int paddingCount = initialPaddingCount;
        while (true)
        {
            foreach (int[] solution in SolveAutoFailPaddingEnumerable(paddingCount)) yield return solution;
            paddingCount++;
        }
    }

    private IEnumerable<int[]> SolveAutoFailPaddingEnumerable(int paddingCount)
    {
        var allSolutions = new List<int>[problemAreas!.Count];
        int minimalLeft = 0;
        for (int index = 0; index < problemAreas.Count; index++)
        {
            var choices = SolveSingleProblemAreaPadding(
                problemAreas[index],
                paddingCount,
                minimalLeft);
            if (choices.Count == 0 || choices[^1] < minimalLeft) yield break;
            // The first element is always the lowest element equal or greater than minimalLeft,
            // because the single problem solver started iterating from minimalLeft.
            int lowest = choices[0];
            // Check if placement is possible for this area and if not, assert 0 padding
            if (placementTimes is not null && !placementTimes[index].HasValue && lowest != minimalLeft)
                yield break;
            allSolutions[index] = choices;
            minimalLeft = lowest;
        }

        // Check if placement is possible for the last area and if not, assert 0 padding
        if (placementTimes is not null && !placementTimes[^1].HasValue && paddingCount != minimalLeft)
            yield break;
        int maximalLeft = paddingCount;
        for (int index = allSolutions.Length - 1; index >= 0; index--)
        {
            allSolutions[index].RemoveAll(value => value > maximalLeft);
            maximalLeft = allSolutions[index][^1];
        }

        foreach (int[] leftPadding in EnumerateSolutions(allSolutions))
        {
            int[] pads = new int[leftPadding.Length + 1];
            int left = 0;
            for (int index = 0; index < leftPadding.Length; index++)
            {
                pads[index] = leftPadding[index] - left;
                left = leftPadding[index];
            }

            // If there is no placement for the last area, assert 0 padding.
            if (placementTimes is not null && !placementTimes[^1].HasValue && left != paddingCount)
                continue;
            pads[^1] = paddingCount - left;
            yield return pads;
        }
    }

    private IEnumerable<int[]> EnumerateSolutions(
        IReadOnlyList<List<int>> allSolutions,
        int depth = 0,
        int minimum = 0)
    {
        if (depth == allSolutions.Count - 1)
        {
            // Loop through all solutions which are greater or equal to the minimum or assert equal to paddingCount if there is no placement spot.
            foreach (int value in allSolutions[depth].Where(value =>
                         value == minimum || !(placementTimes is not null && !placementTimes[depth].HasValue) && value > minimum))
            {
                int[] solution = new int[allSolutions.Count];
                solution[depth] = value;
                yield return solution;
            }

            yield break;
        }

        // Loop through all solutions which are greater or equal to the minimum or assert equal to minimum if there is no placement spot.
        foreach (int value in allSolutions[depth].Where(value =>
                     value == minimum || !(placementTimes is not null && !placementTimes[depth].HasValue) && value > minimum))
        foreach (int[] solution in EnumerateSolutions(allSolutions, depth + 1, value))
        {
            solution[depth] = value;
            yield return solution;
        }
    }

    private List<int> SolveSingleProblemAreaPadding(
        ProblemArea problemArea,
        int paddingCount,
        int minimalLeft = 0)
    {
        List<int> solution = new(paddingCount - minimalLeft + 1);
        for (int left = minimalLeft; left <= paddingCount; left++)
            if (ProblemAreaPaddingWorks(problemArea, left, paddingCount - left))
                solution.Add(left);

        return solution;
    }

    private bool ProblemAreaPaddingWorks(ProblemArea problemArea, int left, int right)
    {
        foreach (int time in problemArea.TimesToCheck)
        {
            int startIndex = PaddedOsuBinarySearch(time - approachTime, left, right);
            int endIndex = hitObjects.FindIndex(
                Math.Max(0, startIndex),
                hitObject => hitObject.Time > time + approachTime);
            if (endIndex < 0) endIndex = hitObjects.Count - 1;
            if (startIndex > problemArea.Index || endIndex < problemArea.Index || time > autoFailCheckTime)
                return false;
        }

        return true;
    }

    private int OsuBinarySearch(int time)
    {
        int minimum = 0;
        int maximum = hitObjects.Count - 1;
        while (minimum <= maximum)
        {
            int middle = minimum + (maximum - minimum) / 2;
            int endTime = (int)hitObjects[middle].EndTime;
            if (time == endTime) return middle;
            if (time > endTime)
                minimum = middle + 1;
            else
                maximum = middle - 1;
        }

        return minimum;
    }

    private int PaddedOsuBinarySearch(int time, int left, int right)
    {
        int minimum = -left;
        int maximum = hitObjects.Count - 1 + right;
        while (minimum <= maximum)
        {
            int middle = minimum + (maximum - minimum) / 2;
            int endTime = middle < 0
                ? int.MinValue
                : middle > hitObjects.Count - 1
                    ? int.MaxValue
                    : (int)hitObjects[middle].EndTime;
            if (time == endTime) return middle;
            if (time > endTime)
                minimum = middle + 1;
            else
                maximum = middle - 1;
        }

        return minimum;
    }

    private int GetAdjustedEndTime(HitObject hitObject)
    {
        if (hitObject.IsCircle) return (int)hitObject.Time + window50;
        if (hitObject.IsSlider || hitObject.IsSpinner) return (int)hitObject.EndTime;
        return (int)Math.Max(hitObject.Time + window50, hitObject.EndTime);
    }

    private void SortHitObjects()
    {
        hitObjects.Sort();
    }

    private void EnsureAnalyzed()
    {
        if (problemAreas is null) throw new InvalidOperationException("Analyze must run before requesting or applying a fix.");
    }

    private sealed record ProblemArea(
        int Index,
        HitObject UnloadableHitObject,
        HashSet<HitObject> Disruptors,
        HashSet<int> TimesToCheck)
    {
        public int StartTime => (int)UnloadableHitObject.Time;
    }
}
