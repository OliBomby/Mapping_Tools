using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Audio.DuplicateDetection;
using Mapping_Tools.Core.Audio.SampleGeneration;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers;

namespace Mapping_Tools.Core.Tools.HitsoundCopierStuff;

/// <summary>
/// Hitsound Copier tool.
/// Allows copying hitsounds between maps with various options.
/// </summary>
public class HitsoundCopier {
    /// <summary>
    /// Leniency for dealing with misalignments in time between the beatmaps.
    /// This is the maximum time in milliseconds between two notes that copy hitsounds.
    /// </summary>
    public double TemporalLeniency = 5;

    /// <summary>
    /// Whether to copy hitsounds of hitsound events (circles, sliderhead/tail, spinner end)
    /// </summary>
    public bool DoCopyHitsounds = true;

    /// <summary>
    /// Whether to copy hitsounds of timing points that change the sound of held notes.
    /// </summary>
    public bool DoCopyBodyHitsounds = true;

    /// <summary>
    /// Whether to copy sample set changes.
    /// </summary>
    public bool DoCopySampleSets = true;

    /// <summary>
    /// Whether to copy sample volume changes.
    /// </summary>
    public bool DoCopyVolumes = true;

    /// <summary>
    /// Whether to preserve all 5% volume hitsounds in the destination beatmap regardless of the volume in the source beatmap.
    /// </summary>
    public bool AlwaysPreserve5Volume = true;

    /// <summary>
    /// Whether to prevent copying storyboarded samples that are already played in the hitsounds.
    /// Requires <see cref="IBeatmap.BeatmapSet"/> with all sound samples to be set in the destination beatmap.
    /// </summary>
    public bool IgnoreHitsoundSatisfiedSamples = true;

    /// <summary>
    /// Whether to copy to slider ticks and create new samples for this.
    /// </summary>
    public bool DoCopyToSliderTicks = false;

    /// <summary>
    /// Whether to copy to slider slides and create new samples for this.
    /// </summary>
    public bool DoCopyToSliderSlides = false;

    /// <summary>
    /// The sample index to start counting from when making new samples for slider ticks or slider slides.
    /// </summary>
    /// <remarks>
    /// The sample index will count upwards from this starting value.
    /// </remarks>
    public int StartIndex = 100;

    /// <summary>
    /// Copies hitsounds from one map to another.
    /// </summary>
    /// <remarks>
    /// Copying to slider ticks or slider slides is not supported in this method.
    /// </remarks>
    /// <param name="sourceBeatmap">The map to copy hitsounds from.</param>
    /// <param name="destBeatmap">The map to copy hitsounds to.</param>
    /// <param name="processedTimeline">The timeline of the destination beatmap with <see cref="HasCopiedContext"/> on each timeline object that has been copied to.</param>
    /// <returns></returns>
    public void CopyHitsoundsBasic(IBeatmap sourceBeatmap, IBeatmap destBeatmap, out Timeline processedTimeline) {
        // Every defined hitsound and sampleset on hitsound gets copied to their copyTo destination
        // Timelines
        var tlTo = destBeatmap.GetTimeline();
        var tlFrom = sourceBeatmap.GetTimeline();

        var volumeMuteTimes = DoCopyVolumes && AlwaysPreserve5Volume ? new List<double>() : null;

        if (DoCopyHitsounds) {
            ResetHitObjectHitsounds(destBeatmap);
            CopyHitsounds(tlFrom, tlTo);
        }

        // Save tlo times where timingpoint volume is 5%
        // Timingpointchange all the undefined tlo from copyFrom
        volumeMuteTimes?.AddRange(from tloTo in tlTo.TimelineObjects
            where !tloTo.HasContext<HasCopiedContext>() && Math.Abs(tloTo.Hitsounds.Volume) < Precision.DOUBLE_EPSILON
                                                        && Math.Abs(tloTo.FenoSampleVolume - 5) < Precision.DOUBLE_EPSILON
            select tloTo.Time);

        // Volumes and samplesets and customindices greenlines get copied with timingpointchanges and allafter enabled
        var controlChanges = sourceBeatmap.BeatmapTiming.TimingPoints.Select(tp =>
            new ControlChange(tp, sampleset: DoCopySampleSets, index: DoCopySampleSets,
                volume: DoCopyVolumes)).ToList();

        // Apply the timingpoint changes
        ControlChange.ApplyChanges(destBeatmap.BeatmapTiming, controlChanges, true);

        processedTimeline = tlTo;

        // Return 5% volume to tlo that had it before
        if (volumeMuteTimes != null) {
            var timingPointsChangesMute = new List<ControlChange>();
            processedTimeline.GiveTimingContext(destBeatmap.BeatmapTiming);

            // Exclude objects which use their own sample volume property instead
            foreach (var tloTo in processedTimeline.TimelineObjects
                         .Where(o => Math.Abs(o.Hitsounds.Volume) < Precision.DOUBLE_EPSILON)) {
                if (volumeMuteTimes.Contains(tloTo.Time)) {
                    // Add timingpointschange to copy timingpoint hitsounds
                    var tp = tloTo.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                    tp.Offset = tloTo.Time;
                    tp.Volume = 5;
                    timingPointsChangesMute.Add(new ControlChange(tp, volume: true));
                } else {
                    // Add timingpointschange to preserve index and volume
                    var tp = tloTo.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                    tp.Offset = tloTo.Time;
                    tp.Volume = tloTo.FenoSampleVolume;
                    timingPointsChangesMute.Add(new ControlChange(tp, volume: true));
                }
            }

            // Apply the timingpoint changes
            ControlChange.ApplyChanges(destBeatmap.BeatmapTiming, timingPointsChangesMute);
        }
    }

