using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.MapCleaner;

/// <summary>Rebuilds useful timing effects and optional snaps without filesystem or UI access.</summary>
public static class MapCleanerEngine
{
    /// <summary>Cleans one parsed beatmap without performing filesystem operations.</summary>
    /// <param name="beatmap">The mutable beatmap to clean.</param>
    /// <param name="options">The cleanup and resnapping choices.</param>
    /// <param name="mapDirectory">The mapset directory used to resolve samples.</param>
    /// <param name="firstSamples">The canonical sample paths discovered for the mapset.</param>
    /// <param name="progress">Optional cleanup completion reporting.</param>
    /// <param name="cancellationToken">Cancels cleanup.</param>
    /// <returns>The cleanup counts and timing-point timeline markers.</returns>
    public static MapCleanerResult Clean(
        Beatmap beatmap,
        MapCleanerOptions options,
        string mapDirectory = "",
        IReadOnlyDictionary<string, string>? firstSamples = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(options);
        if (options.BeatDivisors is null || options.BeatDivisors.Length == 0)
        {
            throw new ArgumentException("Select at least one beat divisor.", nameof(options));
        }

        firstSamples ??= new Dictionary<string, string>();
        Timing timing = beatmap.BeatmapTiming;
        GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;
        double circleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
        List<TimingPoint> original = timing.TimingPoints.Select(point => point.Copy()).ToList();
        Timeline timeline = beatmap.GetTimeline();
        int objectsResnapped = 0;

        List<TimingPoint> kiaiToggles = [];
        List<TimingPoint> svChanges = [];
        bool lastKiai = false;
        double lastSv = -100;
        foreach (TimingPoint point in timing.TimingPoints)
        {
            if (point.Kiai != lastKiai)
            {
                kiaiToggles.Add(point.Copy());
                lastKiai = point.Kiai;
            }
            if (point.Uninherited)
            {
                lastSv = -100;
            }
            else if (point.MpB != lastSv)
            {
                svChanges.Add(point.Copy());
                lastSv = point.MpB;
            }
        }
        Report(progress, 9);

        if (options.ResnapObjects)
        {
            foreach (HitObject hitObject in beatmap.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (hitObject.ResnapSelf(timing, options.BeatDivisors))
                {
                    objectsResnapped++;
                }

                hitObject.ResnapEnd(timing, options.BeatDivisors);
                hitObject.ResnapPosition(mode, circleSize);
            }
            foreach (TimingPoint point in kiaiToggles)
            {
                point.ResnapSelf(timing, options.BeatDivisors);
            }

            foreach (TimingPoint point in svChanges)
            {
                point.ResnapSelf(timing, options.BeatDivisors);
            }
            Report(progress, 36);
        }

        if (options.ResnapBookmarks)
        {
            beatmap.SetBookmarks(beatmap.GetBookmarks()
                .Select(bookmark => timing.Resnap(bookmark, options.BeatDivisors))
                .Distinct()
                .ToList());
        }
        Report(progress, 45);

        List<TimingPointChange> changes = [];
        foreach (TimingPoint point in timing.Redlines)
        {
            changes.Add(new TimingPointChange(point, mpb: true, meter: true,
                uninherited: true, omitFirstBarLine: true, fuzziness: Precision.DoubleEpsilon));
        }

        if (mode is GameMode.Taiko or GameMode.Mania)
        {
            foreach (TimingPoint point in svChanges)
            {
                changes.Add(new TimingPointChange(point, mpb: true, fuzziness: 0.4));
            }
        }

        foreach (TimingPoint point in kiaiToggles)
        {
            changes.Add(new TimingPointChange(point, kiai: true));
        }

        foreach (HitObject hitObject in beatmap.HitObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (hitObject.IsSlider)
            {
                TimingPoint sliderVelocity = hitObject.TimingPoint.Copy();
                sliderVelocity.Offset = hitObject.Time;
                sliderVelocity.MpB = hitObject.SliderVelocity;
                changes.Add(new TimingPointChange(sliderVelocity, mpb: true, fuzziness: 0.4));
            }
            if (options.RemoveHitsounds)
            {
                hitObject.ResetHitsounds();
                continue;
            }
            bool volume = hitObject.IsSlider && options.VolumeSliders ||
                          hitObject.IsSpinner && options.VolumeSpinners;
            bool sampleSet = hitObject.IsSlider && options.SampleSetSliders && hitObject.SampleSet == 0;
            bool index = hitObject.IsSlider && options.SampleSetSliders;
            bool sampleSetChanged = false;
            foreach (TimingPoint point in hitObject.BodyHitsounds)
            {
                if (point.Volume == 5 && options.RemoveMuting)
                {
                    volume = false;
                }

                changes.Add(new TimingPointChange(
                    point,
                    volume: volume,
                    index: index,
                    sampleSet: sampleSet));
                if (point.SampleSet != hitObject.HitsoundTimingPoint.SampleSet)
                {
                    sampleSetChanged = options.SampleSetSliders && hitObject.SampleSet == 0;
                }
            }

            if (hitObject.IsSlider && !sampleSetChanged && hitObject.SampleSet == 0)
            {
                hitObject.SampleSet = hitObject.HitsoundTimingPoint.SampleSet;
            }

            if (hitObject.IsSlider && sampleSetChanged)
            {
                TimingPoint point = hitObject.HitsoundTimingPoint.Copy();
                point.Offset = hitObject.Time;
                changes.Add(new TimingPointChange(point, sampleSet: true));
            }
        }
        Report(progress, 75);

        if (!options.RemoveHitsounds)
        {
            foreach (TimelineObject timelineObject in timeline.TimelineObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RewriteObjectHitsound(timelineObject, mode);
                if (timelineObject.Origin.AdditionSet == timelineObject.Origin.SampleSet)
                {
                    timelineObject.Origin.AdditionSet = 0;
                }

                if (!timelineObject.HasHitsound)
                {
                    continue;
                }

                TimingPoint point = timelineObject.HitsoundTimingPoint.Copy();
                bool unmute = timelineObject.FenoSampleVolume == 5 && options.RemoveMuting;
                bool mute = options.RemoveUnclickableHitsounds && !options.RemoveMuting &&
                            !(timelineObject.IsCircle || timelineObject.IsSliderHead || timelineObject.IsHoldnoteHead);
                bool index = !timelineObject.UsesFilename && !unmute;
                bool volume = !unmute;
                if (index && options.AnalyzeSamples && firstSamples.Count > 0)
                {
                    List<string> nativeSamples = timelineObject.GetFirstPlayingFilenames(
                        mode,
                        mapDirectory,
                        firstSamples.ToDictionary());
                    int oldIndex = timelineObject.FenoCustomIndex;
                    int newIndex = timelineObject.FenoCustomIndex;
                    double latest = double.NegativeInfinity;
                    foreach (TimingPointChange change in changes)
                    {
                        if (change.Index &&
                            change.TimingPoint.Offset <= timelineObject.Time &&
                            change.TimingPoint.Offset >= latest)
                        {
                            newIndex = change.TimingPoint.SampleIndex;
                            latest = change.TimingPoint.Offset;
                        }
                    }
                    point.SampleIndex = newIndex;
                    timelineObject.GiveHitsoundTimingPoint(point);
                    if (!nativeSamples.SequenceEqual(timelineObject.GetFirstPlayingFilenames(
                            mode,
                            mapDirectory,
                            firstSamples.ToDictionary())))
                    {
                        point.SampleIndex = oldIndex;
                    }
                    timelineObject.GiveHitsoundTimingPoint(point);
                }
                point.Offset = timelineObject.Time;
                point.SampleIndex = timelineObject.FenoCustomIndex;
                point.Volume = mute ? 5 : timelineObject.FenoSampleVolume;
                changes.Add(new TimingPointChange(point, volume: volume, index: index));
            }
        }
        Report(progress, 85);

        timing.Clear();
        TimingPointChange.Apply(timing, changes);
        beatmap.GiveObjectsGreenlines();
        Fix2BDoubleTaps(beatmap);
        Report(progress, 100);
        return Compare(original, timing.TimingPoints, objectsResnapped);
    }

