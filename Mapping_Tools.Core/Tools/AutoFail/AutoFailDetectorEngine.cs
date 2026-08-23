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
    private const int MaxPaddingCount = 2000;
    private readonly int _approachTime;
    private readonly int _autoFailCheckTime;
    private readonly int _mapEndTime;
    private readonly int _mapStartTime;
    private readonly int _physicsTime;
    private readonly int _window50;
    private List<HitObject> _hitObjects;
    private int?[]? _placementTimes;
    private List<ProblemArea>? _problemAreas;
    private SortedSet<int>? _timesToCheckStartIndex;

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
        _hitObjects = hitObjects;
        _mapStartTime = mapStartTime;
        _mapEndTime = mapEndTime;
        _autoFailCheckTime = autoFailCheckTime;
        _approachTime = approachTime;
        _window50 = window50;
        _physicsTime = physicsTime;

        // Sort the hitobjects
        SortHitObjects();
    }

    /// <summary>Replaces the mutable object collection and invalidates prior analysis state.</summary>
    /// <param name="hitObjects">The new hit-object collection.</param>
    public void SetHitObjects(List<HitObject> hitObjects)
    {
        _hitObjects = hitObjects ?? throw new ArgumentNullException(nameof(hitObjects));
        _problemAreas = null;
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
        _timesToCheckStartIndex = new SortedSet<int>(_hitObjects.SelectMany(hitObject => new[]
        {
            (int)hitObject.EndTime + _approachTime,
            (int)hitObject.EndTime + _approachTime + 1,
        }));

        // Find all problematic areas which could cause auto-fail depending on the binary search
        // A problem area consists of one object and the objects which can unload it
        // An object B can unload another object A if it has a later index than A and an end time earlier than A's end time - approach time.
        // A loaded object has to be loaded after its end time for any period long enough for the physics update tick to count the judgement.
        // I ignore all unloadable objects B for which at least one unloadable object A is loaded implies B is loaded. In that case I say A contains B.
        _problemAreas = [];
        for (int index = 0; index < _hitObjects.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hitObject = _hitObjects[index];
            int adjustedEndTime = GetAdjustedEndTime(hitObject);
            bool negative = adjustedEndTime < hitObject.Time - _approachTime;
            // Ignore all problem areas which are contained by another unloadable object,
            // because fixing the outer problem area will also fix all of the problems inside.
            // Added a check for the end time to prevent weird situations with the endIndex caused by negative duration.
            if (_problemAreas.Count > 0 && !negative)
            {
                // Lower end time means that it will be loaded alongside the previous problem area.
                int lastAdjustedEndTime = GetAdjustedEndTime(_problemAreas[^1].UnloadableHitObject);
                // If the end time is greater but there has been no time to change the start index yet,
                // then it is still contained in the previous problem area.
                if (adjustedEndTime <= lastAdjustedEndTime
                    || _timesToCheckStartIndex.GetViewBetween(
                        lastAdjustedEndTime,
                        adjustedEndTime + _physicsTime).Count
                    == 0)
                    continue;
            }

            // Check all later objects for any which have an early enough end time
            HashSet<HitObject> disruptors = [];
            for (int otherIndex = index + 1; otherIndex < _hitObjects.Count; otherIndex++)
            {
                var other = _hitObjects[otherIndex];
                if (other.EndTime < adjustedEndTime + _physicsTime - _approachTime)
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
                    (int)_hitObjects[index - 1].Time - _approachTime + 1);
            // It cant load before the map has started
            firstRequiredLoadTime = Math.Max(firstRequiredLoadTime, _mapStartTime);
            // These are all the times to check. If the object is loaded at all these times, then it will not cause auto-fail. (terms and conditions apply)
            HashSet<int> timesToCheck = new(
                _timesToCheckStartIndex.GetViewBetween(
                    firstRequiredLoadTime,
                    firstRequiredLoadTime + _physicsTime))
            {
                firstRequiredLoadTime + _physicsTime,
            };
            _problemAreas.Add(new ProblemArea(index, hitObject, disruptors, timesToCheck));
            potential.Add(hitObject.Time);
        }

        // Use osu!'s object loading algorithm to find out which objects are actually loaded
        foreach (var problemArea in _problemAreas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (int time in problemArea.TimesToCheck)
            {
                int startIndex = OsuBinarySearch(time - _approachTime);
                int endIndex = _hitObjects.FindIndex(
                    startIndex,
                    hitObject => hitObject.Time > time + _approachTime);
                if (endIndex < 0) endIndex = _hitObjects.Count - 1;

                var loaded = _hitObjects.GetRange(
                    startIndex,
                    1 + endIndex - startIndex);
                if (!loaded.Contains(problemArea.UnloadableHitObject) || time > _autoFailCheckTime)
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
        if (_problemAreas!.Count == 0) yield break;

        _placementTimes = GetAllSafePlacementTimes();
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
        if (_placementTimes is null) _placementTimes = GetAllSafePlacementTimes();
        if (plan.Padding.Count != _problemAreas!.Count + 1) throw new ArgumentException("The fix plan does not match this analysis.", nameof(plan));
        PlaceFixGuide(plan.Padding);
    }

    private string BuildFixGuide(IReadOnlyList<int> paddingSolution)
    {
        StringBuilder guide = new();
        guide.AppendLine("Auto-fail fix guide. Place these extra objects to fix auto-fail:");
        guide.AppendLine();
        int lastTime = 0;
        for (int index = 0; index < _problemAreas!.Count; index++)
        {
            if (!(_placementTimes is not null && !_placementTimes[index].HasValue) && paddingSolution[index] > 0)
                guide.AppendLine(index == 0
                    ? $"Extra objects before {_problemAreas[index].StartTime}: {paddingSolution[index]}"
                    : $"Extra objects between {lastTime} - {_problemAreas[index].StartTime}: {paddingSolution[index]}");
            lastTime = GetAdjustedEndTime(_problemAreas[index].UnloadableHitObject) - _approachTime;
        }

        if (!(_placementTimes is not null && !_placementTimes[^1].HasValue) && paddingSolution[^1] > 0)
            guide.AppendLine($"Extra objects after {lastTime}: {paddingSolution[^1]}");
        return guide.ToString().TrimEnd();
    }

    private void PlaceFixGuide(IReadOnlyList<int> paddingSolution)
    {
        int lastTime = _mapStartTime;
        for (int index = 0; index < _problemAreas!.Count; index++)
        {
            if (paddingSolution[index] > 0)
            {
                int? time = _placementTimes is not null
                    ? _placementTimes[index]
                    : GetSafePlacementTime(lastTime, _problemAreas[index].StartTime);
                if (!time.HasValue)
                    throw new InvalidOperationException(
                        $"Can't find a safe place to place objects between {lastTime} and {_problemAreas[index].StartTime}.");
                for (int count = 0; count < paddingSolution[index]; count++) _hitObjects.Add(PaddingObject(time.Value));
            }

            lastTime = GetAdjustedEndTime(_problemAreas[index].UnloadableHitObject) - _approachTime;
        }

        if (paddingSolution[^1] > 0)
        {
            int? time = _placementTimes is not null
                ? _placementTimes[^1]
                : GetSafePlacementTime(lastTime, _autoFailCheckTime - _physicsTime);
            if (!time.HasValue)
                throw new InvalidOperationException(
                    $"Can't find a safe place to place objects between {lastTime} and {_mapEndTime}.");
            for (int count = 0; count < paddingSolution[^1]; count++) _hitObjects.Add(PaddingObject(time.Value));
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
        int?[] result = new int?[_problemAreas!.Count + 1];
        int lastTime = _mapStartTime;
        for (int index = 0; index < _problemAreas.Count; index++)
        {
            result[index] = GetSafePlacementTime(lastTime, _problemAreas[index].StartTime);
            lastTime = GetAdjustedEndTime(_problemAreas[index].UnloadableHitObject) - _approachTime;
        }

        result[^1] = GetSafePlacementTime(lastTime, _autoFailCheckTime - _physicsTime);
        return result;
    }

    private int? GetSafePlacementTime(int start, int end)
    {
        var rangeObjects = _hitObjects.FindAll(hitObject => hitObject.EndTime >= start && hitObject.Time <= end);
        for (int time = end - 1; time >= start; time--)
            if (!rangeObjects.Any(hitObject =>
                    time >= (int)hitObject.Time && time <= GetAdjustedEndTime(hitObject) - _approachTime))
                return time;

        return null;
    }

    private int[] SolveAutoFailPadding(int startPaddingCount = 0)
    {
        int padding = startPaddingCount;
        int[] solution;
        while (!SolveAutoFailPadding(padding++, out solution))
            if (padding > MaxPaddingCount)
                throw new InvalidOperationException("No auto-fail fix padding solution found.");

        return solution;
    }

    private bool SolveAutoFailPadding(int paddingCount, out int[] solution)
    {
        solution = new int[_problemAreas!.Count + 1];
        int leftPadding = 0;
        for (int index = 0; index < _problemAreas.Count; index++)
        {
            var choices = SolveSingleProblemAreaPadding(
                _problemAreas[index],
                paddingCount,
                leftPadding);
            if (choices.Count == 0 || choices.Max() < leftPadding) return false;
            // The first element is always the lowest element equal or greater than leftPadding,
            // because the single problem solver started iterating from leftPadding.
            int lowest = choices[0];
            // Check if placement is possible for this area and if not, assert 0 padding
            if (_placementTimes is not null && !_placementTimes[index].HasValue && lowest != leftPadding)
                return false;
            solution[index] = lowest - leftPadding;
            leftPadding = lowest;
        }

        // Check if placement is possible for the last area and if not, assert 0 padding
        if (_placementTimes is not null && !_placementTimes[^1].HasValue && paddingCount != leftPadding)
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
        var allSolutions = new List<int>[_problemAreas!.Count];
        int minimalLeft = 0;
        for (int index = 0; index < _problemAreas.Count; index++)
        {
            var choices = SolveSingleProblemAreaPadding(
                _problemAreas[index],
                paddingCount,
                minimalLeft);
            if (choices.Count == 0 || choices[^1] < minimalLeft) yield break;
            // The first element is always the lowest element equal or greater than minimalLeft,
            // because the single problem solver started iterating from minimalLeft.
            int lowest = choices[0];
            // Check if placement is possible for this area and if not, assert 0 padding
            if (_placementTimes is not null && !_placementTimes[index].HasValue && lowest != minimalLeft)
                yield break;
            allSolutions[index] = choices;
            minimalLeft = lowest;
        }

        // Check if placement is possible for the last area and if not, assert 0 padding
        if (_placementTimes is not null && !_placementTimes[^1].HasValue && paddingCount != minimalLeft)
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
            if (_placementTimes is not null && !_placementTimes[^1].HasValue && left != paddingCount)
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
                         value == minimum || !(_placementTimes is not null && !_placementTimes[depth].HasValue) && value > minimum))
            {
                int[] solution = new int[allSolutions.Count];
                solution[depth] = value;
                yield return solution;
            }

            yield break;
        }

        // Loop through all solutions which are greater or equal to the minimum or assert equal to minimum if there is no placement spot.
        foreach (int value in allSolutions[depth].Where(value =>
                     value == minimum || !(_placementTimes is not null && !_placementTimes[depth].HasValue) && value > minimum))
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
            int startIndex = PaddedOsuBinarySearch(time - _approachTime, left, right);
            int endIndex = _hitObjects.FindIndex(
                Math.Max(0, startIndex),
                hitObject => hitObject.Time > time + _approachTime);
            if (endIndex < 0) endIndex = _hitObjects.Count - 1;
            if (startIndex > problemArea.Index || endIndex < problemArea.Index || time > _autoFailCheckTime)
                return false;
        }

        return true;
    }

    private int OsuBinarySearch(int time)
    {
        int minimum = 0;
        int maximum = _hitObjects.Count - 1;
        while (minimum <= maximum)
        {
            int middle = minimum + (maximum - minimum) / 2;
            int endTime = (int)_hitObjects[middle].EndTime;
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
        int maximum = _hitObjects.Count - 1 + right;
        while (minimum <= maximum)
        {
            int middle = minimum + (maximum - minimum) / 2;
            int endTime = middle < 0
                ? int.MinValue
                : middle > _hitObjects.Count - 1
                    ? int.MaxValue
                    : (int)_hitObjects[middle].EndTime;
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
        if (hitObject.IsCircle) return (int)hitObject.Time + _window50;
        if (hitObject.IsSlider || hitObject.IsSpinner) return (int)hitObject.EndTime;
        return (int)Math.Max(hitObject.Time + _window50, hitObject.EndTime);
    }

    private void SortHitObjects()
    {
        _hitObjects.Sort();
    }

    private void EnsureAnalyzed()
    {
        if (_problemAreas is null) throw new InvalidOperationException("Analyze must run before requesting or applying a fix.");
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
