using System.ComponentModel;
using System.Text.RegularExpressions;
using Mapping_Tools.Domain.Audio;
using Mapping_Tools.Domain.Beatmaps;
using Mapping_Tools.Domain.Beatmaps.BeatDivisors;
using Mapping_Tools.Domain.Beatmaps.Contexts;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.Timelines;
using Mapping_Tools.Domain.Beatmaps.Timelines.TimelineObjects;
using Mapping_Tools.Domain.Beatmaps.Timings;
using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;
using Mapping_Tools.Domain.ToolHelpers;

namespace Mapping_Tools.Domain.Tools.MapCleaner;

public record MapCleanerResult(int ObjectsResnapped, int TimingPointsRemoved);

public record MapCleanerArgs(
    bool ResnapObjects,
    bool ResnapBookmarks,
    List<IBeatDivisor> BeatDivisors,
    bool RemoveHitsounds,
    bool RemoveMuting,
    bool RemoveUnclickableHitsounds,
    bool VolumeSliders,
    bool VolumeSpinners,
    bool SampleSetSliders
);

public sealed record MapCleaningProgress(
    string Phase,
    int? Percent, // optional; domain is allowed to omit
    string? Message = null);

public class MapCleanerService {
    /// <summary>
    /// Cleans a beatmap of unused timing points and various other stuff.
    /// </summary>
    /// <param name="beatmap">The beatmap that is going to be cleaned.</param>
    /// <param name="args">The arguments for how to clean the beatmap.</param>
    /// <param name="sampleLookup">A mapping from all available custom samples to their content hash. Used for determining which custom samples exist and which sounds they make.</param>
    /// <param name="progress">The callback for updating progress.</param>
    /// <returns>Statistics of how much has changed.</returns>
    public MapCleanerResult CleanMap(Beatmap beatmap, MapCleanerArgs args, ISampleLookup? sampleLookup = null, IProgress<MapCleaningProgress>? progress = null) {
        progress?.Report(new MapCleaningProgress("Starting", 0, "Starting map cleaning process."));

        Timing timing = beatmap.BeatmapTiming;
        GameMode mode = beatmap.General.Mode;
        var circleSize = beatmap.Difficulty.CircleSize;

        int objectsResnapped = 0;
        int oldTimingPointsCount = timing.TimingPoints.Count;

        // Collect timeline objects before resnapping, so the timingpoints
        // are still valid and the tlo's get the correct hitsounds and offsets.
        // Resnapping of the hit objects will move the tlo's aswell
        Timeline timeline = beatmap.GetTimeline();

        progress?.Report(new MapCleaningProgress("Collecting timing changes", 3, "Collecting Kiai toggles and SV changes."));

        // Collect Kiai toggles and SliderVelocity changes for mania/taiko
        List<TimingPoint> kiaiToggles = [];
        List<TimingPoint> svChanges = [];
        bool lastKiai = false;
        double lastSv = -100;
        foreach (TimingPoint tp in timing.TimingPoints) {
            if (tp.Kiai != lastKiai) {
                kiaiToggles.Add(tp.Copy());
                lastKiai = tp.Kiai;
            }

            if (tp.Uninherited) {
                lastSv = -100;
            } else {
                if (Precision.AlmostEquals(tp.MpB, lastSv)) {
                    continue;
                }

                svChanges.Add(tp.Copy());
                lastSv = tp.MpB;
            }
        }

        // Resnap shit
        if (args.ResnapObjects) {
            progress?.Report(new MapCleaningProgress("Resnapping objects", 15, "Resnapping all hit objects."));
            // Resnap all objects
            foreach (HitObject ho in beatmap.HitObjects) {
                bool resnapped = ho.ResnapSelf(timing, args.BeatDivisors);
                if (resnapped) {
                    objectsResnapped += 1;
                }

                if (ho is IDuration durationObject)
                    durationObject.ResnapEndTimeSmart(timing, args.BeatDivisors);

                ho.ResnapPosition(mode, circleSize);
            }

            progress?.Report(new MapCleaningProgress("Resnapping Kiai toggles", 25, "Resnapping Kiai toggles."));
            // Resnap Kiai toggles
            foreach (TimingPoint tp in kiaiToggles) {
                tp.ResnapSelf(timing, args.BeatDivisors);
            }

            progress?.Report(new MapCleaningProgress("Resnapping SV changes", 35, "Resnapping SliderVelocity changes."));
            // Resnap SliderVelocity changes
            foreach (TimingPoint tp in svChanges) {
                tp.ResnapSelf(timing, args.BeatDivisors);
            }
        }

        if (args.ResnapBookmarks) {
            progress?.Report(new MapCleaningProgress("Resnapping bookmarks", 40, "Resnapping bookmarks."));
            // Resnap the bookmarks
            List<double> bookmarks = beatmap.Editor.Bookmarks;
            List<double> newBookmarks = bookmarks.Select(o => timing.Resnap(o, args.BeatDivisors)).ToList();

            // Remove duplicate bookmarks
            newBookmarks = newBookmarks.Distinct().ToList();
            beatmap.Editor.Bookmarks = newBookmarks;
        }

        progress?.Report(new MapCleaningProgress("Adding redlines", 50, "Adding redlines."));

        // Make new timingpoints
        List<ControlChange> timingPointsChanges = [];

        // Add redlines
        var redlines = timing.Redlines;
        foreach (TimingPoint tp in redlines) {
            timingPointsChanges.Add(new ControlChange(tp, mpb: true, meter: true, uninherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon));
        }

        // Add redlines
        progress?.Report(new MapCleaningProgress("Adding redlines", 50, "About to add redlines."));

        // Add SliderVelocity changes for taiko and mania
        progress?.Report(new MapCleaningProgress("Adding SV changes", 55, "Adding SliderVelocity changes for taiko/mania."));
        if (mode == GameMode.Taiko || mode == GameMode.Mania) {
            foreach (TimingPoint tp in svChanges) {
                timingPointsChanges.Add(new ControlChange(tp, mpb: true, fuzzyness: 0.4));
            }
        }

        progress?.Report(new MapCleaningProgress("Adding Kiai toggles", 60, "Adding Kiai toggles."));
        // Add Kiai toggles
        foreach (TimingPoint tp in kiaiToggles) {
            timingPointsChanges.Add(new ControlChange(tp, kiai: true));
        }

        progress?.Report(new MapCleaningProgress("Adding hitobject timing", 65, "Adding slider body hitsounds and velocity changes."));
        // Add Hitobject stuff
        foreach (HitObject ho in beatmap.HitObjects) {
            var timingContext = ho.GetContext<TimingContext>();

            if (ho is Slider slider) // SliderVelocity changes
            {
                TimingPoint tp = timingContext.TimingPoint.Copy();
                tp.Offset = slider.StartTime;
                tp.MpB = timingContext.SliderVelocity;
                timingPointsChanges.Add(new ControlChange(tp, mpb: true, fuzzyness: 0.4));
            }

            // Skip adding hitsounds if we want to remove them
            if (args.RemoveHitsounds) {
                ho.ResetHitsounds();
                continue;
            }

            // Body hitsounds
            bool vol = ho is Slider && args.VolumeSliders || ho is Spinner && args.VolumeSpinners;
            bool sam = ho is Slider && args.SampleSetSliders && ho.Hitsounds.SampleSet == 0;
            bool ind = ho is Slider && args.SampleSetSliders;
            bool samplesetActuallyChanged = false;
            foreach (TimingPoint tp in timingContext.BodyHitsounds) {
                if (Math.Abs(tp.Volume - 5) < Precision.DoubleEpsilon && args.RemoveMuting) {
                    vol = false; // Removing sliderbody silencing
                    //ind = false;  // Removing silent custom index
                }

                timingPointsChanges.Add(new ControlChange(tp, volume: vol, index: ind, sampleset: sam));

                if (tp.SampleSet != timingContext.HitsoundTimingPoint.SampleSet) {
                    samplesetActuallyChanged = args.SampleSetSliders && ho.Hitsounds.SampleSet == 0;
                } // True for sampleset change in sliderbody
            }

            if (ho is Slider && !samplesetActuallyChanged && ho.Hitsounds.SampleSet == 0) // Case can put sampleset on sliderbody
            {
                ho.Hitsounds.SampleSet = timingContext.HitsoundTimingPoint.SampleSet;
            }

            if (ho is Slider && samplesetActuallyChanged) // Make it start out with the right sampleset
            {
                TimingPoint tp = timingContext.HitsoundTimingPoint.Copy();
                tp.Offset = ho.StartTime;
                timingPointsChanges.Add(new ControlChange(tp, sampleset: true));
            }
        }

        progress?.Report(new MapCleaningProgress("Adding timeline hitsounds", 75, "Adding timeline hitsounds."));
        if (!args.RemoveHitsounds) {
            // Add timeline hitsounds
            foreach (TimelineObject tlo in timeline.TimelineObjects) {
                // Change the sample sets in the hitobjects
                var tloFenoHitsounds = tlo.FenoHitsounds;

                // If both sets are the same, set the additional set to none, so it inherits from the sample set
                if (tloFenoHitsounds.AdditionSet == tloFenoHitsounds.SampleSet)
                    tloFenoHitsounds.AdditionSet = SampleSet.None;

                tlo.HitsoundsToOrigin(tloFenoHitsounds, copyCustoms: mode == GameMode.Mania);

                if (!tlo.HasHitsound) {
                    continue;
                }

                // Add greenlines for custom indexes and volumes
                var timingContext = tlo.GetContext<TimingContext>();
                TimingPoint tp = timingContext.HitsoundTimingPoint.Copy();

                bool doUnmute = Math.Abs(tlo.FenoSampleVolume - 5) < Precision.DoubleEpsilon && args.RemoveMuting;
                bool doMute = args is { RemoveUnclickableHitsounds: true, RemoveMuting: false } && tlo is not (HitCircleTlo or SliderHead or HoldNoteHead);

                bool ind = !tlo.UsesFilename && !doUnmute; // Index doesnt have to change if custom is overridden by Filename
                bool vol = !doUnmute; // Remove volume change muted

                tp.Offset = tlo.Time;
                tp.SampleIndex = tlo.FenoCustomIndex;
                tp.Volume = doMute ? 5 : tlo.FenoSampleVolume;

                // Index doesn't have to change if the sample it plays currently is the same as the sample it would play with the previous index
                if (ind && sampleLookup is not null) {
                    HashSet<string> nativeSamples = tlo.GetPlayingFilenames(sampleLookup, mode).Select(o => sampleLookup[o]).ToHashSet();

                    // Find the custom index that would be active just before this timing point
                    int newIndex = tlo.FenoCustomIndex;
                    double latest = double.NegativeInfinity;
                    foreach (ControlChange tpc in timingPointsChanges) {
                        if (tpc.Index && tpc.NewTimingPoint.Offset <= tlo.Time && tpc.NewTimingPoint.Offset >= latest) {
                            newIndex = tpc.NewTimingPoint.SampleIndex;
                            latest = tpc.NewTimingPoint.Offset;
                        }
                    }

                    var oldTp = timingContext.HitsoundTimingPoint;
                    var newTp = oldTp.Copy();
                    newTp.SampleIndex = newIndex;
                    timingContext.HitsoundTimingPoint = newTp;

                    // Index changes dont change sound
                    HashSet<string> newSamples = tlo.GetPlayingFilenames(sampleLookup, mode).Select(o => sampleLookup[o]).ToHashSet();
                    if (nativeSamples.SetEquals(newSamples))
                        tp.SampleIndex = newIndex;

                    timingContext.HitsoundTimingPoint = oldTp;
                }

                timingPointsChanges.Add(new ControlChange(tp, volume: vol, index: ind));
            }
        }

        progress?.Report(new MapCleaningProgress("Replacing timing points", 85, "Replacing old timing points."));
        // Replace the old timing points
        timing.Clear();
        ControlChange.ApplyChanges(timing, timingPointsChanges);
        beatmap.GiveObjectsTimingContext();

        progress?.Report(new MapCleaningProgress("Finalizing", 90, "Finalizing timing and context."));
        // Fix this extremely specific thing
        Fix2BDoubleTaps(beatmap);

        progress?.Report(new MapCleaningProgress("Complete", 100, "Map cleaning complete."));

        int timingPointsRemoved = oldTimingPointsCount - beatmap.BeatmapTiming.TimingPoints.Count;
        return new MapCleanerResult(objectsResnapped, timingPointsRemoved);
    }