    private static void RewriteObjectHitsound(TimelineObject item, GameMode mode)
    {
        if (item.Origin.IsCircle)
        {
            item.Origin.SampleSet = item.FenoSampleSet;
            item.Origin.AdditionSet = item.FenoAdditionSet;
            if (mode == GameMode.Mania)
            {
                item.Origin.CustomIndex = item.FenoCustomIndex;
                item.Origin.SampleVolume = item.FenoSampleVolume;
            }
        }
        else if (item.Origin.IsSlider)
        {
            item.Origin.EdgeHitsounds[item.Repeat] = item.GetHitsounds();
            item.Origin.EdgeSampleSets[item.Repeat] = item.FenoSampleSet;
            item.Origin.EdgeAdditionSets[item.Repeat] = item.FenoAdditionSet;
            if (item.Origin.EdgeAdditionSets[item.Repeat] == item.Origin.EdgeSampleSets[item.Repeat])
            {
                item.Origin.EdgeAdditionSets[item.Repeat] = 0;
            }
        }
        else if (item.Origin.IsSpinner && item.Repeat == 1)
        {
            item.Origin.SampleSet = item.FenoSampleSet;
            item.Origin.AdditionSet = item.FenoAdditionSet;
        }
        else if (item.Origin.IsHoldNote && item.Repeat == 0)
        {
            item.Origin.SampleSet = item.FenoSampleSet;
            item.Origin.AdditionSet = item.FenoAdditionSet;
            item.Origin.CustomIndex = item.FenoCustomIndex;
            item.Origin.SampleVolume = item.FenoSampleVolume;
        }
    }

