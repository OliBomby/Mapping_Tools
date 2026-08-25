using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;

namespace Mapping_Tools.Core.Tools.HitsoundCopier;

/// <summary>
///     Applies Hitsound Copier's timeline transformation without reading or writing audio files.
/// </summary>
public static class HitsoundCopierEngine
{
    /// <summary>
    ///     Copies source hitsounds, inherited sample settings, storyboard samples, and eligible
    ///     muted edges into a target beatmap.
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
        HitsoundCopierEngineOptions options,
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
        if (options.CopyMode is not 0 and not 1) throw new ArgumentException("Hitsound Copier received an unknown copy mode.", nameof(options));
        if (options.TemporalLeniency < 0 || !double.IsFinite(options.TemporalLeniency))
            throw new ArgumentOutOfRangeException(nameof(options), "Temporal leniency must be finite and non-negative.");

        Dictionary<string, string> samples = firstSamples is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(firstSamples, StringComparer.OrdinalIgnoreCase);
        string sourceDirectory = string.IsNullOrWhiteSpace(sourceMapDirectory)
            ? mapDirectory
            : sourceMapDirectory;
        var sourceSamplePaths = sourceSamples is null
            ? samples
            : new Dictionary<string, string>(sourceSamples, StringComparer.OrdinalIgnoreCase);
        var generatedSamples = sampleSchema ?? new SampleSchema();
        int matchedCount = 0;
        int generatedCount = 0;
        int mutedCount = 0;

        var targetTimeline = target.GetTimeline();
        Timeline sourceTimeline = new(sourceObjects.ToList(), source.BeatmapTiming);
        targetTimeline.GiveTimingPoints(target.BeatmapTiming);
        sourceTimeline.GiveTimingPoints(source.BeatmapTiming);
        if (options.CopyBodyHitsounds || options.CopyStoryboardedSamples || options.MuteSliderends)
        {
            target.GiveObjectsGreenlines();
            if (sourceObjects.Count > 0 && source.BeatmapTiming.TimingPoints.Count > 0) source.GiveObjectsGreenlines();
        }

        // Get the first timing point time of both beatmaps, so we can prevent hitobjects from adding greenlines before the first redline
        double sourceFirstTime = source.BeatmapTiming.TimingPoints.Count > 0
            ? source.BeatmapTiming.TimingPoints[0].Offset
            : double.PositiveInfinity;
        double firstTime = Math.Min(
            sourceFirstTime + options.TimingOffset,
            target.BeatmapTiming.TimingPoints.Count > 0
                ? target.BeatmapTiming.TimingPoints[0].Offset
                : double.PositiveInfinity);
        // Save tlo times where timingpoint volume is 5%
        var preservedMuteTimes = options.CopyVolumes && options.AlwaysPreserve5Volume
            ? targetTimeline.TimelineObjects
                .Where(item => Math.Abs(item.SampleVolume) < Precision.DOUBLE_EPSILON && Math.Abs(item.FenoSampleVolume - 5) < Precision.DOUBLE_EPSILON)
                .Select(item => item.Time)
                .ToList()
            : null;

        if (options.CopyMode == 0)
        {
            // Every defined hitsound and sampleset on hitsound gets copied to their copyTo destination
            // Timelines
            if (options.CopyHitsounds)
            {
                ResetHitObjectHitsounds(target);
                targetTimeline = target.GetTimeline();
                targetTimeline.GiveTimingPoints(target.BeatmapTiming);
                matchedCount += CopyMatchingHitsounds(options, sourceTimeline, targetTimeline);
            }

            // Volumes and samplesets and customindices greenlines get copied with timingpointchanges and allafter enabled
            var changes = source.BeatmapTiming.TimingPoints
                .Select(point => new TimingPointChange(
                    ShiftTimingPoint(point, options.TimingOffset),
                    sampleSet: options.CopySampleSets,
                    index: options.CopySampleSets,
                    volume: options.CopyVolumes))
                .ToList();
            if (double.IsFinite(sourceFirstTime) && double.IsFinite(firstTime))
            {
                // Add a timing point at the first time too. In case beatmapTo starts earlier than beatmapFrom
                var first = source.BeatmapTiming.GetTimingPointAtTime(firstTime).Copy();
                first.Offset = firstTime;
                changes.Add(new TimingPointChange(
                    first,
                    sampleSet: options.CopySampleSets,
                    index: options.CopySampleSets,
                    volume: options.CopyVolumes));
            }

            // Apply the timingpoint changes
            TimingPointChange.Apply(target.BeatmapTiming, changes, true);
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            if (preservedMuteTimes is not null)
                // Return 5% volume to tlo that had it before
                RestorePreservedVolumes(targetTimeline, target, preservedMuteTimes, firstTime);
        }
        else
        {
            // Smarty mode
            // Copy the defined hitsounds literally (not feno, that will be reserved for cleaner). Only the tlo that have been defined by copyFrom get overwritten.
            List<TimingPointChange> changes = [];
            var mode = (GameMode)target.General["Mode"].IntValue;
            if (options.CopyHitsounds)
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

            if (options.CopyBodyHitsounds)
            {
                // Remove timingpoints in beatmapTo that are in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                foreach (var point in target.HitObjects
                             .SelectMany(item => item.BodyHitsounds)
                             .Where(point => !point.Uninherited && sourceObjects.Any(item => IsInsideShiftedSourceObject(item, point.Offset, options.TimingOffset)))
                             .ToList())
                    target.BeatmapTiming.Remove(point);

                // Get timingpointschanges for every timingpoint from beatmapFrom that is in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
                changes.AddRange(sourceObjects
                    .SelectMany(item => item.BodyHitsounds)
                    .Where(point => target.HitObjects.Any(item => item.Time < point.Offset + options.TimingOffset && item.EndTime > point.Offset + options.TimingOffset))
                    .Select(point => new TimingPointChange(
                        ShiftTimingPoint(point, options.TimingOffset),
                        sampleSet: options.CopySampleSets,
                        index: options.CopySampleSets,
                        volume: options.CopyVolumes)));
            }