    /// <summary>
    /// Copies storyboarded samples between beatmaps.
    /// </summary>
    /// <param name="sourceBeatmap">The beatmap to copy samples from.</param>
    /// <param name="destBeatmap">The beatmap to copy samples to.</param>
    /// <param name="removeOldSamples">Whether to remove the existing storyboarded samples in the destination beatmap.</param>
    public void CopyStoryboardedSamples(IBeatmap sourceBeatmap, IBeatmap destBeatmap, bool removeOldSamples) {
        CopyStoryboardedSamples(sourceBeatmap, destBeatmap, destBeatmap.GetTimeline(), removeOldSamples);
    }

    private void CopyStoryboardedSamples(IBeatmap sourceBeatmap, IBeatmap destBeatmap, Timeline destTimeline, bool removeOldSamples) {
        if (removeOldSamples) {
            destBeatmap.Storyboard.StoryboardSoundSamples.Clear();
        }

        destBeatmap.GiveObjectsTimingContext();
        destTimeline.GiveTimingContext(destBeatmap.BeatmapTiming);

        IDuplicateSampleMap sampleComparer = null;
        string containingFolderPath = string.Empty;
        if (destBeatmap.BeatmapSet != null && IgnoreHitsoundSatisfiedSamples) {
            sampleComparer = new MonolithicDuplicateSampleDetector().AnalyzeSamples(destBeatmap.BeatmapSet.SoundFiles, out _);
            containingFolderPath = Path.GetDirectoryName(destBeatmap.GetBeatmapSetRelativePath()) ?? string.Empty;
        }

        var samplesTo = new HashSet<StoryboardSoundSample>(destBeatmap.Storyboard.StoryboardSoundSamples);
        var mode = destBeatmap.General.Mode;

        foreach (var sampleFrom in sourceBeatmap.Storyboard.StoryboardSoundSamples) {
            // Add the StoryboardSoundSamples from beatmapFrom to beatmapTo only if it doesn't already have the sample
            if (samplesTo.Contains(sampleFrom)) {
                continue;
            }

            // If IgnoreHitoundSatisfiedSamples and the beatmap set is not null
            if (sampleComparer != null) {
                var tloHere = destTimeline.TimelineObjects.FindAll(o =>
                    Math.Abs(o.Time - sampleFrom.StartTime) <= TemporalLeniency);

                var samplesHere = new HashSet<string>();
                foreach (var tlo in tloHere) {
                    foreach (var filename in tlo.GetFirstPlayingFilenames(mode, containingFolderPath, sampleComparer, false)) {
                        samplesHere.Add(filename);
                    }
                }

                // Get the signature of the storyboard sample here
                var sbSamplePath = Path.Combine(containingFolderPath, sampleFrom.FilePath);
                sbSamplePath = sampleComparer.GetOriginalSample(sbSamplePath)?.Filename ?? sbSamplePath;

                // Skip adding this SB sample if its already present in hitsounds
                if (samplesHere.Contains(sbSamplePath))
                    continue;
            }

            destBeatmap.Storyboard.StoryboardSoundSamples.Add(sampleFrom);
        }

        // Sort the storyboarded samples
        destBeatmap.Storyboard.StoryboardSoundSamples.Sort();
    }

