using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.BeatmapHelper.Events;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundCopier;

/// <summary>Describes one unmatched hitsound that may become a generated sample.</summary>
/// <param name="SourceFilenames">Canonical source audio paths played by the event.</param>
/// <param name="Role">The generated sample role, such as <c>slidertick</c>.</param>
/// <param name="SampleSet">The source sample family.</param>
/// <param name="StartIndex">The first custom index to consider.</param>
public sealed record HitsoundSampleAssignmentRequest(
    IReadOnlyList<string> SourceFilenames,
    string Role,
    SampleSet SampleSet,
    int StartIndex);

/// <summary>Describes the custom index and schema entry assigned to an unmatched hitsound.</summary>
/// <param name="Index">The custom sample index assigned to the event.</param>
/// <param name="SampleSet">The sample family used by the assignment.</param>
/// <param name="Schema">Only the newly added sample entries, if any.</param>
public sealed record HitsoundSampleAssignment(
    int Index,
    SampleSet SampleSet,
    SampleSchema Schema);

/// <summary>Reports the deterministic changes made by one target-map copy.</summary>
/// <param name="MatchedHitsoundCount">The number of source events matched to target events.</param>
/// <param name="GeneratedSampleCount">The number of new sample entries created for unmatched events.</param>
/// <param name="MutedEdgeCount">The number of target edge events muted by the filter.</param>
/// <param name="SampleSchema">The newly added sample requirements.</param>
public sealed record HitsoundCopierApplyResult(
    int MatchedHitsoundCount,
    int GeneratedSampleCount,
    int MutedEdgeCount,
    SampleSchema SampleSchema);

