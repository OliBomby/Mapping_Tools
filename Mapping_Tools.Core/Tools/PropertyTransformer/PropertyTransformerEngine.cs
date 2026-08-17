using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Events;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.MapCleaner;

namespace Mapping_Tools.Core.Tools.PropertyTransformer;

/// <summary>
/// Applies Property Transformer changes to parsed beatmaps and storyboards.
/// </summary>
public static class PropertyTransformerEngine
{
    /// <summary>
    /// Transforms timing points, hit objects, bookmarks, and beatmap events in place.
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

        bool Filter(double value, double time)
        {
            bool doFilterMatch = options.MatchFilter.Length > 0 && options.EnableFilters;
            bool doFilterUnmatch = options.UnmatchFilter.Length > 0 && options.EnableFilters;
            bool doFilterRange = (options.MinTimeFilter != -1 || options.MaxTimeFilter != -1) &&
                                 options.EnableFilters &&
                                 !double.IsNaN(time);
            double min = options.MinTimeFilter == -1
                ? double.NegativeInfinity
                : options.MinTimeFilter;
            double max = options.MaxTimeFilter == -1
                ? double.PositiveInfinity
                : options.MaxTimeFilter;

            return (!doFilterMatch || options.MatchFilter.Any(
                        candidate => Precision.AlmostEquals(value, candidate, 0.001))) &&
                   (!doFilterUnmatch || !options.UnmatchFilter.Any(
                        candidate => Precision.AlmostEquals(value, candidate, 0.001))) &&
                   (!doFilterRange || time >= min && time <= max);
        }

        void TransformProperty(
            double multiplier,
            double offset,
            Func<double> getter,
            Action<double> setter,
            double time,
            double? min = null,
            double? max = null,
            bool round = false)
        {
            if (multiplier == 1 && offset == 0)
            {
                return;
            }

            double value = getter();
            if (!Filter(value, time))
            {
                return;
            }

            double newValue = value * multiplier + offset;
            if (round)
            {
                newValue = Math.Round(newValue);
            }

            if (options.ClipProperties)
            {
                if (min.HasValue)
                {
                    newValue = Math.Max(newValue, min.Value);
                }

                if (max.HasValue)
                {
                    newValue = Math.Min(newValue, max.Value);
                }
            }

            setter(newValue);
        }

        void TransformEventTime(
            Beatmap? sourceBeatmap,
            Event current,
            double multiplier,
            double offset)
        {
            int version = sourceBeatmap?.Version ?? 14;
            bool relative = current.ParentEvent is StandardLoop or TriggerLoop;
            if (relative)
            {
                if (current is IHasStartTime start && Filter(start.StartTime, start.StartTime))
                {
                    start.StartTime = version < 128
                        ? Math.Round(start.StartTime * multiplier)
                        : start.StartTime * multiplier;
                }

                if (current is IHasEndTime end && Filter(end.EndTime, end.EndTime))
                {
                    end.EndTime = version < 128
                        ? Math.Round(end.EndTime * multiplier)
                        : end.EndTime * multiplier;
                }
            }
            else
            {
                if (current is IHasStartTime start && Filter(start.StartTime, start.StartTime))
                {
                    start.StartTime = version < 128
                        ? Math.Round(start.StartTime * multiplier + offset)
                        : start.StartTime * multiplier + offset;
                }

                if (current is IHasEndTime end && Filter(end.EndTime, end.EndTime))
                {
                    end.EndTime = version < 128
                        ? Math.Round(end.EndTime * multiplier + offset)
                        : end.EndTime * multiplier + offset;
                }
            }

            if (current is IHasDuration duration && Filter(duration.Duration, double.NaN))
            {
                duration.Duration *= multiplier;
            }

            foreach (Event child in current.ChildEvents)
            {
                TransformEventTime(sourceBeatmap, child, multiplier, offset);
            }
        }

        List<TimingPointChange> timingPointChanges = [];