    private static MapCleanerResult Compare(
        IReadOnlyList<TimingPoint> original,
        IReadOnlyList<TimingPoint> current,
        int resnapped)
    {
        double[] originalOffsets = original.Select(point => point.Offset).ToArray();
        double[] currentOffsets = current.Select(point => point.Offset).ToArray();
        double[] changed = original
            .Where(old =>
            {
                TimingPoint[] matching = current
                    .Where(now => Math.Abs(now.Offset - old.Offset) < Precision.DoubleEpsilon)
                    .ToArray();
                return matching.Length > 0 && matching.All(now => !old.Equals(now));
            })
            .Select(point => point.Offset).ToArray();
        return new MapCleanerResult(
            resnapped,
            0,
            original.Count - current.Count,
            currentOffsets.Except(originalOffsets).ToArray(),
            changed,
            originalOffsets.Except(currentOffsets).ToArray(),
            Math.Max(originalOffsets.LastOrDefault(), currentOffsets.LastOrDefault()));
    }

    private static void Fix2BDoubleTaps(Beatmap beatmap)
    {
        for (int index = 0; index < beatmap.HitObjects.Count - 1; index++)
        {
            HitObject first = beatmap.HitObjects[index];
            HitObject second = beatmap.HitObjects[index + 1];
            if (first.IsSlider &&
                second.IsCircle &&
                Precision.AlmostEquals(first.Time, second.Time))
            {
                (beatmap.HitObjects[index], beatmap.HitObjects[index + 1]) = (second, first);
            }
        }
    }

    private static void Report(IProgress<double>? progress, double value) => progress?.Report(value);
}

internal static class DictionaryCompatibility
{
    public static Dictionary<string, string> ToDictionary(this IReadOnlyDictionary<string, string> source) =>
        source as Dictionary<string, string> ?? source.ToDictionary(pair => pair.Key, pair => pair.Value);
}
