using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.ExternalFileUtil;

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
            AdjustPatternToBeatmap(patternBeatmap, beatmap, out var parts, out var timingPointsChanges);

            // Keep just the timing point changes which are inside the parts.
            // These timing point changes have everything that is necessary for inside the parts of the pattern. (even timing)
            timingPointsChanges = timingPointsChanges.Where(tpc => parts.Any(part => 
                part.StartTime <= tpc.MyTP.Offset && part.EndTime >= tpc.MyTP.Offset)).ToList();

            // Remove stuff
            if (PatternOverwriteMode != PatternOverwriteMode.NoOverwrite) {
                foreach (var part in parts) {
                    RemovePartOfBeatmap(beatmap, part.StartTime - Padding, part.EndTime + Padding);
                }
            }

            // Add timingpoint changes for each hitobject to make sure they still have the wanted SV and hitsounds (especially near the edges of parts)
            // It is possible for the timingpoint of a hitobject at the start of a part to be outside of the part, so this fixes issues related to that
            timingPointsChanges.AddRange(
                beatmap.HitObjects.Where(ho => ho.TimingPoint != null)
                .Select(GetSvChange));

            if (IncludeHitsounds) {
                timingPointsChanges.AddRange(
                    beatmap.HitObjects.Where(ho => ho.HitsoundTimingPoint != null)
                        .Select(GetHitsoundChange));
            }

            // Apply the changes
            TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

            // Add the hitobjects of the pattern
            beatmap.HitObjects.AddRange(patternBeatmap.HitObjects);

            // Sort hitobjects later so the timingpoints changes from the new hitobjects have priority
            beatmap.SortHitObjects();
        }

        /// <summary>
        /// Creates parts that have at least PartingDistance number of beats of a gap between the parts.
        /// </summary>
        /// <param name="beatmap">The beatmap to partition.</param>
        /// <returns>List of tuples with start time, end time.</returns>
        private List<Part> PartitionBeatmap(Beatmap beatmap) {
            var parts = new List<Part>();

            var firstTime = beatmap.HitObjects[0].Time;
            var lastObject = beatmap.HitObjects[0];
            double lastGap = 0;
            foreach (var ho in beatmap.HitObjects.Skip(1)) {
                var gap = beatmap.BeatmapTiming.GetBeatLength(lastObject.EndTime, ho.Time);

                if (gap >= PartingDistance) {
                    parts.Add(new Part(firstTime, 
                        lastObject.EndTime,
                        lastGap, 
                        beatmap.BeatmapTiming.GetBeatLength(firstTime, lastObject.EndTime)
                        ));

                    firstTime = ho.Time;
                    lastGap = gap;
                }

                lastObject = ho;
            }
            parts.Add(new Part(firstTime, 
                lastObject.EndTime, 
                lastGap, 
                beatmap.BeatmapTiming.GetBeatLength(firstTime, lastObject.EndTime)
                ));

            return parts;
        }

        private class Part {
            public double StartTime;
            public double EndTime;
            public double BeatGap;
            public double BeatLength;
            public List<HitObject> HitObjects;

            public Part(double startTime, double endTime, double beatGap, double beatLength) {
                StartTime = startTime;
                EndTime = endTime;
                BeatGap = beatGap;
                BeatLength = beatLength;
            }
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

        private static TimingPointsChange GetSvChange(HitObject ho) {
            var tp = ho.TimingPoint.Copy();
            tp.Offset = ho.Time;
            tp.Uninherited = false;
            tp.MpB = ho.SliderVelocity;
            return new TimingPointsChange(tp, true);
        }

        private static TimingPointsChange GetHitsoundChange(HitObject ho) {
            var tp = ho.HitsoundTimingPoint.Copy();
            tp.Offset = ho.Time;
            tp.Uninherited = false;
            return new TimingPointsChange(tp, sampleset: true, index: true, volume: true);
        }

        private static TimingPointsChange GetBpmChange(TimingPoint tp, double? customOffset = null) {
            tp = tp.Copy();

            if (!tp.Uninherited) {
                tp.MpB = 1000;
                tp.Uninherited = true;
                tp.Meter = new TempoSignature(4);
                tp.OmitFirstBarLine = false;
            }

            if (customOffset.HasValue) {
                tp.Offset = customOffset.Value;
            }
            
            return new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true);
        }

        /// <summary>
        /// Does a procedure similar to <see cref="MapCleaner"/> which adjusts the pattern so it fits in the beatmap.
        /// It does so according to the options selected in this.
        /// </summary>
        /// <param name="patternBeatmap"></param>
        /// <param name="beatmap"></param>
        /// <param name="parts"></param>
        /// <param name="timingPointsChanges"></param>
        private void AdjustPatternToBeatmap(Beatmap patternBeatmap, Beatmap beatmap, out List<Part> parts, out List<TimingPointsChange> timingPointsChanges) {
            double patternStartTime = patternBeatmap.GetHitObjectStartTime();
            double patternEndTime = patternBeatmap.GetHitObjectEndTime();

            Timing originalTiming = beatmap.BeatmapTiming;

            Timing patternTiming = patternBeatmap.BeatmapTiming;

            GameMode patternMode = (GameMode)patternBeatmap.General["Mode"].IntValue;
            GameMode targetMode = (GameMode)beatmap.General["Mode"].IntValue;

            double patternCircleSize = patternBeatmap.Difficulty["CircleSize"].DoubleValue;

            // Partition the pattern based on the timing in the pattern
            if (PatternOverwriteMode == PatternOverwriteMode.PartitionedOverwrite) {
                parts = PartitionBeatmap(patternBeatmap);
            } else {
                parts = new List<Part> {
                    new Part(patternStartTime, patternEndTime, 0, patternTiming.GetBeatLength(patternStartTime, patternEndTime))
                };
            }

            // Construct a new timing which is a mix of the beatmap and the pattern.
            // If ScaleToNewTiming then use beat relative values to determine the duration of timing sections in the pattern.
            // ScaleToNewTiming must scale all the partitions, timingpoints, hitobjects, and events (if applicable).
            var timingChanges = new List<TimingPointsChange>();
            double lastEndTime = double.NegativeInfinity;
            foreach (var part in parts) {
                var startTime = part.StartTime;
                var endTime = part.EndTime;  // Subtract one to omit BPM changes right on the end of the part.

                // Add the original timing between the last part and this part
                timingChanges.AddRange(originalTiming.GetRedlinesInTimeRange(lastEndTime, startTime - 1).Select(o => GetBpmChange(o)));

                var startOriginalRedline = originalTiming.GetRedlineAtTime(startTime);

                // Minus 1 the offset so its possible to have a custom BPM redline right on the start time if you have 
                // the default BPM redline before it.
                var patternDefaultMpb = patternTiming.GetMpBAtTime(startTime - 1);

                TimingPoint[] inPartRedlines;
                TimingPoint startPartRedline;
                switch (TimingOverwriteMode) {
                    case TimingOverwriteMode.PatternTimingOnly:
                        // Subtract one from the end time to omit BPM changes right on the end of the part.
                        inPartRedlines = patternTiming.GetRedlinesInTimeRange(startTime, endTime - 1).ToArray();
                        startPartRedline = patternTiming.GetRedlineAtTime(startTime);
                        break;
                    case TimingOverwriteMode.InPatternAbsoluteTiming:
                        var tempInPartRedlines = patternTiming.GetRedlinesInTimeRange(startTime, endTime - 1);

                        // Replace all parts in the pattern which have the default BPM to timing from the target beatmap.
                        inPartRedlines = tempInPartRedlines.Select(tp => {
                            if (Precision.AlmostEquals(tp.MpB, patternDefaultMpb)) {
                                var tp2 = originalTiming.GetRedlineAtTime(tp.Offset).Copy();
                                tp2.Offset = tp2.Offset;
                                return tp2;
                            }

                            return tp;
                        }).ToArray();

                        startPartRedline = startOriginalRedline;
                        break;
                    case TimingOverwriteMode.InPatternRelativeTiming:
                        // Multiply mix the pattern timing and the original timing together.
                        // The pattern timing divided by the default BPM will be used as a scalar for the original timing.
                        var tempInPartRedlines2 = patternTiming.GetRedlinesInTimeRange(startTime, endTime - 1);
                        var tempInOriginalRedlines = originalTiming.GetRedlinesInTimeRange(startTime, endTime - 1);

                        // Replace all parts in the pattern which have the default BPM to timing from the target beatmap.
                        inPartRedlines = tempInPartRedlines2.Select(tp => {
                            var tp2 = tp.Copy();
                            tp2.MpB *= originalTiming.GetMpBAtTime(tp.Offset) / patternDefaultMpb;
                            return tp2;
                        }).Concat(tempInOriginalRedlines.Select(tp => {
                            var tp2 = tp.Copy();
                            tp2.MpB *= patternTiming.GetMpBAtTime(tp.Offset) / patternDefaultMpb;
                            return tp2;
                        })).ToArray();

                        startPartRedline = patternTiming.GetRedlineAtTime(startTime).Copy();
                        startPartRedline.MpB *= originalTiming.GetMpBAtTime(startTime) / patternDefaultMpb;
                        break;
                    case TimingOverwriteMode.OriginalTimingOnly:
                    default:
                        // Subtract one from the end time to omit BPM changes right on the end of the part.
                        inPartRedlines = originalTiming.GetRedlinesInTimeRange(startTime, endTime - 1).ToArray();
                        startPartRedline = originalTiming.GetRedlineAtTime(startTime);
                        break;
                }

                timingChanges.AddRange(inPartRedlines.Select(o => GetBpmChange(o)));

                // If the pattern starts with different BPM than the map add an extra redline at the start of the pattern
                // to make sure it the pattern starts out at the right BPM as we only copy the timingpoints during the pattern itself
                // and the redline may be way before that.
                // This will probably only do something on the PatternTimingOnly mode as the other modes make sure
                // the BPM at the start of the pattern will be the same as the original beatmap anyways.
                if (Math.Abs(startPartRedline.MpB - startOriginalRedline.MpB) > Precision.DOUBLE_EPSILON) {
                    // We dont have to add the redline again if its already during the pattern.
                    if (Math.Abs(startPartRedline.Offset - startTime) > Precision.DOUBLE_EPSILON) {
                        timingChanges.Add(GetBpmChange(startPartRedline, startTime));
                    }
                }

                // Do the same thing at the end of the pattern to make sure the BPM goes back to normal after the pattern.
                var endOriginalRedline = originalTiming.GetRedlineAtTime(endTime);
                var endPartRedline = inPartRedlines.Last();
                if (Math.Abs(endPartRedline.MpB - endOriginalRedline.MpB) > Precision.DOUBLE_EPSILON) {
                    // We dont have to add the redline again if its already during the parts in between parts.
                    if (Math.Abs(endOriginalRedline.Offset - endTime) > Precision.DOUBLE_EPSILON) {
                        timingChanges.Add(GetBpmChange(endOriginalRedline, endTime));
                    }
                }

                lastEndTime = endTime;
            }

            // Add the original timing between the last part and the rest of the map
            timingChanges.AddRange(originalTiming.GetRedlinesInTimeRange(lastEndTime, double.PositiveInfinity).Select(o => GetBpmChange(o)));

            // Make the new timing from the timing changes
            Timing newTiming = new Timing(beatmap.BeatmapTiming.SliderMultiplier);
            TimingPointsChange.ApplyChanges(newTiming, timingChanges);

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

            // TODO Scale everything to the new timing starting from the first object in the pattern and keeping the number of beats the same.
            if (ScaleToNewTiming) {

            }

            // Fix SV for the new global SV
            if (FixGlobalSV) {
                var globalSvFactor =  originalTiming.SliderMultiplier / patternTiming.SliderMultiplier;
                foreach (HitObject ho in patternBeatmap.HitObjects) {
                    ho.SliderVelocity *= globalSvFactor;
                }
                foreach (TimingPoint tp in svChanges) {
                    tp.MpB *= globalSvFactor;
                }
            }

            // Recalculate temporal length and re-assign redline for the sliderend resnapping later
            // TODO The temporal length needs to be calculated during the timing generating part so it can return the original BPM at the right time (fuck)
            foreach (var ho in patternBeatmap.HitObjects) {
                ho.UnInheritedTimingPoint = newTiming.GetRedlineAtTime(ho.Time);
                if (ho.IsSlider) {
                    ho.TemporalLength = newTiming.CalculateSliderTemporalLength(ho.Time, ho.PixelLength, ho.SliderVelocity);
                }
            }

            // Resnap everything to the new timing.
            if (SnapToNewTiming) {
                // Resnap all objects
                foreach (HitObject ho in patternBeatmap.HitObjects) {
                    ho.ResnapSelf(newTiming, SnapDivisor1, SnapDivisor2);
                    ho.ResnapEnd(newTiming, SnapDivisor1, SnapDivisor2);
                    ho.ResnapPosition(patternMode, patternCircleSize);  // Resnap to column X positions for mania only
                }

                // Resnap Kiai toggles
                foreach (TimingPoint tp in kiaiToggles) {
                    tp.ResnapSelf(newTiming, SnapDivisor1, SnapDivisor2);
                }

                // Resnap SliderVelocity changes
                foreach (TimingPoint tp in svChanges) {
                    tp.ResnapSelf(newTiming, SnapDivisor1, SnapDivisor2);
                }
            }

            // Collect timeline objects after resnapping
            Timeline patternTimeline = patternBeatmap.GetTimeline();

            // Make new timingpoints changes for the hitsounds and other stuff
            timingPointsChanges = new List<TimingPointsChange>();

            // Add redlines
            List<TimingPoint> redlines = newTiming.GetAllRedlines();
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

                if (!IncludeHitsounds)
                    continue;

                // Body hitsounds
                bool vol = ho.IsSlider || ho.IsSpinner;
                bool sam = ho.IsSlider && ho.SampleSet == 0;
                bool ind = ho.IsSlider;
                foreach (TimingPoint tp in ho.BodyHitsounds) {
                    timingPointsChanges.Add(new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam));
                }
            }

            // Add timeline hitsounds
            foreach (TimelineObject tlo in patternTimeline.TimelineObjects) {
                if (tlo.HasHitsound && IncludeHitsounds) {
                    // Add greenlines for hitsounds
                    TimingPoint tp = tlo.HitsoundTimingPoint.Copy();
                    tp.Offset = tlo.Time;
                    timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true, volume: true, index: true));
                }
            }
            
            // Replace the old timingpoints
            patternTiming.TimingPoints.Clear();
            TimingPointsChange.ApplyChanges(newTiming, timingPointsChanges);

            patternBeatmap.GiveObjectsGreenlines();
            patternBeatmap.CalculateSliderEndTimes();
        }
    }
}