    private static void Fix2BDoubleTaps(Beatmap beatmap) {
        /*
         * When having double tap circle+slider on the exact same time, slider-notelock can happen if the circle is
         * the second object instead of the first. What this means is that when hitting the object like a regular
         * double tap, the slider registers but the circle will always miss. This phenomenon can be observed either
         * in the .osu file (the circle will be on the line after the slider), or the editor.
         */
        for (var i = 0; i < beatmap.HitObjects.Count - 1; i++) {
            var ho1 = beatmap.HitObjects[i];
            var ho2 = beatmap.HitObjects[i + 1];
            if (ho1 is Slider && ho2 is HitCircle && Precision.AlmostEquals(ho1.StartTime, ho2.StartTime)) {
                // Swap the two objects
                (beatmap.HitObjects[i], beatmap.HitObjects[i + 1]) = (ho2, ho1);
            }
        }
    }

    public List<string> FindUnusedSamples(IEnumerable<Beatmap> beatmaps, IEnumerable<Storyboard> storyboards, ISampleLookup sampleLookup) {
        // Collect all the used samples
        HashSet<string> allFilenames = [];
        bool anySpinners = false;

        foreach (var beatmap in beatmaps) {
            GameMode mode = beatmap.General.Mode;
            double sliderTickRate = beatmap.Difficulty.SliderTickRate;

            if (!anySpinners)
                anySpinners = mode == 0 && beatmap.HitObjects.Any(o => o is Spinner);

            allFilenames.Add(beatmap.General.AudioFilename.Trim());

            foreach (Slider slider in beatmap.HitObjects.OfType<Slider>()) {
                allFilenames.UnionWith(slider.GetPlayingBodyFilenames(sliderTickRate, false).SelectMany(o => o));
            }

            foreach (TimelineObject tlo in beatmap.GetTimeline().TimelineObjects) {
                allFilenames.UnionWith(tlo.GetLookupFilenames(mode, false).SelectMany(o => o));
            }

            foreach (StoryboardSoundSample sbss in beatmap.Storyboard.StoryboardSoundSamples) {
                allFilenames.Add(sbss.FilePath);
            }
        }

        foreach (var storyboard in storyboards) {
            foreach (StoryboardSoundSample sbss in storyboard.StoryboardSoundSamples) {
                allFilenames.Add(sbss.FilePath);
            }
        }

        // Only if there are spinners in standard you may have spinnerspin and spinnerbonus
        if (anySpinners)
            allFilenames.UnionWith(["spinnerspin", "spinnerbonus"]);

        // We don't do extensions in osu!
        allFilenames = allFilenames.Select(RemoveExtension).ToHashSet();

        // Find which of the available samples are unused
        List<string> unusedSamples = [];
        foreach (string samplePath in sampleLookup.Keys) {
            string extless = RemoveExtension(samplePath);

            if (!(allFilenames.Contains(extless) || BeatmapSkinnableSamples.Any(o => Regex.IsMatch(extless, o)))) {
                unusedSamples.Add(samplePath);
            }
        }

        return unusedSamples;

        string RemoveExtension(string path) {
            int lastPeriod = path.LastIndexOf('.');
            return lastPeriod < 0
                ? path
                : // No extension was found
                path[..lastPeriod];
        }
    }

    private static readonly string[] BeatmapSkinnableSamples = [
        "count1s",
        "count2s",
        "count3s",
        "gos",
        "readys",
        "applause",
        "comboburst",
        "comboburst-[0-9]+",
        "combobreak",
        "failsound",
        "sectionpass",
        "sectionfail",
        "pause-loop",
    ];
}