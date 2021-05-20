using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.Events;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Classes.Tools.MapCleanerStuff {
    public partial class MapCleaner {
        /// <summary>
        /// Cleans a map.
        /// </summary>
        /// <param name="editor">The editor of the beatmap that is going to be cleaned.</param>
        /// <param name="args">The arguments for how to clean the beatmap.</param>
        /// <param name="worker">The BackgroundWorker for updating progress.</param>
        /// <returns>Number of resnapped objects.</returns>
        public static MapCleanerResult CleanMap(BeatmapEditor editor, MapCleanerArgs args, BackgroundWorker worker = null) {
            UpdateProgressBar(worker, 0);

            Beatmap beatmap = editor.Beatmap;
            Timing timing = beatmap.BeatmapTiming;

            GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;
            double circleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
            string mapDir = editor.GetParentFolder();
            Dictionary<string, string> firstSamples = HitsoundImporter.AnalyzeSamples(mapDir);

            int objectsResnapped = 0;
            int samplesRemoved = 0;

            // Collect timeline objects before resnapping, so the timingpoints
            // are still valid and the tlo's get the correct hitsounds and offsets.
            // Resnapping of the hit objects will move the tlo's aswell
            Timeline timeline = beatmap.GetTimeline();

            // Collect Kiai toggles and SliderVelocity changes for mania/taiko
            List<TimingPoint> kiaiToggles = new List<TimingPoint>();
            List<TimingPoint> svChanges = new List<TimingPoint>();
            bool lastKiai = false;
            double lastSV = -100;
            foreach (TimingPoint tp in timing.TimingPoints) {
                if (tp.Kiai != lastKiai) {
                    kiaiToggles.Add(tp.Copy());
                    lastKiai = tp.Kiai;
                }
                if (tp.Uninherited) {
                    lastSV = -100;
                } else {
                    if (tp.MpB != lastSV) {
                        svChanges.Add(tp.Copy());
                        lastSV = tp.MpB;
                    }
                }
            }
            UpdateProgressBar(worker, 9);

            // Resnap shit
            if (args.ResnapObjects) {
                // Resnap all objects
                foreach (HitObject ho in beatmap.HitObjects) {
                    bool resnapped = ho.ResnapSelf(timing, args.BeatDivisors);
                    if (resnapped) {
                        objectsResnapped += 1;
                    }
                    ho.ResnapEnd(timing, args.BeatDivisors);
                    ho.ResnapPosition(mode, circleSize);
                }
                UpdateProgressBar(worker, 18);

                // Resnap Kiai toggles
                foreach (TimingPoint tp in kiaiToggles) {
                    tp.ResnapSelf(timing, args.BeatDivisors);
                }
                UpdateProgressBar(worker, 27);

                // Resnap SliderVelocity changes
                foreach (TimingPoint tp in svChanges) {
                    tp.ResnapSelf(timing, args.BeatDivisors);
                }
                UpdateProgressBar(worker, 36);
            }

            if (args.ResnapBookmarks) {
                // Resnap the bookmarks
                List<double> bookmarks = beatmap.GetBookmarks();
                List<double> newBookmarks = bookmarks.Select(o => timing.Resnap(o, args.BeatDivisors)).ToList();

                // Remove duplicate bookmarks
                newBookmarks = newBookmarks.Distinct().ToList();
                beatmap.SetBookmarks(newBookmarks);

                UpdateProgressBar(worker, 45);
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

            // Add redlines
            var redlines = timing.Redlines;
            foreach (TimingPoint tp in redlines) {
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true, fuzzyness:Precision.DOUBLE_EPSILON));
            }
            UpdateProgressBar(worker, 55);

            // Add SliderVelocity changes for taiko and mania
            if (mode == GameMode.Taiko || mode == GameMode.Mania) {
                foreach (TimingPoint tp in svChanges) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
            }
            UpdateProgressBar(worker, 60);

            // Add Kiai toggles
            foreach (TimingPoint tp in kiaiToggles) {
                timingPointsChanges.Add(new TimingPointsChange(tp, kiai: true));
            }
            UpdateProgressBar(worker, 65);

            // Add Hitobject stuff
            foreach (HitObject ho in beatmap.HitObjects) {
                if (ho.IsSlider) // SliderVelocity changes
                {
                    TimingPoint tp = ho.TimingPoint.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SliderVelocity;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
                // Body hitsounds
                bool vol = (ho.IsSlider && args.VolumeSliders) || (ho.IsSpinner && args.VolumeSpinners);
                bool sam = (ho.IsSlider && args.SampleSetSliders && ho.SampleSet == 0);
                bool ind = (ho.IsSlider && args.SampleSetSliders);
                bool samplesetActuallyChanged = false;
                foreach (TimingPoint tp in ho.BodyHitsounds) {
                    if (tp.Volume == 5 && args.RemoveMuting) {
                        vol = false;  // Removing sliderbody silencing
                        ind = false;  // Removing silent custom index
                    }
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam));
                    if (tp.SampleSet != ho.HitsoundTimingPoint.SampleSet) {
                        samplesetActuallyChanged = args.SampleSetSliders && ho.SampleSet == 0; }  // True for sampleset change in sliderbody
                }
                if (ho.IsSlider && (!samplesetActuallyChanged) && ho.SampleSet == 0)  // Case can put sampleset on sliderbody
                {
                    ho.SampleSet = ho.HitsoundTimingPoint.SampleSet;
                }
                if (ho.IsSlider && samplesetActuallyChanged) // Make it start out with the right sampleset
                {
                    TimingPoint tp = ho.HitsoundTimingPoint.Copy();
                    tp.Offset = ho.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true));
                }
            }
            UpdateProgressBar(worker, 75);

            // Add timeline hitsounds
            foreach (TimelineObject tlo in timeline.TimelineObjects) {
                // Change the samplesets in the hitobjects
                if (tlo.Origin.IsCircle) {
                    tlo.Origin.SampleSet = tlo.FenoSampleSet;
                    tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                    if (mode == GameMode.Mania) {
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                } else if (tlo.Origin.IsSlider) {
                    tlo.Origin.EdgeHitsounds[tlo.Repeat] = tlo.GetHitsounds();
                    tlo.Origin.EdgeSampleSets[tlo.Repeat] = tlo.FenoSampleSet;
                    tlo.Origin.EdgeAdditionSets[tlo.Repeat] = tlo.FenoAdditionSet;
                    if (tlo.Origin.EdgeAdditionSets[tlo.Repeat] == tlo.Origin.EdgeSampleSets[tlo.Repeat])  // Simplify additions to auto
                    {
                        tlo.Origin.EdgeAdditionSets[tlo.Repeat] = 0;
                    }
                } else if (tlo.Origin.IsSpinner) {
                    if (tlo.Repeat == 1) {
                        tlo.Origin.SampleSet = tlo.FenoSampleSet;
                        tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                    }
                } else if (tlo.Origin.IsHoldNote) {
                    if (tlo.Repeat == 0) {
                        tlo.Origin.SampleSet = tlo.FenoSampleSet;
                        tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                }
                if (tlo.Origin.AdditionSet == tlo.Origin.SampleSet)  // Simplify additions to auto
                {
                    tlo.Origin.AdditionSet = 0;
                }
                if (tlo.HasHitsound) // Add greenlines for custom indexes and volumes
                {
                    TimingPoint tp = tlo.HitsoundTimingPoint.Copy();

                    bool doUnmute = tlo.FenoSampleVolume == 5 && args.RemoveMuting;
                    bool doMute = args.RemoveUnclickableHitsounds && !args.RemoveMuting &&
                                  !(tlo.IsCircle || tlo.IsSliderHead || tlo.IsHoldnoteHead);

                    bool ind = !tlo.UsesFilename && !doUnmute;  // Index doesnt have to change if custom is overridden by Filename
                    bool vol = !doUnmute;  // Remove volume change muted

                    // Index doesn't have to change if the sample it plays currently is the same as the sample it would play with the previous index
                    if (ind) {
                        List<string> nativeSamples = tlo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);

                        int oldIndex = tlo.FenoCustomIndex;
                        int newIndex = tlo.FenoCustomIndex;
                        double latest = double.NegativeInfinity;
                        foreach (TimingPointsChange tpc in timingPointsChanges) {
                            if (tpc.Index && tpc.MyTP.Offset <= tlo.Time && tpc.MyTP.Offset >= latest) {
                                newIndex = tpc.MyTP.SampleIndex;
                                latest = tpc.MyTP.Offset;
                            }
                        }

                        tp.SampleIndex = newIndex;
                        tlo.GiveHitsoundTimingPoint(tp);
                        List<string> newSamples = tlo.GetFirstPlayingFilenames(mode, mapDir, firstSamples);
                        if (nativeSamples.SequenceEqual(newSamples)) {
                            // Index changes dont change sound
                            tp.SampleIndex = newIndex;
                        } else {
                            tp.SampleIndex = oldIndex;
                        }
                        
                        tlo.GiveHitsoundTimingPoint(tp);
                    }

                    tp.Offset = tlo.Time;
                    tp.SampleIndex = tlo.FenoCustomIndex;
                    tp.Volume = doMute ? 5 : tlo.FenoSampleVolume;

                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind));
                }
            }
            UpdateProgressBar(worker, 85);
            
            // Replace the old timingpoints
            timing.Clear();
            TimingPointsChange.ApplyChanges(timing, timingPointsChanges);
            beatmap.GiveObjectsGreenlines();

            UpdateProgressBar(worker, 90);

            // Remove unused samples
            if (args.RemoveUnusedSamples)
                RemoveUnusedSamples(mapDir);

            // Complete progressbar
            UpdateProgressBar(worker, 100);

            return new MapCleanerResult(objectsResnapped, samplesRemoved);
        }

        public static int RemoveUnusedSamples(string mapDir) {
            // Collect all the used samples
            HashSet<string> allFilenames = new HashSet<string>();
            bool anySpinners = false;

            List<string> beatmaps = Directory.GetFiles(mapDir, "*.osu", SearchOption.TopDirectoryOnly).ToList();
            foreach (string path in beatmaps) {
                BeatmapEditor editor = new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;

                GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;
                double sliderTickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue;

                if (!anySpinners)
                    anySpinners = mode == 0 && beatmap.HitObjects.Any(o => o.IsSpinner);

                allFilenames.Add(beatmap.General["AudioFilename"].Value.Trim());

                foreach (HitObject ho in beatmap.HitObjects) {
                    allFilenames.UnionWith(ho.GetPlayingBodyFilenames(sliderTickRate, false));
                }

                foreach (TimelineObject tlo in beatmap.GetTimeline().TimelineObjects) {
                    allFilenames.UnionWith(tlo.GetPlayingFilenames(mode, false));
                }

                foreach (StoryboardSoundSample sbss in beatmap.StoryboardSoundSamples) {
                    allFilenames.Add(sbss.FilePath);
                }
            }

            List<string> storyboards = Directory.GetFiles(mapDir, "*.osb", SearchOption.TopDirectoryOnly).ToList();
            foreach (string path in storyboards) {
                StoryboardEditor editor = new StoryboardEditor(path);
                StoryBoard storyboard = editor.StoryBoard;

                foreach (StoryboardSoundSample sbss in storyboard.StoryboardSoundSamples) {
                    allFilenames.Add(sbss.FilePath);
                }
            }

            // Only if there are spinners in standard you may have spinnerspin and spinnerbonus
            if (anySpinners)
                allFilenames.UnionWith(new[] { "spinnerspin", "spinnerbonus" });

            // We don't do extensions in osu!
            HashSet<string> usedFilenames = new HashSet<string>(allFilenames.Select(Path.GetFileNameWithoutExtension));

            // Get the sound files
            var extList = new[] { ".wav", ".ogg", ".mp3" };
            DirectoryInfo di = new DirectoryInfo(mapDir);
            List<FileInfo> sampleFiles = di.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                            .Where(n => extList.Contains(n.Extension, StringComparer.OrdinalIgnoreCase)).ToList();

            int removed = 0;
            foreach (FileInfo fi in sampleFiles) {
                string extless = Path.GetFileNameWithoutExtension(fi.Name);
                if (!(usedFilenames.Contains(extless) || BeatmapSkinnableSamples.Any(o => Regex.IsMatch(extless, o)))) {
                    fi.Delete();
                    //Console.WriteLine($"Deleting sample {fi.Name}");
                    removed++;
                }
            }

            return removed;
        }

        public static readonly string[] BeatmapSkinnableSamples = new string[] {
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
            "pause-loop"
        };

        private static void UpdateProgressBar(BackgroundWorker worker, int progress) {
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(progress);
            }
        }
    }
}
