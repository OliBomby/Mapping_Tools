using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Events;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.PropertyTransformer;

/// <summary>
///     Applies Property Transformer changes to parsed beatmaps and storyboards.
/// </summary>
public static class PropertyTransformerEngine
{
    /// <summary>
    ///     Transforms timing points, hit objects, bookmarks, and beatmap events in place.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap to transform.</param>
    /// <param name="options">The multipliers, offsets, clipping, and filters to apply.</param>
    /// <param name="progress">Optional progress reporting for the feature stages.</param>
    /// <param name="cancellationToken">Cancels between transformation stages and items.</param>
    public static void Apply(
        Beatmap beatmap,
        PropertyTransformerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(options);

        List<TimingPointChange> timingPointChanges = [];

        foreach (var timingPoint in beatmap.BeatmapTiming.TimingPoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Offset
            TransformProperty(
                options,
                options.TimingpointOffsetMultiplier,
                options.TimingpointOffsetOffset,
                () => timingPoint.Offset,
                value => timingPoint.Offset = value,
                timingPoint.Offset,
                round: beatmap.Version < 128);
            if (timingPoint.Uninherited)
                // BPM
                TransformProperty(
                    options,
                    options.TimingpointBpmMultiplier,
                    options.TimingpointBpmOffset,
                    timingPoint.GetBpm,
                    timingPoint.SetBpm,
                    timingPoint.Offset,
                    15,
                    10000);

            // Slider Velocity
            TransformProperty(
                options,
                options.TimingpointSvMultiplier,
                options.TimingpointSvOffset,
                () => beatmap.BeatmapTiming.GetSvMultiplierAtTime(timingPoint.Offset),
                value =>
                {
                    var changed = timingPoint.Copy();
                    changed.MpB = -100 / value;
                    timingPointChanges.Add(new TimingPointChange(
                        changed,
                        true,
                        fuzziness: 0.4));
                },
                timingPoint.Offset,
                0.1,
                10);
            // Index
            TransformProperty(
                options,
                options.TimingpointIndexMultiplier,
                options.TimingpointIndexOffset,
                () => timingPoint.SampleIndex,
                value => timingPoint.SampleIndex = (int)value,
                timingPoint.Offset,
                0,
                int.MaxValue,
                true);
            // Volume
            TransformProperty(
                options,
                options.TimingpointVolumeMultiplier,
                options.TimingpointVolumeOffset,
                () => timingPoint.Volume,
                value => timingPoint.Volume = (int)value,
                timingPoint.Offset,
                5,
                100,
                true);
        }

        Report(progress, 20);

        if (options.HitObjectTimeMultiplier != 1 || options.HitObjectTimeOffset != 0)
            // Hitobject time
            foreach (var hitObject in beatmap.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Get the end time early because the start time gets modified
                double oldEndTime = hitObject.GetEndTime(false);
                // Transform start time of hitobject
                TransformProperty(
                    options,
                    options.HitObjectTimeMultiplier,
                    options.HitObjectTimeOffset,
                    () => hitObject.Time,
                    value => hitObject.Time = value,
                    hitObject.Time,
                    round: beatmap.Version < 128);
                if (hitObject.IsHoldNote || hitObject.IsSpinner)
                    // Transform end time of hold notes and spinner
                    TransformProperty(
                        options,
                        options.HitObjectTimeMultiplier,
                        options.HitObjectTimeOffset,
                        () => oldEndTime,
                        value => hitObject.EndTime = value,
                        oldEndTime,
                        round: beatmap.Version < 128);
            }

        Report(progress, 25);

        if (options.HitObjectVolumeMultiplier != 1 || options.HitObjectVolumeOffset != 0)
            // Hitobject volume
            foreach (var hitObject in beatmap.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.HitObjectVolumeMultiplier,
                    options.HitObjectVolumeOffset,
                    () => hitObject.SampleVolume,
                    value => hitObject.SampleVolume = value,
                    hitObject.Time,
                    0,
                    100,
                    true);
            }

        Report(progress, 30);

        if (options.BookmarkTimeMultiplier != 1 || options.BookmarkTimeOffset != 0)
            // Bookmark time
            beatmap.SetBookmarks(beatmap.GetBookmarks()
                .Select(bookmark => PassesFilter(options, bookmark, bookmark)
                    ? beatmap.Version < 128
                        ? Math.Round(bookmark * options.BookmarkTimeMultiplier + options.BookmarkTimeOffset)
                        : bookmark * options.BookmarkTimeMultiplier + options.BookmarkTimeOffset
                    : bookmark)
                .ToList());