        foreach (TimingPoint timingPoint in beatmap.BeatmapTiming.TimingPoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TransformProperty(
                options.TimingpointOffsetMultiplier,
                options.TimingpointOffsetOffset,
                () => timingPoint.Offset,
                value => timingPoint.Offset = value,
                timingPoint.Offset,
                round: beatmap.Version < 128);
            if (timingPoint.Uninherited)
            {
                TransformProperty(
                    options.TimingpointBpmMultiplier,
                    options.TimingpointBpmOffset,
                    timingPoint.GetBpm,
                    timingPoint.SetBpm,
                    timingPoint.Offset,
                    15,
                    10000);
            }

            TransformProperty(
                options.TimingpointSvMultiplier,
                options.TimingpointSvOffset,
                () => beatmap.BeatmapTiming.GetSvMultiplierAtTime(timingPoint.Offset),
                value =>
                {
                    TimingPoint changed = timingPoint.Copy();
                    changed.MpB = -100 / value;
                    timingPointChanges.Add(new TimingPointChange(
                        changed,
                        mpb: true,
                        fuzziness: 0.4));
                },
                timingPoint.Offset,
                0.1,
                10);
            TransformProperty(
                options.TimingpointIndexMultiplier,
                options.TimingpointIndexOffset,
                () => timingPoint.SampleIndex,
                value => timingPoint.SampleIndex = (int)value,
                timingPoint.Offset,
                0,
                int.MaxValue,
                round: true);
            TransformProperty(
                options.TimingpointVolumeMultiplier,
                options.TimingpointVolumeOffset,
                () => timingPoint.Volume,
                value => timingPoint.Volume = (int)value,
                timingPoint.Offset,
                5,
                100,
                round: true);
        }

        Report(progress, 20);

        if (options.HitObjectTimeMultiplier != 1 || options.HitObjectTimeOffset != 0)
        {
            foreach (HitObject hitObject in beatmap.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double oldEndTime = hitObject.GetEndTime(false);
                TransformProperty(
                    options.HitObjectTimeMultiplier,
                    options.HitObjectTimeOffset,
                    () => hitObject.Time,
                    value => hitObject.Time = value,
                    hitObject.Time,
                    round: beatmap.Version < 128);
                if (hitObject.IsHoldNote || hitObject.IsSpinner)
                {
                    TransformProperty(
                        options.HitObjectTimeMultiplier,
                        options.HitObjectTimeOffset,
                        () => oldEndTime,
                        value => hitObject.EndTime = value,
                        oldEndTime,
                        round: beatmap.Version < 128);
                }
            }
        }

        Report(progress, 25);