            // Apply the timingpoint changes
            TimingPointChange.Apply(target.BeatmapTiming, changes);
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            if (preservedMuteTimes is not null)
                // Return 5% volume to tlo that had it before
                RestorePreservedVolumes(targetTimeline, target, preservedMuteTimes, firstTime);
        }

        if (options.CopyStoryboardedSamples)
        {
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            if (options.CopyMode == 0) target.StoryboardSoundSamples.Clear();

            HashSet<StoryboardSoundSample> existing = new(target.StoryboardSoundSamples);
            var mode = (GameMode)target.General["Mode"].IntValue;
            foreach (var sample in source.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double sampleTime = sample.StartTime + options.TimingOffset;
                if (options.IgnoreHitsoundSatisfiedSamples)
                {
                    var playing = targetTimeline.TimelineObjects
                        .Where(item => Math.Abs(item.Time - sampleTime) <= options.TemporalLeniency)
                        .SelectMany(item => GetResolvedSamplePaths(item, mode, mapDirectory, samples))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    string storyboardPath = CanonicalPath(mapDirectory, sample.FilePath, samples);
                    if (playing.Contains(storyboardPath)) continue;
                }

                if (options.IgnoreWheneverHitsound
                    && targetTimeline.TimelineObjects.Any(item =>
                        Math.Abs(item.Time - sampleTime) <= options.TemporalLeniency))
                    continue;

                StoryboardSoundSample copy = new(
                    sampleTime,
                    sample.Layer,
                    sample.FilePath,
                    sample.Volume);
                if (!existing.Contains(copy))
                {
                    // Add the StoryboardSoundSamples from beatmapFrom to beatmapTo if it doesn't already have the sample
                    target.StoryboardSoundSamples.Add(copy);
                    existing.Add(copy);
                }
            }

            // Sort the storyboarded samples
            target.StoryboardSoundSamples.Sort();
        }

        if (options.MuteSliderends)
        {
            target.GiveObjectsGreenlines();
            targetTimeline.GiveTimingPoints(target.BeatmapTiming);
            List<TimingPointChange> changes = [];
            foreach (var item in targetTimeline.TimelineObjects
                         .Where(item => Precision.AlmostBigger(item.Time, firstTime)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var point = item.HitsoundTimingPoint.Copy();
                point.Offset = item.Time;
                if (FilterMute(item, target, options))
                {
                    // Set volume to 5%, remove all hitsounds, apply customindex and sampleset
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

                // Add timingpointschange to preserve index and volume and sampleset
                changes.Add(new TimingPointChange(
                    point,
                    sampleSet: true,
                    index: options.MutedIndex >= 0,
                    volume: true));
            }

            // Apply the timingpoint changes
            TimingPointChange.Apply(target.BeatmapTiming, changes);
        }

        return new HitsoundCopierApplyResult(matchedCount, generatedCount, mutedCount, generatedSamples);
    }

    private static int CopyMatchingHitsounds(
        HitsoundCopierEngineOptions options,
        Timeline source,
        Timeline target)
    {
        int count = 0;
        foreach (var sourceItem in source.TimelineObjects.Where(item => item.HasHitsound))
        {
            var targetItem = FindMatch(sourceItem, target, options.TimingOffset, options.TemporalLeniency);
            if (targetItem is not null)
            {
                // Copy to this tlo
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
        HitsoundCopierEngineOptions options,
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
        foreach (var sourceItem in source.TimelineObjects.Where(item => item.HasHitsound))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetItem = FindMatch(sourceItem, target, options.TimingOffset, options.TemporalLeniency);
            if (targetItem is not null)
            {
                // Copy to this tlo
                CopyHitsounds(options, sourceItem, targetItem);
                matchedCount++;
                if (Precision.AlmostBigger(targetItem.Time, firstTime))
                {
                    // Add timingpointschange to copy timingpoint hitsounds
                    var point = sourceItem.HitsoundTimingPoint.Copy();
                    point.Offset = targetItem.Time;
                    changes.Add(new TimingPointChange(
                        point,
                        sampleSet: options.CopySampleSets,
                        index: options.CopySampleSets,
                        volume: options.CopyVolumes));
                }
            }
            // Try to find a slider tick in range to copy the sample to instead.
            // This slider tick gets a custom sample and timingpoints change to imitate the copied hitsound.
            else if (options.CopyToSliderTicks
                     && FindSliderTickInRange(
                         targetBeatmap,
                         sourceItem.Time + options.TimingOffset - options.TemporalLeniency,
                         sourceItem.Time + options.TimingOffset + options.TemporalLeniency,
                         out double tickTime,
                         out var tickSlider)
                     && customSampledTimes.Add((int)tickTime))
            {
                var assignment = TryAssign(
                    sourceItem,
                    "slidertick",
                    mode,
                    sourceMapDirectory,
                    sourceSamples,
                    options,
                    assignSample);
                if (assignment is not null)
                {
                    // Add a new custom sample to this slider tick to represent the hitsounds
                    generatedSamples.MergeWith(assignment.Schema);
                    // Make sure the slider with the slider ticks uses auto sampleset so the customized greenlines control the hitsounds
                    tickSlider!.SampleSet = SampleSet.None;
                    // Add timingpointschange
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

        // Do the sliderslide hitsounds after because the ticks need to add sliderslides with strict indices.
        foreach (var sourceItem in sliderSlides)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!FindSliderAtTime(targetBeatmap, sourceItem.Time + options.TimingOffset, out var slider)
                || customSampledTimes.Contains((int)(sourceItem.Time + options.TimingOffset)))
                continue;

            var assignment = TryAssign(
                sourceItem,
                "sliderslide",
                mode,
                sourceMapDirectory,
                sourceSamples,
                options,
                assignSample);
            if (assignment is null) continue;

            // Add a new custom sample to this slider slide to represent the hitsounds
            generatedSamples.MergeWith(assignment.Schema);
            // Add timingpointschange
            AddCustomTimingChanges(changes, sourceItem, sourceItem.Time + options.TimingOffset, assignment, options);
            // Make sure the slider with the slider ticks uses auto sampleset so the customized greenlines control the hitsounds
            slider!.SampleSet = SampleSet.None;
            generatedCount += assignment.Schema.Count;
        }

        // Timingpointchange all the undefined tlo from copyFrom
        foreach (var targetItem in target.TimelineObjects)
        {
            if (!targetItem.CanCopy || !Precision.AlmostBigger(targetItem.Time, firstTime)) continue;

            var point = targetItem.HitsoundTimingPoint.Copy();
            bool holdSampleSet = options.CopySampleSets && targetItem.SampleSet == SampleSet.None;
            bool holdIndex = options.CopySampleSets && !(targetItem.CanCustoms && targetItem.CustomIndex != 0);
            // Dont hold indexes or sampleset if the sample it plays currently is the same as the sample it would play without conserving
            if (holdSampleSet || holdIndex)
            {
                var native = GetResolvedSamplePaths(targetItem, mode, mapDirectory, firstSamples);
                if (holdSampleSet)
                {
                    var old = targetItem.FenoSampleSet;
                    var next = old;
                    double latest = double.NegativeInfinity;
                    foreach (var change in changes.Where(change =>
                                 change.SampleSet && change.TimingPoint.Offset <= targetItem.Time && change.TimingPoint.Offset >= latest))
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
                    foreach (var change in changes.Where(change =>
                                 change.Index && change.TimingPoint.Offset <= targetItem.Time && change.TimingPoint.Offset >= latest))
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
        double timingOffset)
    {
        return source.Time + timingOffset < targetTime && source.EndTime + timingOffset > targetTime;
    }

    private static TimingPoint ShiftTimingPoint(TimingPoint point, double timingOffset)
    {
        var shifted = point.Copy();
        shifted.Offset += timingOffset;
        return shifted;
    }

    private static HitsoundSampleAssignment? TryAssign(
        TimelineObject source,
        string role,
        GameMode mode,
        string sourceMapDirectory,
        IReadOnlyDictionary<string, string> sourceSamples,
        HitsoundCopierEngineOptions options,
        Func<HitsoundSampleAssignmentRequest, HitsoundSampleAssignment?>? assignSample)
    {
        if (assignSample is null) return null;

        var filenames = source.GetPlayingFilenames(mode, false)
            .Select(filename => CanonicalPath(sourceMapDirectory, filename, sourceSamples))
            .ToList();
        if (filenames.Count == 0) return null;

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
        HitsoundCopierEngineOptions options)
    {
        var point = source.HitsoundTimingPoint.Copy();
        point.Offset = targetTime;
        point.SampleIndex = assignment.Index;
        point.SampleSet = assignment.SampleSet;
        point.Volume = source.FenoSampleVolume;
        changes.Add(new TimingPointChange(
            point,
            sampleSet: options.CopySampleSets,
            index: options.CopySampleSets,
            volume: options.CopyVolumes));
        var revert = source.HitsoundTimingPoint.Copy();
        revert.Offset = targetTime + 5;
        // Add timingpointschange 5ms later to revert the stuff back to whatever it should be
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
        return target.GetNearestTlo(sourceTime, true) is { } targetItem && Math.Abs(Math.Round(sourceTime) - Math.Round(targetItem.Time)) <= leniency
            ? targetItem
            : null;
    }

    private static void CopyHitsounds(
        HitsoundCopierEngineOptions options,
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

        // Copy sliderbody hitsounds
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
        foreach (var item in beatmap.HitObjects)
        {
            // Remove all hitsounds
            item.Hitsounds = 0;
            item.SampleSet = SampleSet.None;
            item.AdditionSet = SampleSet.None;
            item.CustomIndex = 0;
            item.SampleVolume = 0;
            item.Filename = string.Empty;
            if (item.IsSlider)
            {
                // Remove edge hitsounds
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
        // Exclude objects which use their own sample volume property instead
        foreach (var item in timeline.TimelineObjects.Where(item =>
                     Math.Abs(item.SampleVolume) < Precision.DOUBLE_EPSILON && Precision.AlmostBigger(item.Time, firstTime)))
        {
            var point = item.HitsoundTimingPoint.Copy();
            point.Offset = item.Time;
            point.Volume = muteTimes.Contains(item.Time) ? 5 : item.FenoSampleVolume;
            changes.Add(new TimingPointChange(point, volume: true));
        }

        TimingPointChange.Apply(beatmap.BeatmapTiming, changes);
    }

    private static bool FilterMute(TimelineObject item, Beatmap beatmap, HitsoundCopierEngineOptions options)
    {
        // Check whether it's defined
        if (!item.CanCopy
            || !(item.IsSliderEnd || item.IsSpinnerEnd)
            || item.Repeat != 1
            || item.Whistle
            || item.Finish
            || item.Clap
            || options.MutedSampleSet != SampleSet.None && item.FenoSampleSet != options.MutedSampleSet)
            return false;

        // Check filter snap
        var all = options.BeatDivisors.Concat(options.MutedDivisors).ToArray();
        var timingPoint = beatmap.BeatmapTiming.GetRedlineAtTime(item.Time - 1);
        double snapped = beatmap.BeatmapTiming.Resnap(item.Time, all, false, timingPoint);
        double beats = (snapped - timingPoint.Offset) / timingPoint.MpB;
        // Get all the divisors which the sliderend could possibly be snapped to
        var possible = all.Where(divisor =>
                Precision.AlmostEquals(beats % divisor.GetValue(), 0) || Precision.AlmostEquals(beats % divisor.GetValue(), divisor.GetValue()))
            .ToList();
        // Make sure all the possible beat divisors of lower priority are in the muted category
        // Check filter temporal length
        return possible.Count > 0
               && !possible.TakeWhile(divisor => !options.MutedDivisors.Contains(divisor)).Any()
               && Precision.AlmostBigger(item.Origin.TemporalLength, options.MinLength * timingPoint.MpB);
    }

    private static bool FindSliderTickInRange(
        Beatmap beatmap,
        double start,
        double end,
        out double time,
        out HitObject? slider)
    {
        double tickRate = beatmap.Difficulty.TryGetValue("SliderTickRate", out var value)
            ? value.DoubleValue
            : 1;
        // Check all sliders in range and exclude those which have NaN SV, because those dont have slider ticks
        foreach (var item in beatmap.HitObjects.Where(item => item.IsSlider && !double.IsNaN(item.SliderVelocity) && item.Time < end && item.EndTime > start))
        foreach (double tick in item.GetSliderTickTimes(tickRate))
            if (tick >= start && tick <= end)
            {
                time = tick;
                slider = item;
                return true;
            }

        time = -1;
        slider = null;
        return false;
    }

    private static bool FindSliderAtTime(Beatmap beatmap, double time, out HitObject? slider)
    {
        slider = beatmap.HitObjects.FirstOrDefault(item => item.IsSlider && item.Time < time && item.EndTime > time);
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