/// <summary>
/// Applies Hitsound Copier's timeline transformation without reading or writing audio files.
/// </summary>
public static class HitsoundCopierEngine
{
    /// <summary>
    /// Copies source hitsounds, inherited sample settings, storyboard samples, and eligible
    /// muted edges into a target beatmap.
    /// </summary>
    /// <param name="target">The mutable beatmap receiving the changes.</param>
    /// <param name="source">The beatmap supplying timing, storyboard, and source objects.</param>
    /// <param name="sourceObjects">The source objects selected by the application layer.</param>
    /// <param name="options">The legacy-compatible copy settings.</param>
    /// <param name="mapDirectory">The target mapset directory used to resolve target sample paths.</param>
    /// <param name="firstSamples">Canonical target sample paths discovered by an application adapter.</param>
    /// <param name="sourceMapDirectory">The source mapset directory used to resolve source sample paths.</param>
    /// <param name="sourceSamples">Canonical source sample paths discovered by an application adapter.</param>
    /// <param name="assignSample">Optional adapter for custom sample assignment.</param>
    /// <param name="sampleSchema">An existing schema whose indices must remain unique across target maps.</param>
    /// <param name="cancellationToken">Cancels between timeline and timing changes.</param>
    /// <returns>A summary and any generated sample schema.</returns>
    public static HitsoundCopierApplyResult Apply(
        Beatmap target,
        Beatmap source,
        IReadOnlyCollection<HitObject> sourceObjects,
        HitsoundCopierOptions options,
        string mapDirectory,
        IReadOnlyDictionary<string, string>? firstSamples = null,
        string? sourceMapDirectory = null,
        IReadOnlyDictionary<string, string>? sourceSamples = null,
        Func<HitsoundSampleAssignmentRequest, HitsoundSampleAssignment?>? assignSample = null,
        SampleSchema? sampleSchema = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceObjects);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapDirectory);
        if (options.CopyMode is not 0 and not 1)
        {
            throw new ArgumentException("Hitsound Copier received an unknown copy mode.", nameof(options));
        }
        if (options.TemporalLeniency < 0 || !double.IsFinite(options.TemporalLeniency))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Temporal leniency must be finite and non-negative.");
        }

        Dictionary<string, string> samples = firstSamples is null
            ? new(StringComparer.OrdinalIgnoreCase)
            : new(firstSamples, StringComparer.OrdinalIgnoreCase);
        string sourceDirectory = string.IsNullOrWhiteSpace(sourceMapDirectory)
            ? mapDirectory
            : sourceMapDirectory;
        Dictionary<string, string> sourceSamplePaths = sourceSamples is null
            ? samples
            : new(sourceSamples, StringComparer.OrdinalIgnoreCase);
        SampleSchema generatedSamples = sampleSchema ?? new();
        int matchedCount = 0;
        int generatedCount = 0;
        int mutedCount = 0;

        Timeline targetTimeline = target.GetTimeline();
        Timeline sourceTimeline = new(sourceObjects.ToList(), source.BeatmapTiming);
        targetTimeline.GiveTimingPoints(target.BeatmapTiming);
        sourceTimeline.GiveTimingPoints(source.BeatmapTiming);
        if (options.CopyBodyHitsounds || options.CopyStoryboardedSamples || options.MuteSliderends)
        {
            target.GiveObjectsGreenlines();
            if (sourceObjects.Count > 0 && source.BeatmapTiming.TimingPoints.Count > 0)
            {
                source.GiveObjectsGreenlines();
            }
        }

        double sourceFirstTime = source.BeatmapTiming.TimingPoints.Count > 0
            ? source.BeatmapTiming.TimingPoints[0].Offset
            : double.PositiveInfinity;
        double firstTime = Math.Min(
            sourceFirstTime + options.TimingOffset,
            target.BeatmapTiming.TimingPoints.Count > 0
                ? target.BeatmapTiming.TimingPoints[0].Offset
                : double.PositiveInfinity);
        List<double>? preservedMuteTimes = options.CopyVolumes && options.AlwaysPreserve5Volume
            ? targetTimeline.TimelineObjects
                .Where(item => Math.Abs(item.SampleVolume) < Precision.DoubleEpsilon &&
                               Math.Abs(item.FenoSampleVolume - 5) < Precision.DoubleEpsilon)
                .Select(item => item.Time)
                .ToList()
            : null;

        if (options.CopyMode == 0)
        {
            if (options.CopyHitsounds)
            {
                ResetHitObjectHitsounds(target);
                targetTimeline = target.GetTimeline();
                targetTimeline.GiveTimingPoints(target.BeatmapTiming);
                matchedCount += CopyMatchingHitsounds(options, sourceTimeline, targetTimeline);
            }

            List<TimingPointChange> changes = source.BeatmapTiming.TimingPoints
                .Select(point => new TimingPointChange(
                    ShiftTimingPoint(point, options.TimingOffset),
                    sampleSet: options.CopySampleSets,
                    index: options.CopySampleSets,
                    volume: options.CopyVolumes))
                .ToList();
            if (double.IsFinite(sourceFirstTime) && double.IsFinite(firstTime))
            {
                TimingPoint first = source.BeatmapTiming.GetTimingPointAtTime(firstTime).Copy();
                first.Offset = firstTime;
                changes.Add(new TimingPointChange(
                    first,
                    sampleSet: options.CopySampleSets,
                    index: options.CopySampleSets,
                    volume: options.CopyVolumes));
            }
            TimingPointChange.Apply(target.BeatmapTiming, changes, allAfter: true);
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            if (preservedMuteTimes is not null)
            {
                RestorePreservedVolumes(targetTimeline, target, preservedMuteTimes, firstTime);
            }
        }
        else
        {
            List<TimingPointChange> changes = [];
            GameMode mode = (GameMode)target.General["Mode"].IntValue;
            if (options.CopyHitsounds)
            {
                CopySmartHitsounds(
                    target,
                    sourceTimeline,
                    targetTimeline,
                    options,
                    firstTime,
                    mode,
                    mapDirectory,
                    samples,
                    sourceDirectory,
                    sourceSamplePaths,
                    changes,
                    generatedSamples,
                    assignSample,
                    ref matchedCount,
                    ref generatedCount,
                    cancellationToken);
            }

            if (options.CopyBodyHitsounds)
            {
                foreach (TimingPoint point in target.HitObjects
                             .SelectMany(item => item.BodyHitsounds)
                             .Where(point => !point.Uninherited &&
                    sourceObjects.Any(item => IsInsideShiftedSourceObject(item, point.Offset, options.TimingOffset)))
                             .ToList())
                {
                    target.BeatmapTiming.Remove(point);
                }

                changes.AddRange(sourceObjects
                    .SelectMany(item => item.BodyHitsounds)
                    .Where(point => target.HitObjects.Any(item => item.Time < point.Offset + options.TimingOffset &&
                                                                   item.EndTime > point.Offset + options.TimingOffset))
                    .Select(point => new TimingPointChange(
                        ShiftTimingPoint(point, options.TimingOffset),
                        sampleSet: options.CopySampleSets,
                        index: options.CopySampleSets,
                        volume: options.CopyVolumes)));
            }

            TimingPointChange.Apply(target.BeatmapTiming, changes);
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            if (preservedMuteTimes is not null)
            {
                RestorePreservedVolumes(targetTimeline, target, preservedMuteTimes, firstTime);
            }
        }

        if (options.CopyStoryboardedSamples)
        {
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            if (options.CopyMode == 0)
            {
                target.StoryboardSoundSamples.Clear();
            }

            HashSet<StoryboardSoundSample> existing = new(target.StoryboardSoundSamples);
            GameMode mode = (GameMode)target.General["Mode"].IntValue;
            foreach (StoryboardSoundSample sample in source.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double sampleTime = sample.StartTime + options.TimingOffset;
                if (options.IgnoreHitsoundSatisfiedSamples)
                {
                    HashSet<string> playing = targetTimeline.TimelineObjects
                        .Where(item => Math.Abs(item.Time - sampleTime) <= options.TemporalLeniency)
                        .SelectMany(item => GetResolvedSamplePaths(item, mode, mapDirectory, samples))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    string storyboardPath = CanonicalPath(mapDirectory, sample.FilePath, samples);
                    if (playing.Contains(storyboardPath))
                    {
                        continue;
                    }
                }

                if (options.IgnoreWheneverHitsound && targetTimeline.TimelineObjects.Any(item =>
                        Math.Abs(item.Time - sampleTime) <= options.TemporalLeniency))
                {
                    continue;
                }

                StoryboardSoundSample copy = new(
                    sampleTime,
                    sample.Layer,
                    sample.FilePath,
                    sample.Volume);
                if (!existing.Contains(copy))
                {
                    target.StoryboardSoundSamples.Add(copy);
                    existing.Add(copy);
                }
            }
            target.StoryboardSoundSamples.Sort();
        }

        if (options.MuteSliderends)
        {
            target.GiveObjectsGreenlines();
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            List<TimingPointChange> changes = [];
            foreach (TimelineObject item in targetTimeline.TimelineObjects
                         .Where(item => Precision.AlmostBigger(item.Time, firstTime)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                TimingPoint point = item.HitsoundTimingPoint.Copy();
                point.Offset = item.Time;
                if (FilterMute(item, target, options))
                {
                    item.SampleSet = options.MutedSampleSet;
                    item.AdditionSet = SampleSet.None;
                    item.Normal = false;
                    item.Whistle = false;
                    item.Finish = false;
                    item.Clap = false;
                    item.HitsoundsToOrigin();
                    point.SampleSet = options.MutedSampleSet;
                    point.SampleIndex = options.MutedIndex;
                    point.Volume = 5;
                    mutedCount++;
                }
                changes.Add(new TimingPointChange(
                    point,
                    sampleSet: true,
                    index: options.MutedIndex >= 0,
                    volume: true));
            }
            TimingPointChange.Apply(target.BeatmapTiming, changes);
        }

        return new HitsoundCopierApplyResult(matchedCount, generatedCount, mutedCount, generatedSamples);
    }

    private static int CopyMatchingHitsounds(
        HitsoundCopierOptions options,
        Timeline source,
        Timeline target)
    {
        int count = 0;
        foreach (TimelineObject sourceItem in source.TimelineObjects.Where(item => item.HasHitsound))
        {
            TimelineObject? targetItem = FindMatch(sourceItem, target, options.TimingOffset, options.TemporalLeniency);
            if (targetItem is not null)
            {
                CopyHitsounds(options, sourceItem, targetItem);
                count++;
            }
            sourceItem.CanCopy = false;
        }
        return count;
    }

    private static void CopySmartHitsounds(
        Beatmap targetBeatmap,
        Timeline source,
        Timeline target,
        HitsoundCopierOptions options,
        double firstTime,
        GameMode mode,
        string mapDirectory,
        Dictionary<string, string> firstSamples,
        string sourceMapDirectory,
        IReadOnlyDictionary<string, string> sourceSamples,
        List<TimingPointChange> changes,
        SampleSchema generatedSamples,
        Func<HitsoundSampleAssignmentRequest, HitsoundSampleAssignment?>? assignSample,
        ref int matchedCount,
        ref int generatedCount,
        CancellationToken cancellationToken)
    {
        HashSet<int> customSampledTimes = [];
        List<TimelineObject> sliderSlides = [];
        foreach (TimelineObject sourceItem in source.TimelineObjects.Where(item => item.HasHitsound))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimelineObject? targetItem = FindMatch(sourceItem, target, options.TimingOffset, options.TemporalLeniency);
            if (targetItem is not null)
            {
                CopyHitsounds(options, sourceItem, targetItem);
                matchedCount++;
                if (Precision.AlmostBigger(targetItem.Time, firstTime))
                {
                    TimingPoint point = sourceItem.HitsoundTimingPoint.Copy();
                    point.Offset = targetItem.Time;
                    changes.Add(new TimingPointChange(
                        point,
                        sampleSet: options.CopySampleSets,
                        index: options.CopySampleSets,
                        volume: options.CopyVolumes));
                }
            }
            else if (options.CopyToSliderTicks &&
                     FindSliderTickInRange(
                         targetBeatmap,
                         sourceItem.Time + options.TimingOffset - options.TemporalLeniency,
                         sourceItem.Time + options.TimingOffset + options.TemporalLeniency,
                         out double tickTime,
                         out HitObject? tickSlider) &&
                     customSampledTimes.Add((int)tickTime))
            {
                HitsoundSampleAssignment? assignment = TryAssign(
                    sourceItem,
                    "slidertick",
                    mode,
                    sourceMapDirectory,
                    sourceSamples,
                    options,
                    assignSample);
                if (assignment is not null)
                {
                    generatedSamples.MergeWith(assignment.Schema);
                    tickSlider!.SampleSet = SampleSet.None;
                    AddCustomTimingChanges(changes, sourceItem, tickTime, assignment, options);
                    generatedCount += assignment.Schema.Count;
                }
            }
            else if (options.CopyToSliderSlides)
            {
                sliderSlides.Add(sourceItem);
            }
            sourceItem.CanCopy = false;
        }

        foreach (TimelineObject sourceItem in sliderSlides)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!FindSliderAtTime(targetBeatmap, sourceItem.Time + options.TimingOffset, out HitObject? slider) ||
                customSampledTimes.Contains((int)(sourceItem.Time + options.TimingOffset)))
            {
                continue;
            }

            HitsoundSampleAssignment? assignment = TryAssign(
                sourceItem,
                "sliderslide",
                mode,
                sourceMapDirectory,
                sourceSamples,
                options,
                assignSample);
            if (assignment is null)
            {
                continue;
            }

            generatedSamples.MergeWith(assignment.Schema);
            AddCustomTimingChanges(changes, sourceItem, sourceItem.Time + options.TimingOffset, assignment, options);
            slider!.SampleSet = SampleSet.None;
            generatedCount += assignment.Schema.Count;
        }

        foreach (TimelineObject targetItem in target.TimelineObjects)
        {
            if (!targetItem.CanCopy || !Precision.AlmostBigger(targetItem.Time, firstTime))
            {
                continue;
            }

            TimingPoint point = targetItem.HitsoundTimingPoint.Copy();
            bool holdSampleSet = options.CopySampleSets && targetItem.SampleSet == SampleSet.None;
            bool holdIndex = options.CopySampleSets &&
                             !(targetItem.CanCustoms && targetItem.CustomIndex != 0);
            if (holdSampleSet || holdIndex)
            {
                IReadOnlyList<string> native = GetResolvedSamplePaths(targetItem, mode, mapDirectory, firstSamples);
                if (holdSampleSet)
                {
                    SampleSet old = targetItem.FenoSampleSet;
                    SampleSet next = old;
                    double latest = double.NegativeInfinity;
                    foreach (TimingPointChange change in changes.Where(change =>
                                 change.SampleSet && change.TimingPoint.Offset <= targetItem.Time &&
                                 change.TimingPoint.Offset >= latest))
                    {
                        next = change.TimingPoint.SampleSet;
                        latest = change.TimingPoint.Offset;
                    }
                    point.SampleSet = next;
                    targetItem.GiveHitsoundTimingPoint(point);
                    point.SampleSet = native.SequenceEqual(GetResolvedSamplePaths(targetItem, mode, mapDirectory, firstSamples)) ? next : old;
                }

                if (holdIndex)
                {
                    int old = targetItem.FenoCustomIndex;
                    int next = old;
                    double latest = double.NegativeInfinity;
                    foreach (TimingPointChange change in changes.Where(change =>
                                 change.Index && change.TimingPoint.Offset <= targetItem.Time &&
                                 change.TimingPoint.Offset >= latest))
                    {
                        next = change.TimingPoint.SampleIndex;
                        latest = change.TimingPoint.Offset;
                    }
                    point.SampleIndex = next;
                    targetItem.GiveHitsoundTimingPoint(point);
                    point.SampleIndex = native.SequenceEqual(GetResolvedSamplePaths(targetItem, mode, mapDirectory, firstSamples)) ? next : old;
                }
                targetItem.GiveHitsoundTimingPoint(point);
            }

            point.Offset = targetItem.Time;
            changes.Add(new TimingPointChange(
                point,
                sampleSet: holdSampleSet,
                index: holdIndex,
                volume: options.CopyVolumes));
        }
    }

    private static bool IsInsideShiftedSourceObject(
        HitObject source,
        double targetTime,
        double timingOffset) =>
        source.Time + timingOffset < targetTime &&
        source.EndTime + timingOffset > targetTime;

    private static TimingPoint ShiftTimingPoint(TimingPoint point, double timingOffset)
    {
        TimingPoint shifted = point.Copy();
        shifted.Offset += timingOffset;
        return shifted;
    }

    private static HitsoundSampleAssignment? TryAssign(
        TimelineObject source,
        string role,
        GameMode mode,
        string sourceMapDirectory,
        IReadOnlyDictionary<string, string> sourceSamples,
        HitsoundCopierOptions options,
        Func<HitsoundSampleAssignmentRequest, HitsoundSampleAssignment?>? assignSample)
    {
        if (assignSample is null)
        {
            return null;
        }

        List<string> filenames = source.GetPlayingFilenames(mode, false)
            .Select(filename => CanonicalPath(sourceMapDirectory, filename, sourceSamples))
            .ToList();
        if (filenames.Count == 0)
        {
            return null;
        }

        return assignSample(new HitsoundSampleAssignmentRequest(
            filenames,
            role,
            source.FenoSampleSet,
            options.StartIndex));
    }

    private static void AddCustomTimingChanges(
        List<TimingPointChange> changes,
        TimelineObject source,
        double targetTime,
        HitsoundSampleAssignment assignment,
        HitsoundCopierOptions options)
    {
        TimingPoint point = source.HitsoundTimingPoint.Copy();
        point.Offset = targetTime;
        point.SampleIndex = assignment.Index;
        point.SampleSet = assignment.SampleSet;
        point.Volume = source.FenoSampleVolume;
        changes.Add(new TimingPointChange(
            point,
            sampleSet: options.CopySampleSets,
            index: options.CopySampleSets,
            volume: options.CopyVolumes));
        TimingPoint revert = source.HitsoundTimingPoint.Copy();
        revert.Offset = targetTime + 5;
        changes.Add(new TimingPointChange(
            revert,
            sampleSet: options.CopySampleSets,
            index: options.CopySampleSets,
            volume: options.CopyVolumes));
    }

    private static TimelineObject? FindMatch(
        TimelineObject source,
        Timeline target,
        double offset,
        double leniency)
    {
        double sourceTime = source.Time + offset;
        return target.GetNearestTlo(sourceTime, true) is { } targetItem &&
               Math.Abs(Math.Round(sourceTime) - Math.Round(targetItem.Time)) <= leniency
            ? targetItem
            : null;
    }

    private static void CopyHitsounds(
        HitsoundCopierOptions options,
        TimelineObject source,
        TimelineObject target)
    {
        target.SampleSet = source.SampleSet;
        target.AdditionSet = source.AdditionSet;
        target.Normal = source.Normal;
        target.Whistle = source.Whistle;
        target.Finish = source.Finish;
        target.Clap = source.Clap;
        if (target.CanCustoms)
        {
            target.CustomIndex = source.CustomIndex;
            target.SampleVolume = source.SampleVolume;
            target.Filename = source.Filename;
        }
        if (target.IsSliderHead && source.IsSliderHead && options.CopyBodyHitsounds)
        {
            target.Origin.Hitsounds = source.Origin.Hitsounds;
            target.Origin.SampleSet = source.Origin.SampleSet;
            target.Origin.AdditionSet = source.Origin.AdditionSet;
        }
        target.HitsoundsToOrigin();
        target.CanCopy = false;
    }

    private static void ResetHitObjectHitsounds(Beatmap beatmap)
    {
        foreach (HitObject item in beatmap.HitObjects)
        {
            item.Hitsounds = 0;
            item.SampleSet = SampleSet.None;
            item.AdditionSet = SampleSet.None;
            item.CustomIndex = 0;
            item.SampleVolume = 0;
            item.Filename = string.Empty;
            if (item.IsSlider)
            {
                item.EdgeHitsounds = item.EdgeHitsounds.Select(_ => 0).ToList();
                item.EdgeSampleSets = item.EdgeSampleSets.Select(_ => SampleSet.None).ToList();
                item.EdgeAdditionSets = item.EdgeAdditionSets.Select(_ => SampleSet.None).ToList();
            }
        }
    }

    private static void RestorePreservedVolumes(
        Timeline timeline,
        Beatmap beatmap,
        List<double> muteTimes,
        double firstTime)
    {
        List<TimingPointChange> changes = [];
        foreach (TimelineObject item in timeline.TimelineObjects.Where(item =>
                     Math.Abs(item.SampleVolume) < Precision.DoubleEpsilon &&
                     Precision.AlmostBigger(item.Time, firstTime)))
        {
            TimingPoint point = item.HitsoundTimingPoint.Copy();
            point.Offset = item.Time;
            point.Volume = muteTimes.Contains(item.Time) ? 5 : item.FenoSampleVolume;
            changes.Add(new TimingPointChange(point, volume: true));
        }
        TimingPointChange.Apply(beatmap.BeatmapTiming, changes);
    }

    private static bool FilterMute(TimelineObject item, Beatmap beatmap, HitsoundCopierOptions options)
    {
        if (!item.CanCopy || !(item.IsSliderEnd || item.IsSpinnerEnd) || item.Repeat != 1 ||
            item.Whistle || item.Finish || item.Clap ||
            options.MutedSampleSet != SampleSet.None && item.FenoSampleSet != options.MutedSampleSet)
        {
            return false;
        }

        IBeatDivisor[] all = options.BeatDivisors.Concat(options.MutedDivisors).ToArray();
        TimingPoint timingPoint = beatmap.BeatmapTiming.GetRedlineAtTime(item.Time - 1);
        double snapped = beatmap.BeatmapTiming.Resnap(item.Time, all, false, tp: timingPoint);
        double beats = (snapped - timingPoint.Offset) / timingPoint.MpB;
        List<IBeatDivisor> possible = all.Where(divisor =>
                Precision.AlmostEquals(beats % divisor.GetValue(), 0) ||
                Precision.AlmostEquals(beats % divisor.GetValue(), divisor.GetValue()))
            .ToList();
        return possible.Count > 0 &&
               !possible.TakeWhile(divisor => !options.MutedDivisors.Contains(divisor)).Any() &&
               Precision.AlmostBigger(item.Origin.TemporalLength, options.MinLength * timingPoint.MpB);
    }

    private static bool FindSliderTickInRange(
        Beatmap beatmap,
        double start,
        double end,
        out double time,
        out HitObject? slider)
    {
        double tickRate = beatmap.Difficulty.TryGetValue("SliderTickRate", out TValue value)
            ? value.DoubleValue
            : 1;
        foreach (HitObject item in beatmap.HitObjects.Where(item => item.IsSlider &&
                     !double.IsNaN(item.SliderVelocity) &&
                     (item.Time < end && item.EndTime > start)))
        {
            foreach (double tick in item.GetSliderTickTimes(tickRate))
            {
                if (tick >= start && tick <= end)
                {
                    time = tick;
                    slider = item;
                    return true;
                }
            }
        }
        time = -1;
        slider = null;
        return false;
    }

    private static bool FindSliderAtTime(Beatmap beatmap, double time, out HitObject? slider)
    {
        slider = beatmap.HitObjects.FirstOrDefault(item => item.IsSlider &&
                                                            item.Time < time && item.EndTime > time);
        return slider is not null;
    }

    private static string CanonicalPath(
        string mapDirectory,
        string filename,
        IReadOnlyDictionary<string, string> firstSamples)
    {
        string path = Path.IsPathRooted(filename)
            ? filename
            : Path.Combine(mapDirectory, filename);
        string extensionless = Path.Combine(
            Path.GetDirectoryName(path) ?? mapDirectory,
            Path.GetFileNameWithoutExtension(path));
        return firstSamples.TryGetValue(extensionless, out string first) ? first : path;
    }

    private static IReadOnlyList<string> GetResolvedSamplePaths(
        TimelineObject item,
        GameMode mode,
        string mapDirectory,
        IReadOnlyDictionary<string, string> firstSamples)
    {
        return item.GetPlayingFilenames(mode)
            .Select(filename => CanonicalPath(mapDirectory, filename, firstSamples))
            .ToList();
    }
}
