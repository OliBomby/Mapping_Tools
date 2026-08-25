using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.MapCleaner.Models;

namespace Mapping_Tools.Core.Tools.MapCleaner;

/// <summary>Rebuilds useful timing effects and optional snaps without filesystem or UI access.</summary>
public static class MapCleanerEngine
{
    /// <summary>Cleans one parsed beatmap without performing filesystem operations.</summary>
    /// <param name="beatmap">The mutable beatmap to clean.</param>
    /// <param name="options">The cleanup and resnapping choices.</param>
    /// <param name="mapDirectory">The mapset directory used to resolve samples.</param>
    /// <param name="firstSamples">The canonical sample paths discovered for the mapset.</param>
    /// <param name="progress">Optional normalized cleanup completion reporting.</param>
    /// <param name="cancellationToken">Cancels cleanup.</param>
    /// <returns>The cleanup counts and timing-point timeline markers.</returns>
    public static MapCleanerResult Clean(
        Beatmap beatmap,
        MapCleanerEngineOptions options,
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

        // Collect timeline objects before resnapping, so the timingpoints
        // are still valid and the tlo's get the correct hitsounds and offsets.
        // Resnapping of the hit objects will move the tlo's aswell
        Timeline timeline = beatmap.GetTimeline();
        int objectsResnapped = 0;

        // Collect Kiai toggles and SliderVelocity changes for mania/taiko
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
        Report(progress, 0.09);

        if (options.ResnapObjects)
        {
            // Resnap all objects
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

            // Resnap Kiai toggles
            foreach (TimingPoint point in kiaiToggles)
            {
                point.ResnapSelf(timing, options.BeatDivisors);
            }

            // Resnap SliderVelocity changes
            foreach (TimingPoint point in svChanges)
            {
                point.ResnapSelf(timing, options.BeatDivisors);
            }
            Report(progress, 0.36);
        }

        if (options.ResnapBookmarks)
        {
            // Resnap the bookmarks
            beatmap.SetBookmarks(beatmap.GetBookmarks()
                .Select(bookmark => timing.Resnap(bookmark, options.BeatDivisors))
                .Distinct()
                .ToList());
        }
        Report(progress, 0.45);

        // Make new timingpoints
        List<TimingPointChange> changes = [];

        // Add redlines
        foreach (TimingPoint point in timing.Redlines)
        {
            changes.Add(new TimingPointChange(point, mpb: true, meter: true,
                uninherited: true, omitFirstBarLine: true, fuzziness: Precision.DOUBLE_EPSILON));
        }

        if (mode is GameMode.Taiko or GameMode.Mania)
        {
            // Add SliderVelocity changes for taiko and mania
            foreach (TimingPoint point in svChanges)
            {
                changes.Add(new TimingPointChange(point, mpb: true, fuzziness: 0.4));
            }
        }

        // Add Kiai toggles
        foreach (TimingPoint point in kiaiToggles)
        {
            changes.Add(new TimingPointChange(point, kiai: true));
        }

        // Add Hitobject stuff
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
                // Skip adding hitsounds if we want to remove them
                hitObject.ResetHitsounds();
                continue;
            }

            // Body hitsounds
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
        Report(progress, 0.75);

        if (!options.RemoveHitsounds)
        {
            // Add timeline hitsounds
            foreach (TimelineObject timelineObject in timeline.TimelineObjects)
            {
                // Change the samplesets in the hitobjects
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

                // Add greenlines for custom indexes and volumes
                TimingPoint point = timelineObject.HitsoundTimingPoint.Copy();
                bool unmute = timelineObject.FenoSampleVolume == 5 && options.RemoveMuting;
                bool mute = options.RemoveUnclickableHitsounds && !options.RemoveMuting &&
                            !(timelineObject.IsCircle || timelineObject.IsSliderHead || timelineObject.IsHoldnoteHead);
                bool index = !timelineObject.UsesFilename && !unmute;
                bool volume = !unmute;
                if (index && options.AnalyzeSamples && firstSamples.Count > 0)
                {
                    // Index doesn't have to change if the sample it plays currently is the same as the sample it would play with the previous index
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
                    // Index changes dont change sound
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
        Report(progress, 0.85);

        // Replace the old timingpoints
        timing.Clear();
        TimingPointChange.Apply(timing, changes);
        beatmap.GiveObjectsGreenlines();

        // Fix this extremely specific thing
        Fix2BDoubleTaps(beatmap);
        Report(progress, 1);
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
                    .Where(now => Math.Abs(now.Offset - old.Offset) < Precision.DOUBLE_EPSILON)
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
        /*
         * When having doubletap circle+slider on the exact same time, slider-notelock can happen if the circle is
         * the second object instead of the first. What this means is that when hitting the object like a regular
         * doubletap, the slider registers but the circle will always miss. This phenomenon can be observed either
         * in the .osu file (the circle will be on the line after the slider), or the editor.
         */
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