    /// <summary>
    /// Copies hitsounds from one map to another.
    /// This smart version will preserve all hitsounds in objects of the destination beatmap that didn't get anything copied to them.
    /// </summary>
    /// <param name="sourceBeatmap">The map to copy hitsounds from.</param>
    /// <param name="destBeatmap">The map to copy hitsounds to.</param>
    /// <param name="processedTimeline">The timeline of the destination beatmap with <see cref="HasCopiedContext"/> on each timeline object that has been copied to.</param>
    /// <param name="sampleSchema">The sample schema to add new samples to when copy to slider ticks or copy to slider slides is enabled.</param>
    /// <returns></returns>
    public void CopyHitsoundsSmart(IBeatmap sourceBeatmap, IBeatmap destBeatmap, out Timeline processedTimeline, SampleSchema sampleSchema = null) {
        // Smarty mode
        // Copy the defined hitsounds literally (not feno, that will be reserved for cleaner)
        // Only the tlo that have been defined by copyFrom get overwritten.
        sourceBeatmap.GiveObjectsTimingContext();
        destBeatmap.GiveObjectsTimingContext();

        var tlTo = destBeatmap.GetTimeline();
        var tlFrom = sourceBeatmap.GetTimeline();

        var controlChanges = new List<ControlChange>();
        var mode = destBeatmap.General.Mode;

        IDuplicateSampleMap sampleComparer = null;
        string containingFolderPath = string.Empty;
        if (destBeatmap.BeatmapSet != null && IgnoreHitsoundSatisfiedSamples) {
            sampleComparer = new MonolithicDuplicateSampleDetector().AnalyzeSamples(destBeatmap.BeatmapSet.SoundFiles, out _);
            containingFolderPath = Path.GetDirectoryName(destBeatmap.GetBeatmapSetRelativePath()) ?? string.Empty;
        }

        if (DoCopyHitsounds) {
            CopyHitsounds(destBeatmap, tlFrom, tlTo, controlChanges, mode, containingFolderPath, sampleComparer, ref sampleSchema);
        }

        if (DoCopyBodyHitsounds) {
            // Remove timingpoints in beatmapTo that are in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
            foreach (var tp in from ho in destBeatmap.HitObjects
                     from tp in ho.GetContext<TimingContext>().BodyHitsounds
                     where sourceBeatmap.HitObjects.Any(o => o.StartTime < tp.Offset && o.EndTime > tp.Offset)
                     where !tp.Uninherited
                     select tp) {
                destBeatmap.BeatmapTiming.Remove(tp);
            }

            // Get timingpointschanges for every timingpoint from beatmapFrom that is in a sliderbody/spinnerbody for both beatmapTo and BeatmapFrom
            controlChanges.AddRange(from ho in sourceBeatmap.HitObjects
                from tp in ho.GetContext<TimingContext>().BodyHitsounds
                where destBeatmap.HitObjects.Any(o => o.StartTime < tp.Offset && o.EndTime > tp.Offset)
                select new ControlChange(tp.Copy(), sampleset: DoCopySampleSets, index: DoCopySampleSets,
                    volume: DoCopyVolumes));
        }

        // Apply the timingpoint changes
        ControlChange.ApplyChanges(destBeatmap.BeatmapTiming, controlChanges);

        processedTimeline = tlTo;
    }

