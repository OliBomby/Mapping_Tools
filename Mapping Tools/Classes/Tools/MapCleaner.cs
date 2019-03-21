using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.Tools {
    class MapCleaner {
        public struct Arguments {
            public string Path;
            public bool VolumeSliders;
            public bool SamplesetSliders;
            public bool VolumeSpinners;
            public bool RemoveSliderendMuting;
            public bool ResnapObjects;
            public bool ResnapBookmarks;
            public int Snap1;
            public int Snap2;
            public bool RemoveUnclickableHitsounds;

            public Arguments(string path, bool volumeSliders, bool samplesetSliders, bool volumeSpinners, bool removeSliderendMuting, bool resnapObjects, bool resnapBookmarks,
                             int snap1, int snap2, bool removeUnclickableHitsounds) {
                Path = path;
                VolumeSliders = volumeSliders;
                SamplesetSliders = samplesetSliders;
                VolumeSpinners = volumeSpinners;
                RemoveSliderendMuting = removeSliderendMuting;
                ResnapObjects = resnapObjects;
                ResnapBookmarks = resnapBookmarks;
                Snap1 = snap1;
                Snap2 = snap2;
                RemoveUnclickableHitsounds = removeUnclickableHitsounds;
            }

            public static Arguments BasicResnap(string path) {
                return new Arguments(path, true, true, true, false, true, false, 16, 12, false);
            }
        }

        /// <summary>
        /// Cleans a map.
        /// </summary>
        /// <param name="beatmap">The beatmap that is going to be cleaned.</param>
        /// <param name="arguments">The arguments for how to clean the beatmap.</param>
        /// <param name="worker">The BackgroundWorker for updating progress.</param>
        /// <returns>Number of resnapped objects.</returns>
        public static int CleanMap(Beatmap beatmap, Arguments arguments, BackgroundWorker worker = null) {
            Timing timing = beatmap.BeatmapTiming;
            Timeline timeline = beatmap.GetTimeline();

            int mode = beatmap.General["Mode"].Value;
            int objectsResnapped = 0;

            // Count total stages
            int maxStages = 11;

            // Collect Kiai toggles and SV changes for mania/taiko
            List<TimingPoint> kiaiToggles = new List<TimingPoint>();
            List<TimingPoint> svChanges = new List<TimingPoint>();
            bool lastKiai = false;
            double lastSV = -100;
            for (int i = 0; i < timing.TimingPoints.Count; i++) {
                TimingPoint tp = timing.TimingPoints[i];
                if (tp.Kiai != lastKiai) {
                    kiaiToggles.Add(tp.Copy());
                    lastKiai = tp.Kiai;
                }
                if (tp.Inherited) {
                    lastSV = -100;
                } else {
                    if (tp.MpB != lastSV) {
                        svChanges.Add(tp.Copy());
                        lastSV = tp.MpB;
                    }
                }
                UpdateProgressbar(worker, (double)i / timing.TimingPoints.Count, 0, maxStages);
            }

            // Resnap shit
            if (arguments.ResnapObjects) {
                // Resnap all objects
                for (int i = 0; i < beatmap.HitObjects.Count; i++) {
                    HitObject ho = beatmap.HitObjects[i];
                    bool resnapped = ho.ResnapSelf(timing, arguments.Snap1, arguments.Snap2);
                    if (resnapped) {
                        objectsResnapped += 1;
                    }
                    ho.ResnapEnd(timing, arguments.Snap1, arguments.Snap2);
                    UpdateProgressbar(worker, (double)i / beatmap.HitObjects.Count, 1, maxStages);
                }

                // Resnap Kiai toggles and SV changes
                for (int i = 0; i < kiaiToggles.Count; i++) {
                    TimingPoint tp = kiaiToggles[i];
                    tp.ResnapSelf(timing, arguments.Snap1, arguments.Snap2);
                    UpdateProgressbar(worker, (double)i / kiaiToggles.Count, 2, maxStages);
                }
                for (int i = 0; i < svChanges.Count; i++) {
                    TimingPoint tp = svChanges[i];
                    tp.ResnapSelf(timing, arguments.Snap1, arguments.Snap2);
                    UpdateProgressbar(worker, (double)i / svChanges.Count, 3, maxStages);
                }
            }

            if (arguments.ResnapBookmarks) {
                // Resnap the bookmarks
                List<double> newBookmarks = new List<double>();
                List<double> bookmarks = beatmap.GetBookmarks();
                for (int i = 0; i < bookmarks.Count; i++) {
                    double bookmark = bookmarks[i];
                    newBookmarks.Add(Math.Floor(timing.Resnap(bookmark, arguments.Snap1, arguments.Snap2)));
                    UpdateProgressbar(worker, (double)i / bookmarks.Count, 4, maxStages);
                }
                beatmap.SetBookmarks(newBookmarks);
            }

            // Maybe mute unclickable timelineobjects
            if (arguments.RemoveUnclickableHitsounds) {
                foreach (TimelineObject tlo in timeline.TimeLineObjects) {
                    if (!(tlo.IsCircle || tlo.IsSliderHead || tlo.IsHoldnoteHead))  // Not clickable
                    {
                        tlo.FenoSampleVolume = 5;  // 5% volume mute
                    }
                }
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();
            // Add redlines
            List<TimingPoint> redlines = timing.GetAllRedlines();
            for (int i = 0; i < redlines.Count; i++) {
                TimingPoint tp = redlines[i];
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true));
                UpdateProgressbar(worker, (double)i / redlines.Count, 5, maxStages);
            }
            // Add SV changes for taiko and mania
            if (mode == 1 || mode == 3) {
                for (int i = 0; i < svChanges.Count; i++) {
                    TimingPoint tp = svChanges[i];
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                    UpdateProgressbar(worker, (double)i / svChanges.Count, 6, maxStages);
                }
            }
            // Add Kiai toggles
            for (int i = 0; i < kiaiToggles.Count; i++) {
                TimingPoint tp = kiaiToggles[i];
                timingPointsChanges.Add(new TimingPointsChange(tp, kiai: true));
                UpdateProgressbar(worker, (double)i / kiaiToggles.Count, 7, maxStages);
            }
            // Add Hitobject stuff
            for (int i = 0; i < beatmap.HitObjects.Count; i++) {
                HitObject ho = beatmap.HitObjects[i];
                if (ho.IsSlider) // SV changes
                {
                    TimingPoint tp = ho.TP.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SV;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
                // Body hitsounds
                bool vol = (ho.IsSlider && arguments.VolumeSliders) || (ho.IsSpinner && arguments.VolumeSpinners);
                bool sam = (ho.IsSlider && arguments.SamplesetSliders && ho.SampleSet == 0);
                bool ind = (ho.IsSlider && arguments.SamplesetSliders);
                bool samplesetActuallyChanged = false;
                foreach (TimingPoint tp in ho.BodyHitsounds) {
                    if (tp.Volume == 5 && arguments.RemoveSliderendMuting) {
                        vol = false; }  // Removing sliderbody silencing
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam));
                    if (tp.SampleSet != ho.HitsoundTP.SampleSet) {
                        samplesetActuallyChanged = arguments.SamplesetSliders && ho.SampleSet == 0; }  // True for sampleset change in sliderbody
                }
                if (ho.IsSlider && (!samplesetActuallyChanged) && ho.SampleSet == 0)  // Case can put sampleset on sliderbody
                {
                    ho.SampleSet = ho.HitsoundTP.SampleSet;
                    ho.SliderExtras = true;
                }
                if (ho.IsSlider && samplesetActuallyChanged) // Make it start out with the right sampleset
                {
                    TimingPoint tp = ho.HitsoundTP.Copy();
                    tp.Offset = ho.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true));
                }
                UpdateProgressbar(worker, (double)i / beatmap.HitObjects.Count, 8, maxStages);
            }
            // Add timeline hitsounds
            for (int i = 0; i < timeline.TimeLineObjects.Count; i++) {
                TimelineObject tlo = timeline.TimeLineObjects[i];
                // Change the samplesets in the hitobjects
                if (tlo.Origin.IsCircle) {
                    tlo.Origin.SampleSet = tlo.FenoSampleSet;
                    tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                    if (mode == 3) {
                        tlo.Origin.CustomIndex = tlo.FenoCustomIndex;
                        tlo.Origin.SampleVolume = tlo.FenoSampleVolume;
                    }
                } else if (tlo.Origin.IsSlider) {
                    tlo.Origin.EdgeHitsounds[tlo.Repeat] = tlo.GetHitsounds();
                    tlo.Origin.EdgeSampleSets[tlo.Repeat] = tlo.FenoSampleSet;
                    tlo.Origin.EdgeAdditionSets[tlo.Repeat] = tlo.FenoAdditionSet;
                    tlo.Origin.SliderExtras = true;
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
                if (mode == 0 && tlo.HasHitsound) // Add greenlines for custom indexes and volumes
                {
                    TimingPoint tp = tlo.Origin.TP.Copy();
                    tp.Offset = tlo.Time;
                    tp.SampleIndex = tlo.FenoCustomIndex;
                    tp.Volume = tlo.FenoSampleVolume;
                    bool ind = !(tlo.Filename != "" && (tlo.IsCircle || tlo.IsHoldnoteHead || tlo.IsSpinnerEnd));  // Index doesnt have to change if custom is overridden by Filename
                    bool vol = !(tp.Volume == 5 && arguments.RemoveSliderendMuting && (tlo.IsSliderEnd || tlo.IsSpinnerEnd));  // Remove volume change if sliderend muting or spinnerend muting
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind));
                }
                UpdateProgressbar(worker, (double)i / timeline.TimeLineObjects.Count, 9, maxStages);
            }


            // Add the new timingpoints
            timingPointsChanges = timingPointsChanges.OrderBy(o => o.TP.Offset).ToList();
            List<TimingPoint> newTimingPoints = new List<TimingPoint>();

            for (int i = 0; i < timingPointsChanges.Count; i++) {
                TimingPointsChange c = timingPointsChanges[i];
                c.AddChange(newTimingPoints, timing);
                UpdateProgressbar(worker, (double)i / timingPointsChanges.Count, 10, maxStages);
            }

            // Replace the old timingpoints
            timing.TimingPoints = newTimingPoints;

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

            return objectsResnapped;
        }

        private static void UpdateProgressbar(BackgroundWorker worker, double fraction, int stage, int maxStages) {
            // Update progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress((int)((fraction + stage) / maxStages * 100));
            }
        }
    }
}