        if (options.HitObjectVolumeMultiplier != 1 || options.HitObjectVolumeOffset != 0)
        {
            foreach (HitObject hitObject in beatmap.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.HitObjectVolumeMultiplier,
                    options.HitObjectVolumeOffset,
                    () => hitObject.SampleVolume,
                    value => hitObject.SampleVolume = value,
                    hitObject.Time,
                    0,
                    100,
                    round: true);
            }
        }

        Report(progress, 30);

        if (options.BookmarkTimeMultiplier != 1 || options.BookmarkTimeOffset != 0)
        {
            beatmap.SetBookmarks(beatmap.GetBookmarks()
                .Select(bookmark => Filter(bookmark, bookmark)
                    ? beatmap.Version < 128
                        ? Math.Round(bookmark * options.BookmarkTimeMultiplier + options.BookmarkTimeOffset)
                        : bookmark * options.BookmarkTimeMultiplier + options.BookmarkTimeOffset
                    : bookmark)
                .ToList());
        }

        Report(progress, 40);

        IEnumerable<Event> beatmapEvents = beatmap.StoryboardLayerBackground
            .Concat(beatmap.StoryboardLayerFail)
            .Concat(beatmap.StoryboardLayerPass)
            .Concat(beatmap.StoryboardLayerForeground)
            .Concat(beatmap.StoryboardLayerOverlay);
        if (options.SbEventTimeMultiplier != 1 || options.SbEventTimeOffset != 0)
        {
            foreach (Event current in beatmapEvents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformEventTime(
                    beatmap,
                    current,
                    options.SbEventTimeMultiplier,
                    options.SbEventTimeOffset);
            }
        }

        Report(progress, 50);

        if (options.SbSampleTimeMultiplier != 1 || options.SbSampleTimeOffset != 0)
        {
            foreach (StoryboardSoundSample sample in beatmap.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.SbSampleTimeMultiplier,
                    options.SbSampleTimeOffset,
                    () => sample.StartTime,
                    value => sample.StartTime = value,
                    sample.StartTime,
                    round: beatmap.Version < 128);
            }
        }

        Report(progress, 55);

        if (options.SbSampleVolumeMultiplier != 1 || options.SbSampleVolumeOffset != 0)
        {
            foreach (StoryboardSoundSample sample in beatmap.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.SbSampleVolumeMultiplier,
                    options.SbSampleVolumeOffset,
                    () => sample.Volume,
                    value => sample.Volume = value,
                    sample.StartTime,
                    8,
                    100,
                    round: true);
            }
        }

        Report(progress, 60);

        if (options.BreakTimeMultiplier != 1 || options.BreakTimeOffset != 0)
        {
            foreach (Break breakPeriod in beatmap.BreakPeriods)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.BreakTimeMultiplier,
                    options.BreakTimeOffset,
                    () => breakPeriod.StartTime,
                    value => breakPeriod.StartTime = value,
                    breakPeriod.StartTime,
                    round: beatmap.Version < 128);
                TransformProperty(
                    options.BreakTimeMultiplier,
                    options.BreakTimeOffset,
                    () => breakPeriod.EndTime,
                    value => breakPeriod.EndTime = value,
                    breakPeriod.EndTime,
                    round: beatmap.Version < 128);
            }
        }

        Report(progress, 70);

        if (options.VideoTimeMultiplier != 1 || options.VideoTimeOffset != 0)
        {
            foreach (Event current in beatmap.BackgroundAndVideoEvents)
            {
                if (current is not Video video)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.VideoTimeMultiplier,
                    options.VideoTimeOffset,
                    () => video.StartTime,
                    value => video.StartTime = value,
                    video.StartTime,
                    round: beatmap.Version < 128);
            }
        }

        Report(progress, 80);

        if (options.PreviewTimeMultiplier != 1 || options.PreviewTimeOffset != 0)
        {
            if (beatmap.General.ContainsKey("PreviewTime") &&
                beatmap.General["PreviewTime"].IntValue != -1)
            {
                double previewTime = beatmap.General["PreviewTime"].DoubleValue;
                TransformProperty(
                    options.PreviewTimeMultiplier,
                    options.PreviewTimeOffset,
                    () => previewTime,
                    value => beatmap.General["PreviewTime"].SetDouble(value),
                    previewTime,
                    round: beatmap.Version < 128);
            }
        }

        Report(progress, 90);
        TimingPointChange.Apply(beatmap.BeatmapTiming, timingPointChanges);
        Report(progress, 100);

        void Report(IProgress<double>? reporter, double value) => reporter?.Report(value);
    }

    /// <summary>
    /// Transforms storyboard events, storyboard samples, and video start times in place.
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

        bool Filter(double value, double time)
        {
            bool doFilterMatch = options.MatchFilter.Length > 0 && options.EnableFilters;
            bool doFilterUnmatch = options.UnmatchFilter.Length > 0 && options.EnableFilters;
            bool doFilterRange = (options.MinTimeFilter != -1 || options.MaxTimeFilter != -1) &&
                                 options.EnableFilters &&
                                 !double.IsNaN(time);
            double min = options.MinTimeFilter == -1
                ? double.NegativeInfinity
                : options.MinTimeFilter;
            double max = options.MaxTimeFilter == -1
                ? double.PositiveInfinity
                : options.MaxTimeFilter;

            return (!doFilterMatch || options.MatchFilter.Any(
                        candidate => Precision.AlmostEquals(value, candidate, 0.001))) &&
                   (!doFilterUnmatch || !options.UnmatchFilter.Any(
                        candidate => Precision.AlmostEquals(value, candidate, 0.001))) &&
                   (!doFilterRange || time >= min && time <= max);
        }

        void TransformProperty(
            double multiplier,
            double offset,
            Func<double> getter,
            Action<double> setter,
            double time,
            double? min = null,
            double? max = null,
            bool round = false)
        {
            if (multiplier == 1 && offset == 0)
            {
                return;
            }

            double value = getter();
            if (!Filter(value, time))
            {
                return;
            }

            double newValue = value * multiplier + offset;
            if (round)
            {
                newValue = Math.Round(newValue);
            }

            if (options.ClipProperties)
            {
                if (min.HasValue)
                {
                    newValue = Math.Max(newValue, min.Value);
                }

                if (max.HasValue)
                {
                    newValue = Math.Min(newValue, max.Value);
                }
            }

            setter(newValue);
        }

        void TransformEventTime(Event current, double multiplier, double offset)
        {
            bool relative = current.ParentEvent is StandardLoop or TriggerLoop;
            if (relative)
            {
                if (current is IHasStartTime start && Filter(start.StartTime, start.StartTime))
                {
                    start.StartTime = Math.Round(start.StartTime * multiplier);
                }

                if (current is IHasEndTime end && Filter(end.EndTime, end.EndTime))
                {
                    end.EndTime = Math.Round(end.EndTime * multiplier);
                }
            }
            else
            {
                if (current is IHasStartTime start && Filter(start.StartTime, start.StartTime))
                {
                    start.StartTime = Math.Round(start.StartTime * multiplier + offset);
                }

                if (current is IHasEndTime end && Filter(end.EndTime, end.EndTime))
                {
                    end.EndTime = Math.Round(end.EndTime * multiplier + offset);
                }
            }

            if (current is IHasDuration duration && Filter(duration.Duration, double.NaN))
            {
                duration.Duration *= multiplier;
            }

            foreach (Event child in current.ChildEvents)
            {
                TransformEventTime(child, multiplier, offset);
            }
        }

        IEnumerable<Event> events = storyboard.StoryboardLayerBackground
            .Concat(storyboard.StoryboardLayerFail)
            .Concat(storyboard.StoryboardLayerPass)
            .Concat(storyboard.StoryboardLayerForeground)
            .Concat(storyboard.StoryboardLayerOverlay);
        if (options.SbEventTimeMultiplier != 1 || options.SbEventTimeOffset != 0)
        {
            foreach (Event current in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformEventTime(
                    current,
                    options.SbEventTimeMultiplier,
                    options.SbEventTimeOffset);
            }
        }

        Report(progress, 50);

        if (options.SbSampleTimeMultiplier != 1 || options.SbSampleTimeOffset != 0)
        {
            foreach (StoryboardSoundSample sample in storyboard.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.SbSampleTimeMultiplier,
                    options.SbSampleTimeOffset,
                    () => sample.StartTime,
                    value => sample.StartTime = value,
                    sample.StartTime,
                    round: true);
            }
        }

        Report(progress, 60);

        if (options.SbSampleVolumeMultiplier != 1 || options.SbSampleVolumeOffset != 0)
        {
            foreach (StoryboardSoundSample sample in storyboard.StoryboardSoundSamples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.SbSampleVolumeMultiplier,
                    options.SbSampleVolumeOffset,
                    () => sample.Volume,
                    value => sample.Volume = value,
                    sample.StartTime,
                    8,
                    100,
                    round: true);
            }
        }

        Report(progress, 70);

        if (options.VideoTimeMultiplier != 1 || options.VideoTimeOffset != 0)
        {
            foreach (Event current in storyboard.BackgroundAndVideoEvents)
            {
                if (current is not Video video)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                TransformProperty(
                    options.VideoTimeMultiplier,
                    options.VideoTimeOffset,
                    () => video.StartTime,
                    value => video.StartTime = value,
                    video.StartTime,
                    round: true);
            }
        }

        Report(progress, 90);
        Report(progress, 100);

        void Report(IProgress<double>? reporter, double value) => reporter?.Report(value);
    }
}