    private void CopyHitsounds(Timeline tlFrom, Timeline tlTo) {
        foreach (var tloFrom in tlFrom.TimelineObjects) {
            var tloTo = tlTo.GetNearestTlo(tloFrom.Time,  tlo => !tlo.HasContext<HasCopiedContext>());

            if (tloTo != null &&
                Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= TemporalLeniency) {
                // Copy to this tlo
                CopyHitsounds(tloFrom, tloTo);
            }

            tloFrom.SetContext(new HasCopiedContext());
        }
    }

    private void CopyHitsounds(IBeatmap beatmapTo,
        Timeline tlFrom, Timeline tlTo,
        List<ControlChange> controlChanges, GameMode mode, string containingFolderPath,
        IDuplicateSampleMap comparer, ref SampleSchema sampleSchema) {

        var CustomSampledTimes = new HashSet<int>();
        var tloToSliderSlide = new List<TimelineObject>();

        foreach (var tloFrom in tlFrom.TimelineObjects) {
            var tloTo = tlTo.GetNearestTlo(tloFrom.Time, tlo => !tlo.HasContext<HasCopiedContext>());

            if (tloTo != null &&
                Math.Abs(Math.Round(tloFrom.Time) - Math.Round(tloTo.Time)) <= TemporalLeniency) {
                // Copy to this tlo
                CopyHitsounds(tloFrom, tloTo);

                // Add timingpointschange to copy timingpoint hitsounds
                var tp = tloFrom.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                tp.Offset = tloTo.Time;
                controlChanges.Add(new ControlChange(tp, sampleset: DoCopySampleSets,
                    index: DoCopySampleSets, volume: DoCopyVolumes));
            }
            // Try to find a slider tick in range to copy the sample to instead.
            // This slider tick gets a custom sample and timingpoints change to imitate the copied hitsound.
            else
            if (DoCopyToSliderTicks &&
                FindSliderTickInRange(beatmapTo, tloFrom.Time - TemporalLeniency, tloFrom.Time + TemporalLeniency, out var sliderTickTime, out var tickSlider) &&
                !CustomSampledTimes.Contains((int)sliderTickTime)) {

                // Add a new custom sample to this slider tick to represent the hitsounds
                IEnumerable<string> sampleFilenames = tloFrom.GetFirstPlayingFilenames(mode, containingFolderPath, comparer, false);
                var samples = new HashSet<ISampleGenerator>(
                    sampleFilenames
                        // ReSharper disable once PossibleNullReferenceException
                        .Select(o => (ISampleGenerator) new RawAudioSampleGenerator(Helpers.OpenSample(o, comparer.GetOriginalSample(o).GetData())))
                );

                if (samples.Count > 0) {
                    var hstp = tloFrom.GetContext<TimingContext>().HitsoundTimingPoint;

                    if (sampleSchema.AddHitsound(samples, "slidertick", tloFrom.FenoSampleSet,
                            out int index, out var sampleSet, StartIndex)) {
                        // Add a copy of the slider slide sound to this index if necessary
                        // Because we want to retain the same slider slide sound at the time of the slider tick
                        var oldIndex = hstp.SampleIndex;
                        var oldSampleSet = hstp.SampleSet;
                        var oldSlideFilename = Helpers.GetHitsoundFilename(oldSampleSet, "sliderslide", oldIndex);
                        var oldSlidePath = Path.Combine(containingFolderPath, oldSlideFilename);

                        // Check if there exists a custom sample for slider slide here
                        var oldSlideSample = comparer.GetOriginalSample(oldSlidePath);
                        if (oldSlideSample != null) {
                            // Add a copy to the sample schema at the index of the slider tick
                            var slideGeneratingArgs = new RawAudioSampleGenerator(Helpers.OpenSample(oldSlideSample.Filename, oldSlideSample.GetData()));
                            var newSlideFilename = Helpers.GetHitsoundFilename(sampleSet, "sliderslide", index);

                            sampleSchema.Add(newSlideFilename,
                                new HashSet<ISampleGenerator> { slideGeneratingArgs });
                        }
                    }

                    // Make sure the slider with the slider ticks uses auto sampleset so the customized greenlines control the hitsounds
                    tickSlider.Hitsounds.SampleSet = SampleSet.None;

                    // Add timingpointschange
                    var tp = hstp.Copy();
                    tp.Offset = sliderTickTime;
                    tp.SampleIndex = index;
                    tp.SampleSet = sampleSet;
                    tp.Volume = tloFrom.FenoSampleVolume;
                    controlChanges.Add(new ControlChange(tp, sampleset: DoCopySampleSets,
                        index: DoCopySampleSets, volume: DoCopyVolumes));

                    // Add timingpointschange 5ms later to revert the stuff back to whatever it should be
                    var tp2 = hstp.Copy();
                    tp2.Offset = sliderTickTime + 5;
                    controlChanges.Add(new ControlChange(tp2, sampleset: DoCopySampleSets,
                        index: DoCopySampleSets, volume: DoCopyVolumes));

                    CustomSampledTimes.Add((int)sliderTickTime);
                }
            }
            // If the there is no slidertick to be found, then try copying it to the slider slide
            else
            if (DoCopyToSliderSlides) {
                tloToSliderSlide.Add(tloFrom);
            }

            tloFrom.SetContext(new HasCopiedContext());
        }

        // Do the sliderslide hitsounds after because the ticks need to add sliderslides with strict indices.
        foreach (var tlo in tloToSliderSlide) {
            if (!FindSliderAtTime(beatmapTo, tlo.Time, out var slideSlider) ||
                CustomSampledTimes.Contains((int)tlo.Time))
                continue;

            // Add a new custom sample to this slider slide to represent the hitsounds
            IEnumerable<string> sampleFilenames = tlo.GetFirstPlayingFilenames(mode, containingFolderPath, comparer, false);
            var samples = new HashSet<ISampleGenerator>(
                sampleFilenames
                    // ReSharper disable once PossibleNullReferenceException
                    .Select(o => (ISampleGenerator)new RawAudioSampleGenerator(Helpers.OpenSample(o, comparer.GetOriginalSample(o).GetData())))
            );

            if (samples.Count > 0) {
                sampleSchema.AddHitsound(samples, "sliderslide", tlo.FenoSampleSet,
                    out int index, out var sampleSet, StartIndex);

                // Add timingpointschange
                var tp = tlo.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                tp.Offset = tlo.Time;
                tp.SampleIndex = index;
                tp.SampleSet = sampleSet;
                tp.Volume = tlo.FenoSampleVolume;
                controlChanges.Add(new ControlChange(tp, sampleset: DoCopySampleSets,
                    index: DoCopySampleSets, volume: DoCopyVolumes));

                // Make sure the slider with the slider ticks uses auto sampleset so the customized greenlines control the hitsounds
                slideSlider.Hitsounds.SampleSet = SampleSet.None;
            }
        }

        // Timingpointchange all the undefined tlo from copyFrom
        // But this is super complicated because we also try to prevent unnecessary hitsounds
        foreach (var tloTo in tlTo.TimelineObjects) {
            if (tloTo.HasContext<HasCopiedContext>()) continue;

            var hstp = tloTo.GetContext<TimingContext>().HitsoundTimingPoint;
            var hstpCopy = hstp.Copy();
            var holdSampleset = DoCopySampleSets && tloTo.Hitsounds.SampleSet == SampleSet.None;
            var holdIndex = DoCopySampleSets && !(tloTo.CanCustoms && tloTo.Hitsounds.CustomIndex != 0);

            // Dont hold indexes or sampleset if the sample it plays currently is the same as the sample it would play without conserving
            if (holdSampleset || holdIndex) {
                var nativeSamples = tloTo.GetFirstPlayingFilenames(mode, containingFolderPath, comparer).ToArray();

                if (holdSampleset) {
                    var oldSampleSet = hstp.SampleSet;
                    var newSampleSet = hstp.SampleSet;
                    var latest = double.NegativeInfinity;
                    foreach (var tpc in controlChanges) {
                        if (!tpc.Sampleset || !(tpc.MyTP.Offset <= tloTo.Time) || !(tpc.MyTP.Offset >= latest))
                            continue;
                        newSampleSet = tpc.MyTP.SampleSet;
                        latest = tpc.MyTP.Offset;
                    }

                    hstp.SampleSet = newSampleSet;
                    var newSamples = tloTo.GetFirstPlayingFilenames(mode, containingFolderPath, comparer);
                    hstp.SampleSet = oldSampleSet;

                    hstpCopy.SampleSet = nativeSamples.SequenceEqual(newSamples) ? newSampleSet : oldSampleSet;
                }

                if (holdIndex) {
                    var oldIndex = hstp.SampleIndex;
                    var newIndex = hstp.SampleIndex;
                    var latest = double.NegativeInfinity;
                    foreach (var tpc in controlChanges) {
                        if (!tpc.Index || !(tpc.MyTP.Offset <= tloTo.Time) || !(tpc.MyTP.Offset >= latest))
                            continue;
                        newIndex = tpc.MyTP.SampleIndex;
                        latest = tpc.MyTP.Offset;
                    }

                    hstp.SampleIndex = newIndex;
                    var newSamples = tloTo.GetFirstPlayingFilenames(mode, containingFolderPath, comparer);
                    hstp.SampleIndex = oldIndex;

                    hstpCopy.SampleIndex = nativeSamples.SequenceEqual(newSamples) ? newIndex : oldIndex;
                }
            }

            hstpCopy.Offset = tloTo.Time;
            controlChanges.Add(new ControlChange(hstp, sampleset: holdSampleset, index: holdIndex,
                volume: DoCopyVolumes));
        }
    }