        Report(progress, 40);

        IEnumerable<Event> beatmapEvents = beatmap.StoryboardLayerBackground
            .Concat(beatmap.StoryboardLayerFail)
            .Concat(beatmap.StoryboardLayerPass)
            .Concat(beatmap.StoryboardLayerForeground)
            .Concat(beatmap.StoryboardLayerOverlay);
        if (options.SbEventTimeMultiplier != 1 || options.SbEventTimeOffset != 0)
            // Storyboarded event time
            foreach (var current in beatmapEvents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformEventTime(
                    options,
                    beatmap,
                    current,
                    options.SbEventTimeMultiplier,
                    options.SbEventTimeOffset);
            }

        Report(progress, 50);

        if (options.SbSampleTimeMultiplier != 1 || options.SbSampleTimeOffset != 0)
            // Storyboarded sample time
            foreach (var sample in beatmap.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.SbSampleTimeMultiplier,
                    options.SbSampleTimeOffset,
                    () => sample.StartTime,
                    value => sample.StartTime = value,
                    sample.StartTime,
                    round: beatmap.Version < 128);
            }

        Report(progress, 55);

        if (options.SbSampleVolumeMultiplier != 1 || options.SbSampleVolumeOffset != 0)
            // Storyboarded sample volume
            foreach (var sample in beatmap.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.SbSampleVolumeMultiplier,
                    options.SbSampleVolumeOffset,
                    () => sample.Volume,
                    value => sample.Volume = value,
                    sample.StartTime,
                    8,
                    100,
                    true);
            }

        Report(progress, 60);

        if (options.BreakTimeMultiplier != 1 || options.BreakTimeOffset != 0)
            // Break time
            foreach (var breakPeriod in beatmap.BreakPeriods)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.BreakTimeMultiplier,
                    options.BreakTimeOffset,
                    () => breakPeriod.StartTime,
                    value => breakPeriod.StartTime = value,
                    breakPeriod.StartTime,
                    round: beatmap.Version < 128);
                TransformProperty(
                    options,
                    options.BreakTimeMultiplier,
                    options.BreakTimeOffset,
                    () => breakPeriod.EndTime,
                    value => breakPeriod.EndTime = value,
                    breakPeriod.EndTime,
                    round: beatmap.Version < 128);
            }

        Report(progress, 70);

        if (options.VideoTimeMultiplier != 1 || options.VideoTimeOffset != 0)
            // Video start time
            foreach (var current in beatmap.BackgroundAndVideoEvents)
            {
                if (current is not Video video) continue;

                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.VideoTimeMultiplier,
                    options.VideoTimeOffset,
                    () => video.StartTime,
                    value => video.StartTime = value,
                    video.StartTime,
                    round: beatmap.Version < 128);
            }

        Report(progress, 80);

        if (options.PreviewTimeMultiplier != 1 || options.PreviewTimeOffset != 0)
            if (beatmap.General.ContainsKey("PreviewTime") && beatmap.General["PreviewTime"].IntValue != -1)
            {
                // Preview point time
                double previewTime = beatmap.General["PreviewTime"].DoubleValue;
                TransformProperty(
                    options,
                    options.PreviewTimeMultiplier,
                    options.PreviewTimeOffset,
                    () => previewTime,
                    value => beatmap.General["PreviewTime"].SetDouble(value),
                    previewTime,
                    round: beatmap.Version < 128);
            }

        Report(progress, 90);
        TimingPointChange.Apply(beatmap.BeatmapTiming, timingPointChanges);
        Report(progress, 100);
    }

    /// <summary>
    ///     Transforms storyboard events, storyboard samples, and video start times in place.
    /// </summary>
    /// <param name="storyboard">The mutable storyboard to transform.</param>
    /// <param name="options">The multipliers, offsets, clipping, and filters to apply.</param>
    /// <param name="progress">Optional progress reporting for the feature stages.</param>
    /// <param name="cancellationToken">Cancels between transformation stages and items.</param>
    public static void Apply(
        StoryBoard storyboard,
        PropertyTransformerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storyboard);
        ArgumentNullException.ThrowIfNull(options);

        IEnumerable<Event> events = storyboard.StoryboardLayerBackground
            .Concat(storyboard.StoryboardLayerFail)
            .Concat(storyboard.StoryboardLayerPass)
            .Concat(storyboard.StoryboardLayerForeground)
            .Concat(storyboard.StoryboardLayerOverlay);
        // Storyboarded event time
        if (options.SbEventTimeMultiplier != 1 || options.SbEventTimeOffset != 0)
            foreach (var current in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformEventTime(
                    options,
                    null,
                    current,
                    options.SbEventTimeMultiplier,
                    options.SbEventTimeOffset);
            }

        Report(progress, 50);

        if (options.SbSampleTimeMultiplier != 1 || options.SbSampleTimeOffset != 0)
            // Storyboarded sample time
            foreach (var sample in storyboard.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.SbSampleTimeMultiplier,
                    options.SbSampleTimeOffset,
                    () => sample.StartTime,
                    value => sample.StartTime = value,
                    sample.StartTime,
                    round: true);
            }

        Report(progress, 60);

        if (options.SbSampleVolumeMultiplier != 1 || options.SbSampleVolumeOffset != 0)
            // Storyboarded sample volume
            foreach (var sample in storyboard.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.SbSampleVolumeMultiplier,
                    options.SbSampleVolumeOffset,
                    () => sample.Volume,
                    value => sample.Volume = value,
                    sample.StartTime,
                    8,
                    100,
                    true);
            }

        Report(progress, 70);

        if (options.VideoTimeMultiplier != 1 || options.VideoTimeOffset != 0)
            // Video start time
            foreach (var current in storyboard.BackgroundAndVideoEvents)
            {
                if (current is not Video video) continue;

                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options,
                    options.VideoTimeMultiplier,
                    options.VideoTimeOffset,
                    () => video.StartTime,
                    value => video.StartTime = value,
                    video.StartTime,
                    round: true);
            }

        Report(progress, 90);
        Report(progress, 100);
    }

    private static bool PassesFilter(
        PropertyTransformerOptions options,
        double value,
        double time)
    {
        bool doFilterMatch = options.MatchFilter.Length > 0 && options.EnableFilters;
        bool doFilterUnmatch = options.UnmatchFilter.Length > 0 && options.EnableFilters;
        bool doFilterRange = (options.MinTimeFilter != -1 || options.MaxTimeFilter != -1) && options.EnableFilters && !double.IsNaN(time);
        double min = options.MinTimeFilter == -1
            ? double.NegativeInfinity
            : options.MinTimeFilter;
        double max = options.MaxTimeFilter == -1
            ? double.PositiveInfinity
            : options.MaxTimeFilter;

        return (!doFilterMatch || options.MatchFilter.Any(candidate => Precision.AlmostEquals(value, candidate, 0.001)))
               && (!doFilterUnmatch || !options.UnmatchFilter.Any(candidate => Precision.AlmostEquals(value, candidate, 0.001)))
               && (!doFilterRange || time >= min && time <= max);
    }

    private static void TransformProperty(
        PropertyTransformerOptions options,
        double multiplier,
        double offset,
        Func<double> getter,
        Action<double> setter,
        double time,
        double? min = null,
        double? max = null,
        bool round = false)
    {
        if (multiplier == 1 && offset == 0) return;

        double value = getter();
        if (!PassesFilter(options, value, time)) return;

        double newValue = value * multiplier + offset;
        if (round) newValue = Math.Round(newValue);

        if (options.ClipProperties)
        {
            if (min.HasValue) newValue = Math.Max(newValue, min.Value);

            if (max.HasValue) newValue = Math.Min(newValue, max.Value);
        }

        setter(newValue);
    }

    private static void TransformEventTime(
        PropertyTransformerOptions options,
        Beatmap? sourceBeatmap,
        Event current,
        double multiplier,
        double offset)
    {
        int version = sourceBeatmap?.Version ?? 14;
        // Commands under loops use relative time so they shouldn't get offset
        double eventOffset = current.ParentEvent is StandardLoop or TriggerLoop
            ? 0
            : offset;

        if (current is IHasStartTime start)
            TransformProperty(
                options,
                multiplier,
                eventOffset,
                () => start.StartTime,
                value => start.StartTime = value,
                start.StartTime,
                round: version < 128);

        if (current is IHasEndTime end)
            TransformProperty(
                options,
                multiplier,
                eventOffset,
                () => end.EndTime,
                value => end.EndTime = value,
                end.EndTime,
                round: version < 128);

        if (current is IHasDuration duration)
            // Just a duration doesnt have a time to filter
            TransformProperty(
                options,
                multiplier,
                0,
                () => duration.Duration,
                value => duration.Duration = value,
                double.NaN);

        // Recurse to also transform all the children events
        foreach (var child in current.ChildEvents) TransformEventTime(options, sourceBeatmap, child, multiplier, offset);
    }

    private static void Report(IProgress<double>? progress, double value)
    {
        progress?.Report(value);
    }
}
