using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    /// <summary>
    /// Helper class for placing a <see cref="OsuPattern"/> into a <see cref="Beatmap"/>.
    /// </summary>
    public class OsuPatternPlacer {
        /// <summary>
        /// Extra time in milliseconds around patterns for removing a wider range of objects in the target beatmap.
        /// </summary>
        public double Padding = 5;
        /// <summary>
        /// Minimum number of beats in between partitions of a pattern.
        /// </summary>
        public double PartingDistance = 4;
        /// <summary>
        /// Determines how to remove the objects in the target beatmap which overlap with the pattern.
        /// </summary>
        public PatternOverwriteMode PatternOverwriteMode = PatternOverwriteMode.PartitionedOverwrite;
        /// <summary>
        /// Determines which timing stuff to keep from the pattern.
        /// </summary>
        public TimingOverwriteMode TimingOverwriteMode = TimingOverwriteMode.InPatternRelativeTiming;
        public bool IncludeHitsounds = true;
        public bool ScaleToNewCircleSize = true;
        public bool ScaleToNewTiming = true;
        public bool SnapToNewTiming = true;
        public int SnapDivisor1 = 16;
        public int SnapDivisor2 = 12;
        public bool FixGlobalSV = true;
        public bool FixColourHax = true;
        public bool FixStackLeniency = true;
        public bool FixTickRate = true;
        /// <summary>
        /// Optional scaling factor for changing the size of the pattern before placing it into the target beatmap.
        /// </summary>
        public double CustomScale = 1;
        /// <summary>
        /// Optional rotation in radians for rotating the pattern before placing it into the target beatmap.
        /// </summary>
        public double CustomRotate = 0;

        /// <summary>
        /// Places each hit object of the pattern beatmap into the other beatmap and applies timingpoint changes to copy timingpoint stuff aswell.
        /// The given pattern beatmap could be modified by this method if protectBeatmapPattern is false.
        /// </summary>
        /// <param name="patternBeatmap">The pattern beatmap to be placed into the beatmap.</param>
        /// <param name="beatmap">To beatmap to place the pattern in.</param>
        /// <param name="time">The time at which to place the first hit object of the pattern beatmap.</param>
        /// <param name="protectBeatmapPattern">If true, copies the pattern beatmap to prevent the pattern beatmap from being modified by this method.</param>
        public void PlaceOsuPatternAtTime(Beatmap patternBeatmap, Beatmap beatmap, double time = double.NaN, bool protectBeatmapPattern = true) {
            double offset = double.IsNaN(time) ? 0 : time - patternBeatmap.GetHitObjectStartTime();
            PlaceOsuPattern(patternBeatmap, beatmap, offset, protectBeatmapPattern);
        }

        /// <summary>
        /// Places each hit object of the pattern beatmap into the other beatmap and applies timingpoint changes to copy timingpoint stuff aswell.
        /// The given pattern beatmap could be modified by this method if protectBeatmapPattern is false.
        /// </summary>
        /// <param name="patternBeatmap">The pattern beatmap to be placed into the beatmap.</param>
        /// <param name="beatmap">To beatmap to place the pattern in.</param>
        /// <param name="offset">An offset to move the pattern beatmap in time with.</param>
        /// <param name="protectBeatmapPattern">If true, copies the pattern beatmap to prevent the pattern beatmap from being modified by this method.</param>
        public void PlaceOsuPattern(Beatmap patternBeatmap, Beatmap beatmap, double offset = 0, bool protectBeatmapPattern = true) {
            if (protectBeatmapPattern) {
                // Copy so the original pattern doesnt get changed
                patternBeatmap = patternBeatmap.DeepCopy();
            }

            if (Math.Abs(offset) > Precision.DOUBLE_EPSILON) {
                patternBeatmap.OffsetTime(offset);
            }

            // We adjust the pattern first so it alligns with the beatmap.
            // The right timing is applied and optional pre-processing is applied.
            // Sliderends and object timingpoints get recalculated.
            AdjustPatternToBeatmap(patternBeatmap, beatmap);

            // Partition the pattern beatmap
            List<Tuple<double, double>> parts;
            if (PatternOverwriteMode == PatternOverwriteMode.PartitionedOverwrite) {
                parts = PartitionBeatmap(patternBeatmap);
            }
            else {
                parts = new List<Tuple<double, double>> {
                    new Tuple<double, double>(patternBeatmap.GetHitObjectStartTime(), patternBeatmap.GetHitObjectEndTime())
                };
            }

            // Remove stuff
            if (PatternOverwriteMode != PatternOverwriteMode.NoOverwrite) {
                foreach (var part in parts) {
                    RemovePartOfBeatmap(beatmap, part.Item1 - Padding, part.Item2 + Padding);
                }
            }

            // Add the hitobjects of the pattern
            beatmap.HitObjects.AddRange(patternBeatmap.HitObjects);

            // Add timingpoint changes for each timingpoint in a part in the pattern
            var timingPointsChanges = new List<TimingPointsChange>();
            foreach (var part in parts) {
                timingPointsChanges.AddRange(
                    patternBeatmap.BeatmapTiming.TimingPoints.Where(tp => tp.Offset >= part.Item1 - Padding &&
                                                                          tp.Offset <= part.Item2 + Padding)
                    .Select(tp => GetTimingPointsChange(tp, true, true)));
            }

            // Add timingpoint changes for each hitobject to make sure they still have the wanted SV and hitsounds (especially near the edges of parts)
            // It is possible for the timingpoint of a hitobject at the start of a part to be outside of the part, so this fixes issues related to that
            timingPointsChanges.AddRange(
                beatmap.HitObjects.Where(ho => ho.TimingPoint != null)
                .Select(ho => GetTimingPointsChange(ho, true, true)));

            // Apply the changes
            TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

            // Sort hitobjects later so the timingpoints changes from the new hitobjects have priority
            beatmap.SortHitObjects();
        }

        /// <summary>
        /// Creates parts that have at least PartingDistance number of beats of a gap between the parts.
        /// </summary>
        /// <param name="beatmap">The beatmap to partition.</param>
        /// <returns>List of tuples with start time, end time.</returns>
        private List<Tuple<double, double>> PartitionBeatmap(Beatmap beatmap) {
            List<Tuple<double, double>> parts = new List<Tuple<double, double>>();

            var firstTime = beatmap.HitObjects[0].Time;
            var lastObject = beatmap.HitObjects[0];
            foreach (var ho in beatmap.HitObjects.Skip(1)) {
                var gap = beatmap.BeatmapTiming.GetBeatLength(lastObject.EndTime, ho.Time);

                if (gap >= PartingDistance) {
                    parts.Add(new Tuple<double, double>(firstTime, lastObject.EndTime));
                    firstTime = ho.Time;
                }

                lastObject = ho;
            }
            parts.Add(new Tuple<double, double>(firstTime, lastObject.EndTime));

            return parts;
        }

        /// <summary>
        /// Removes hitobjects and timingpoints in the beatmap between the start and the end time
        /// </summary>
        /// <param name="beatmap"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private static void RemovePartOfBeatmap(Beatmap beatmap, double startTime, double endTime) {
            beatmap.HitObjects.RemoveAll(h => h.Time >= startTime && h.Time <= endTime);
            beatmap.BeatmapTiming.TimingPoints.RemoveAll(tp => tp.Offset >= startTime && tp.Offset <= endTime);
        }

        private static TimingPointsChange GetTimingPointsChange(HitObject ho, bool sv, bool hs) {
            var tp = ho.TimingPoint.Copy();
            tp.Offset = ho.Time;
            tp.Uninherited = false;
            tp.MpB = ho.SliderVelocity;
            return new TimingPointsChange(tp, sv, false, hs, hs, hs);
        }

        private static TimingPointsChange GetTimingPointsChange(TimingPoint tp, bool sv, bool hs) {
            tp = tp.Copy();
            tp.MpB = tp.Uninherited ? -100 : tp.MpB;
            tp.Uninherited = false;
            return new TimingPointsChange(tp, sv, false, hs, hs, hs);
        }

        /// <summary>
        /// Does a procedure similar to <see cref="MapCleaner"/> which adjusts the pattern so it fits in the beatmap.
        /// It does so according to the options selected in this.
        /// </summary>
        /// <param name="patternBeatmap"></param>
        /// <param name="beatmap"></param>
        private void AdjustPatternToBeatmap(Beatmap patternBeatmap, Beatmap beatmap) {
            double patternStartTime = patternBeatmap.GetHitObjectStartTime();
            Timing patternTiming = patternBeatmap.BeatmapTiming;
            Timeline patternTimeline = patternBeatmap.GetTimeline();
            
            TimingPoint firstPatternRedline = patternTiming.GetRedlineAtTime(patternStartTime);
            double firstPatternMpb = firstPatternRedline.MpB;

            GameMode patternMode = (GameMode)patternBeatmap.General["Mode"].IntValue;
            GameMode targetMode = (GameMode)beatmap.General["Mode"].IntValue;

            double patternCircleSize = patternBeatmap.Difficulty["CircleSize"].DoubleValue;

            // Construct a new timing which is a mix of the beatmap and the pattern.
            // If ScaleToNewTiming then use beat relative values to determine the duration of timing sections in the pattern.
            // Don't care about partitions. Just include the full timing of the pattern. Otherwise it'd get too complicated.
            Timing targetTiming;
            switch (TimingOverwriteMode) {
                case TimingOverwriteMode.PatternTimingOnly:
                    targetTiming = patternTiming;
                    // If the pattern starts with different BPM than the map add an extra redline at the start of the pattern
                    // to make sure it the pattern starts out at the right BPM as we only copy the timingpoints during the pattern itself
                    // and the redline may be way before that.
                    if (Math.Abs(firstPatternRedline.MpB - beatmap.BeatmapTiming.GetMpBAtTime(patternStartTime)) > Precision.DOUBLE_EPSILON) {
                        var firstRedlineCopy = firstPatternRedline.Copy();
                        // We dont have to add the redline again if its already during the pattern.
                        if (Math.Abs(firstRedlineCopy.Offset - patternStartTime) > Precision.DOUBLE_EPSILON) {
                            firstRedlineCopy.Offset = patternStartTime;
                            targetTiming.TimingPoints.Add(firstRedlineCopy);
                            targetTiming.Sort();
                        }
                    }
                    break;
                // These other cases dont need to add the extra redline as they make sure the BPM at the start of the pattern will be the same
                // as the BPM at that part in the target map. Basically just using the target map timing at the start of the pattern.
                case TimingOverwriteMode.InPatternAbsoluteTiming:
                    // Replace all parts in the pattern which have the default MpB to timing from the target beatmap.
                    break;
                case TimingOverwriteMode.InPatternRelativeTiming:
                    // Scale all timing in the pattern such that the first BPM matches with BPM from the target beatmap.
                    break;
                // case TimingOverwriteMode.OriginalTimingOnly:
                default:
                    targetTiming = beatmap.BeatmapTiming;
                    break;
            }

            // Collect Kiai toggles and SliderVelocity changes for mania/taiko
            List<TimingPoint> kiaiToggles = new List<TimingPoint>();
            List<TimingPoint> svChanges = new List<TimingPoint>();
            bool lastKiai = false;
            double lastSV = -100;
            foreach (TimingPoint tp in patternTiming.TimingPoints) {
                if (tp.Kiai != lastKiai) {
                    kiaiToggles.Add(tp.Copy());
                    lastKiai = tp.Kiai;
                }
                if (tp.Uninherited) {
                    lastSV = -100;
                } else {
                    if (Math.Abs(tp.MpB - lastSV) > Precision.DOUBLE_EPSILON) {
                        svChanges.Add(tp.Copy());
                        lastSV = tp.MpB;
                    }
                }
            }

            // Scale everything to the new timing starting from the first object in the pattern and keeping the number of beats the same.
            if (ScaleToNewTiming) {

            }
            // Resnap everything to the new timing.
            if (SnapToNewTiming) {
                // Resnap all objects
                foreach (HitObject ho in patternBeatmap.HitObjects) {
                    ho.ResnapSelf(patternTiming, SnapDivisor1, SnapDivisor2);
                    ho.ResnapEnd(patternTiming, SnapDivisor1, SnapDivisor2);
                    ho.ResnapPosition(patternMode, patternCircleSize);  // Resnap to column X positions for mania only
                }

                // Resnap Kiai toggles
                foreach (TimingPoint tp in kiaiToggles) {
                    tp.ResnapSelf(patternTiming, SnapDivisor1, SnapDivisor2);
                }

                // Resnap SliderVelocity changes
                foreach (TimingPoint tp in svChanges) {
                    tp.ResnapSelf(patternTiming, SnapDivisor1, SnapDivisor2);
                }
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

            // Add redlines
            List<TimingPoint> redlines = targetTiming.GetAllRedlines();
            foreach (TimingPoint tp in redlines) {
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true));
            }

            // Add SliderVelocity changes for taiko and mania
            if (patternMode == GameMode.Taiko || patternMode == GameMode.Mania) {
                foreach (TimingPoint tp in svChanges) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }
            }

            // Add Kiai toggles
            foreach (TimingPoint tp in kiaiToggles) {
                timingPointsChanges.Add(new TimingPointsChange(tp, kiai: true));
            }

            // Add Hitobject stuff
            foreach (HitObject ho in patternBeatmap.HitObjects) {
                if (ho.IsSlider) // SliderVelocity changes
                {
                    TimingPoint tp = ho.TimingPoint.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SliderVelocity;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }

                // Body hitsounds
                bool vol = ho.IsSlider || (ho.IsSpinner);
                bool sam = ho.IsSlider && ho.SampleSet == 0;
                bool ind = ho.IsSlider;
                bool samplesetActuallyChanged = false;
                foreach (TimingPoint tp in ho.BodyHitsounds) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam));
                    if (tp.SampleSet != ho.HitsoundTimingPoint.SampleSet) {
                        samplesetActuallyChanged = ho.SampleSet == 0; }  // True for sampleset change in sliderbody
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

            // Add timeline hitsounds
            foreach (TimelineObject tlo in patternTimeline.TimelineObjects) {
                // Change the samplesets in the hitobjects
                if (tlo.Origin.IsCircle) {
                    tlo.Origin.SampleSet = tlo.FenoSampleSet;
                    tlo.Origin.AdditionSet = tlo.FenoAdditionSet;
                    if (patternMode == GameMode.Mania) {
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

                    bool ind = !tlo.UsesFilename;  // Index doesnt have to change if custom is overridden by Filename
                    const bool vol = true;

                    tp.Offset = tlo.Time;
                    tp.SampleIndex = tlo.FenoCustomIndex;
                    tp.Volume = tlo.FenoSampleVolume;

                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind));
                }
            }
            
            // Replace the old timingpoints
            patternTiming.TimingPoints.Clear();
            TimingPointsChange.ApplyChanges(patternTiming, timingPointsChanges);

            patternBeatmap.GiveObjectsGreenlines();
            patternBeatmap.CalculateSliderEndTimes();
        }
    }
}