    private static bool FindSliderTickInRange(IBeatmap beatmap, double startTime, double endTime, out double sliderTickTime, out HitObject tickSlider) {
        var tickrate = beatmap.Difficulty.SliderTickRate;

        // Check all sliders in range and exclude those which have NaN SV, because those dont have slider ticks
        foreach (var slider in beatmap.HitObjects.Where(o => o is Slider &&
                                                             !double.IsNaN(o.GetContext<TimingContext>().SliderVelocity) &&
                                                             (o.StartTime < endTime || o.EndTime > startTime))) {
            var timeBetweenTicks = slider.GetContext<TimingContext>().UninheritedTimingPoint.MpB / tickrate;

            sliderTickTime = slider.StartTime + timeBetweenTicks;
            while (sliderTickTime < slider.EndTime - 5) {  // This -5 is to make sure the +5 ms timingpoint that reverts the change is still inside the slider
                if (sliderTickTime >= startTime && sliderTickTime <= endTime) {
                    tickSlider = slider;
                    return true;
                }
                sliderTickTime += timeBetweenTicks;
            }
        }

        sliderTickTime = -1;
        tickSlider = null;
        return false;
    }

    private static bool FindSliderAtTime(IBeatmap beatmap, double time, out HitObject slider) {
        slider = beatmap.HitObjects.FirstOrDefault(ho => ho is Slider && ho.StartTime < time && ho.EndTime > time);
        return slider != null;
    }

    private void CopyHitsounds(TimelineObject tloFrom, TimelineObject tloTo) {
        // Copy to this tlo
        tloFrom.Hitsounds.CopyTo(tloTo.Hitsounds, tloTo.CanCustoms);

        // Copy sliderbody hitsounds
        if (tloTo.Origin != null && tloFrom.Origin != null &&
            tloTo is SliderHead && tloFrom is SliderHead && DoCopyBodyHitsounds) {
            tloFrom.Origin.Hitsounds.CopyTo(tloTo.Origin.Hitsounds, false);
        }

        tloTo.HitsoundsToOrigin();
        tloTo.SetContext(new HasCopiedContext());
    }

    private static void ResetHitObjectHitsounds(IBeatmap beatmap) {
        foreach (var ho in beatmap.HitObjects) {
            // Remove all hitsounds
            ho.Hitsounds = new HitSampleInfo();

            if (ho is not Slider slider) continue;

            // Remove edge hitsounds
            slider.EdgeHitsounds = slider.EdgeHitsounds.Select(_ => new HitSampleInfo()).ToList();
        }
    }
}