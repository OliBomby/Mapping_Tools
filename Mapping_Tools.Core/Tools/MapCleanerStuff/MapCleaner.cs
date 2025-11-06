using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Core.Audio.DuplicateDetection;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff.TimelineObjects;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.BeatmapHelper.Types;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers;

namespace Mapping_Tools.Core.Tools.MapCleanerStuff;

/// <summary>
/// Mapping Tool for cleaning beatmaps, resnapping hit objects, and restructuring greenlines.
/// </summary>
public class MapCleaner {
    /// <summary>
    /// Delegate for reporting progress updates if Map Cleaner is running async.
    /// </summary>
    /// <param name="progress">The progress [0-1].</param>
    public delegate void ProgressUpdateDelegate(double progress);

    /// <summary>
    /// Cleans a beatmap.
    /// If the <see cref="IBeatmap.BeatmapSet"/> property exists on the beatmap, then it will analyze samples for better hitsounds cleaning.
    /// </summary>
    /// <param name="beatmap">The beatmap that is going to be cleaned.</param>
    /// <param name="args">The arguments for how to clean the beatmap.</param>
    /// <param name="progressUpdater">The BackgroundWorker for updating progress.</param>
    /// <returns>Number of resnapped objects and removed greenlines.</returns>
    public static IMapCleanerResult CleanMap(IBeatmap beatmap, IMapCleanerArgs args, ProgressUpdateDelegate progressUpdater = null) {
        UpdateProgress(progressUpdater, 0);

        Timing timing = beatmap.BeatmapTiming;

        GameMode mode = beatmap.General.Mode;
        double circleSize = beatmap.Difficulty.CircleSize;

        IDuplicateSampleMap sampleComparer = null;
        string containingFolderPath = string.Empty;
        if (beatmap.BeatmapSet != null) {
            sampleComparer = new MonolithicDuplicateSampleDetector().AnalyzeSamples(beatmap.BeatmapSet.SoundFiles, out _);
            containingFolderPath = Path.GetDirectoryName(beatmap.GetBeatmapSetRelativePath()) ?? string.Empty;
        }

        int objectsResnapped = 0;

        // Collect timeline objects before resnapping, so the timingpoints
        // are still valid and the tlo's get the correct hitsounds and offsets.
        // Resnapping of the hit objects will move the tlo's aswell
        Timeline timeline = beatmap.GetTimeline();

        // Collect Kiai toggles and SliderVelocity changes for mania/taiko
        List<TimingPoint> kiaiToggles = new List<TimingPoint>();
        List<TimingPoint> svChanges = new List<TimingPoint>();
        bool lastKiai = false;
        double lastSV = 1;
        foreach (TimingPoint tp in timing.TimingPoints) {
            // Add kiai toggles
            if (tp.Kiai != lastKiai) {
                kiaiToggles.Add(tp.Copy());
                lastKiai = tp.Kiai;
            }

            // Add SV changes
            if (tp.Uninherited) {
                lastSV = 1;
            } else {
                var sv = tp.GetSliderVelocity();
                if (sv != lastSV) {
                    svChanges.Add(tp.Copy());
                    lastSV = sv;
                }
            }
        }
        UpdateProgress(progressUpdater, 9);

        // Resnap shit
        if (args.ResnapObjects) {
            // Resnap all objects
            foreach (HitObject ho in beatmap.HitObjects) {
                bool resnapped = ho.ResnapSelf(timing, args.BeatDivisors);
                if (resnapped) {
                    objectsResnapped += 1;
                }

                switch (ho) {
                    case Slider sliderHo:
                        sliderHo.ResnapEndTimeSmart(timing, args.BeatDivisors);
                        break;
                    case IDuration durationHo:
                        durationHo.ResnapEndTime(timing, args.BeatDivisors);
                        break;
                }

                ho.ResnapPosition(mode, circleSize);

                // Update the timeline objects
                ho.GetContext<TimelineContext>().UpdateTimelineObjectTimes(ho);
            }
            UpdateProgress(progressUpdater, 18);

            // Resnap Kiai toggles
            foreach (TimingPoint tp in kiaiToggles) {
                tp.ResnapSelf(timing, args.BeatDivisors);
            }
            UpdateProgress(progressUpdater, 27);

            // Resnap SliderVelocity changes
            foreach (TimingPoint tp in svChanges) {
                tp.ResnapSelf(timing, args.BeatDivisors);
            }
            UpdateProgress(progressUpdater, 36);
        }

        if (args.ResnapBookmarks) {
            // Resnap the bookmarks
            // Remove duplicate bookmarks
            beatmap.Editor.Bookmarks = beatmap.Editor.Bookmarks
                .Select(o => timing.Resnap(o, args.BeatDivisors))
                .Distinct().ToList();

            UpdateProgress(progressUpdater, 45);
        }

        // Make new timingpoints
        List<ControlChange> controlChanges = new List<ControlChange>();

        // Add redlines
        var redlines = timing.Redlines;
        foreach (TimingPoint tp in redlines) {
            controlChanges.Add(new ControlChange(tp, mpb: true, meter: true, uninherited: true, omitFirstBarLine: true, fuzzyness:Precision.DOUBLE_EPSILON));
        }
        UpdateProgress(progressUpdater, 55);

        // Add SliderVelocity changes for taiko and mania
        if (mode == GameMode.Taiko || mode == GameMode.Mania) {
            foreach (TimingPoint tp in svChanges) {
                controlChanges.Add(new ControlChange(tp, mpb: true));
            }
        }
        UpdateProgress(progressUpdater, 60);

        // Add Kiai toggles
        foreach (TimingPoint tp in kiaiToggles) {
            controlChanges.Add(new ControlChange(tp, kiai: true));
        }
        UpdateProgress(progressUpdater, 65);

        // Add Hitobject stuff
        foreach (HitObject ho in beatmap.HitObjects) {
            var timingContext = ho.GetContext<TimingContext>();

            // SliderVelocity changes
            if (ho is Slider) {
                TimingPoint tp = timingContext.TimingPoint.Copy();
                tp.Offset = ho.StartTime;
                tp.Uninherited = false;
                tp.SetSliderVelocity(timingContext.SliderVelocity);
                controlChanges.Add(new ControlChange(tp, mpb: true));
            }

            // Add body hitsounds
            if (ho is IHasDuration durationHo) {
                bool vol = (ho is Slider && args.VolumeSliders) || (ho is Spinner && args.VolumeSpinners);
                bool sam = (ho is Slider && args.SampleSetSliders && ho.Hitsounds.SampleSet == SampleSet.None);
                bool ind = (ho is Slider && args.SampleSetSliders);

                // Whether sampleset of the body hitsounds changed during the hit object
                bool samplesetActuallyChanged = false;
                // For all body hitsounds that are actually during the hit object
                foreach (TimingPoint tp in timingContext.BodyHitsounds.Where(tp => ho.StartTime < tp.Offset && tp.Offset < durationHo.EndTime)) {
                    if (tp.Volume == 5 && args.RemoveMuting) {
                        vol = false;  // Removing sliderbody silencing
                        ind = false;  // Removing silent custom index
                    }
                    controlChanges.Add(new ControlChange(tp, volume: vol, index: ind, sampleset: sam));

                    if (tp.SampleSet != timingContext.HitsoundTimingPoint.SampleSet) {
                        samplesetActuallyChanged = args.SampleSetSliders && ho.Hitsounds.SampleSet == SampleSet.None;
                    }  // True for sampleset change in sliderbody
                }

                // In this case we can put sampleset on sliderbody without timing points
                if (ho is Slider && !samplesetActuallyChanged && ho.Hitsounds.SampleSet == SampleSet.None) {
                    ho.Hitsounds.SampleSet = timingContext.HitsoundTimingPoint.SampleSet;
                }

                // Make sure it starts out with the right sampleset
                if (ho is Slider && samplesetActuallyChanged) {
                    TimingPoint tp = timingContext.HitsoundTimingPoint.Copy();
                    tp.Offset = ho.StartTime;
                    controlChanges.Add(new ControlChange(tp, sampleset: true));
                }
            }
        }
        UpdateProgress(progressUpdater, 75);

        // Add timeline hitsounds
        foreach (TimelineObject tlo in timeline.TimelineObjects) {
            // Change the samplesets in the hitobjects
            tlo.Hitsounds.SampleSet = tlo.FenoSampleSet;
            tlo.Hitsounds.AdditionSet = tlo.FenoAdditionSet;

            // Simplify volume and
            if (mode == GameMode.Mania && tlo.CanCustoms) {
                tlo.Hitsounds.CustomIndex = tlo.FenoCustomIndex;
                tlo.Hitsounds.Volume = tlo.FenoSampleVolume;
            }

            // Simplify additions to auto
            if (tlo.Hitsounds.AdditionSet == tlo.Hitsounds.SampleSet) {
                tlo.Hitsounds.AdditionSet = SampleSet.None;
            }

            tlo.HitsoundsToOrigin();

            // Add greenlines for custom indexes and volumes
            if (tlo.HasHitsound) {
                var timingContext = tlo.GetContext<TimingContext>();
                TimingPoint tp = timingContext.HitsoundTimingPoint.Copy();

                bool doUnmute = tlo.FenoSampleVolume == 5 && args.RemoveMuting;
                bool doMute = args.RemoveUnclickableHitsounds && !args.RemoveMuting &&
                              !(tlo is HitCircleTlo || (tlo is SliderNode sn && sn.NodeIndex == 0) || tlo is HoldNoteHead);

                bool ind = !tlo.UsesFilename && !doUnmute;  // Index doesnt have to change if custom is overridden by Filename
                bool vol = !doUnmute;  // Remove volume change muted

                tp.Offset = tlo.Time;
                tp.SampleIndex = tlo.FenoCustomIndex;
                tp.Volume = doMute ? 5 : tlo.FenoSampleVolume;

                // Index doesn't have to change if the sample it plays currently is the same as the sample it would play with the previous index
                if (ind && sampleComparer != null) {
                    // Get the samples which the timeline object originally plays
                    List<string> nativeSamples = tlo.GetFirstPlayingFilenames(mode, containingFolderPath, sampleComparer).ToList();

                    int oldIndex = tlo.FenoCustomIndex;

                    // Find the custom index that the timeline object would have with the current control changes
                    int newIndex = tlo.FenoCustomIndex;
                    double latest = double.NegativeInfinity;
                    foreach (var tpc in controlChanges.Where(tpc =>
                                 tpc.Index && tpc.MyTP.Offset <= tlo.Time && tpc.MyTP.Offset >= latest)) {
                        newIndex = tpc.MyTP.SampleIndex;
                        latest = tpc.MyTP.Offset;
                    }

                    // Simulate the next timing context
                    var newTimingContext = (TimingContext)timingContext.Copy();
                    newTimingContext.HitsoundTimingPoint.SampleIndex = newIndex;
                    tlo.SetContext(newTimingContext);

                    // Get the samples which the timeline object plays after the control changes
                    List<string> newSamples = tlo.GetFirstPlayingFilenames(mode, containingFolderPath, sampleComparer).ToList();

                    // If the samples are equal, then we can use the alternative index
                    tp.SampleIndex = nativeSamples.SequenceEqual(newSamples) ? newIndex : oldIndex;

                    // Reset the timing context back to what it was
                    tlo.SetContext(timingContext);
                }

                controlChanges.Add(new ControlChange(tp, volume: vol, index: ind));
            }
        }
        UpdateProgress(progressUpdater, 85);

        // Save the old number of timing points
        var oldCount = timing.Count;

        // Replace the old timingpoints
        timing.Clear();
        ControlChange.ApplyChanges(timing, controlChanges);
        beatmap.GiveObjectsTimingContext();

        // Complete progressbar
        UpdateProgress(progressUpdater, 100);

        return new MapCleanerResult(objectsResnapped, oldCount - timing.Count);
    }

    private static void UpdateProgress(ProgressUpdateDelegate progressUpdater, int progress) {
        progressUpdater?.Invoke(progress);
    }
}