using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.MapCleanerStuff;

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
        public TimingOverwriteMode TimingOverwriteMode = TimingOverwriteMode.OriginalTimingOnly;
        public bool IncludeHitsounds = false;
        public bool IncludeKiai = false;
        public bool ScaleToNewCircleSize = false;
        public bool ScaleToNewTiming = true;
        public bool SnapToNewTiming = true;
        public IBeatDivisor[] BeatDivisors = RationalBeatDivisor.GetDefaultBeatDivisors();
        public bool FixGlobalSv = true;
        public bool FixBpmSv = true;
        public bool FixColourHax = true;
        public bool FixStackLeniency = false;
        public bool FixTickRate = false;
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

            // Do the offset
            if (Math.Abs(offset) > Precision.DOUBLE_EPSILON) {
                patternBeatmap.OffsetTime(offset);
            }

            // We adjust the pattern first so it alligns with the beatmap.
            // The right timing is applied and optional pre-processing is applied.
            // Sliderends and object timingpoints get recalculated.
            PreparePattern(patternBeatmap, beatmap, out var parts, out var timingPointsChanges);

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

            // Sort hitobjects
            beatmap.SortHitObjects();

            if (FixColourHax) {
                beatmap.FixComboSkip();
            }

            beatmap.GiveObjectsGreenlines();
            beatmap.CalculateSliderEndTimes();
        }

        /// <summary>
        /// Creates parts that have at least PartingDistance number of beats of a gap between the parts.
        /// </summary>
        /// <param name="beatmap">The beatmap to partition.</param>
        /// <param name="beatMode">Set to true if the beatmap uses beat time.</param>
        /// <returns>List of tuples with start time, end time.</returns>
        private List<Part> PartitionBeatmap(Beatmap beatmap, bool beatMode) {
            var parts = new List<Part>();

            int startIndex = 0;
            for (int i = 1; i < beatmap.HitObjects.Count; i++) {
                var gap = beatMode ?
                    beatmap.HitObjects[i].Time - beatmap.HitObjects[i-1].GetEndTime(false) :
                    beatmap.BeatmapTiming.GetBeatLength(beatmap.HitObjects[i-1].GetEndTime(), beatmap.HitObjects[i].Time);

                if (Precision.AlmostBigger(gap, PartingDistance)) {
                    parts.Add(new Part(beatmap.HitObjects[startIndex].Time,
                        beatmap.HitObjects[i-1].GetEndTime(!beatMode), 
                        beatmap.HitObjects.GetRange(startIndex, i - startIndex)));

                    startIndex = i;
                }
            }
            parts.Add(new Part(beatmap.HitObjects[startIndex].Time,
                beatmap.HitObjects[beatmap.HitObjects.Count-1].GetEndTime(!beatMode), 
                beatmap.HitObjects.GetRange(startIndex, beatmap.HitObjects.Count - startIndex)));

            return parts;
        }

        private class Part {
            public double StartTime;
            public double EndTime;
            public readonly List<HitObject> HitObjects;

            public Part(double startTime, double endTime, List<HitObject> hitObjects) {
                StartTime = startTime;
                EndTime = endTime;
                HitObjects = hitObjects;
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
            beatmap.BeatmapTiming.RemoveAll(tp => tp.Offset >= startTime && tp.Offset <= endTime);
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

        /// <summary>
        /// Does a procedure similar to <see cref="MapCleaner"/> which adjusts the pattern so it fits in the beatmap.
        /// It does so according to the options selected in this.
        /// </summary>
        /// <param name="patternBeatmap"></param>
        /// <param name="beatmap"></param>
        /// <param name="parts"></param>
        /// <param name="timingPointsChanges"></param>
        private void PreparePattern(Beatmap patternBeatmap, Beatmap beatmap, out List<Part> parts, out List<TimingPointsChange> timingPointsChanges) {
            double patternStartTime = patternBeatmap.GetHitObjectStartTime();

            Timing originalTiming = beatmap.BeatmapTiming;
            Timing patternTiming = patternBeatmap.BeatmapTiming;

            GameMode targetMode = (GameMode)beatmap.General["Mode"].IntValue;

            double originalCircleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
            double patternCircleSize = patternBeatmap.Difficulty["CircleSize"].DoubleValue;

            double originalTickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue;
            double patternTickRate = patternBeatmap.Difficulty["SliderTickRate"].DoubleValue;

            // Don't include SV changes if it is based on nothing
            bool includePatternSliderVelocity = patternTiming.Count > 0;

            // Avoid including hitsounds if there are no timingpoints to get hitsounds from
            bool includeTimingPointHitsounds = IncludeHitsounds && patternTiming.Count > 0;

            // Don't scale to new timing if the pattern has no timing to speak of
            bool scaleToNewTiming = ScaleToNewTiming && patternTiming.Redlines.Count > 0;

            // Avoid overwriting timing if the pattern has no redlines
            TimingOverwriteMode timingOverwriteMode = patternTiming.Redlines.Count > 0
                ? TimingOverwriteMode
                : TimingOverwriteMode.OriginalTimingOnly;

            // Get the scale for custom scale x CS scale
            double csScale = Beatmap.GetHitObjectRadius(originalCircleSize) /
                             Beatmap.GetHitObjectRadius(patternCircleSize);
            double spatialScale = ScaleToNewCircleSize && !double.IsNaN(csScale) ? CustomScale * csScale : CustomScale;

            // Get a BPM multiplier to fix the tick rate
            // This multiplier is not meant to change SV so this is subtracted from the greenline SV later
            double bpmMultiplier = FixTickRate ? patternTickRate / originalTickRate : 1;

            // Dont give new combo to all hit objects which were actually new combo in the pattern,
            // because it leads to unexpected NC's at the start of patterns.

            // Collect Kiai toggles
            List<TimingPoint> kiaiToggles = new List<TimingPoint>();
            bool lastKiai = false;
            // If not including the kiai of the pattern, add the kiai of the original map.
            // This has to be done because this part of the original map might get deleted.
            foreach (TimingPoint tp in IncludeKiai ? patternTiming.TimingPoints : originalTiming.TimingPoints) {
                if (tp.Kiai != lastKiai || kiaiToggles.Count == 0) {
                    kiaiToggles.Add(tp.Copy());
                    lastKiai = tp.Kiai;
                }
            }

            // Collect SliderVelocity changes for mania/taiko
            List<TimingPoint> svChanges = new List<TimingPoint>();
            double lastSV = -100;
            // If not including the SV of the pattern, add the SV of the original map.
            // This has to be done because this part of the original map might get deleted.
            foreach (TimingPoint tp in includePatternSliderVelocity ? patternTiming.TimingPoints : originalTiming.TimingPoints) {
                if (tp.Uninherited) {
                    lastSV = -100;
                } else {
                    if (Math.Abs(tp.MpB - lastSV) > Precision.DOUBLE_EPSILON) {
                        svChanges.Add(tp.Copy());
                        lastSV = tp.MpB;
                    }
                }
            }

            // If not including the SV of the pattern, set the SV of sliders to that of the original beatmap,
            // so the pattern will take over the SV of the original beatmap.
            if (!includePatternSliderVelocity) {
                foreach (var ho in patternBeatmap.HitObjects.Where(ho => ho.IsSlider)) {
                    ho.SliderVelocity = originalTiming.GetSvAtTime(ho.Time);
                }
            }

            // Get the timeline before moving all objects so it has the correct hitsounds
            // Make sure that moving the objects in the pattern moves the timeline objects aswell
            // This method is NOT safe to use in beat time
            Timeline patternTimeline = patternBeatmap.GetTimeline();
            Timing transformOriginalTiming = originalTiming;
            Timing transformPatternTiming = patternTiming;
            if (scaleToNewTiming) {
                // Transform everything to beat time relative to pattern start time
                foreach (var ho in patternBeatmap.HitObjects) {
                    double oldEndTime = ho.GetEndTime(false);

                    ho.Time = patternTiming.GetBeatLength(patternStartTime, ho.Time);
                    ho.EndTime = patternTiming.GetBeatLength(patternStartTime, oldEndTime);

                    // The body hitsounds are not copies of timingpoints in patternTiming so they should be copied before changing offset
                    for (int i = 0; i < ho.BodyHitsounds.Count; i++) {
                        TimingPoint tp = ho.BodyHitsounds[i].Copy();
                        tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
                        ho.BodyHitsounds[i] = tp;
                    }
                }

                foreach (var tp in kiaiToggles.Concat(svChanges)) {
                    tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
                }

                // Transform the pattern redlines to beat time
                // This will not change the order of redlines (unless negative BPM exists)
                transformPatternTiming = patternTiming.Copy();
                foreach (var tp in transformPatternTiming.Redlines) {
                    tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
                }

                // Transform the original timingpoints to beat time
                // This will not change the order of timingpoints (unless negative BPM exists)
                transformOriginalTiming = originalTiming.Copy();
                foreach (var tp in transformOriginalTiming.TimingPoints) {
                    tp.Offset = originalTiming.GetBeatLength(patternStartTime, tp.Offset);
                }
            }

            // Fix SV for the new global SV
            var globalSvFactor =  transformOriginalTiming.SliderMultiplier / transformPatternTiming.SliderMultiplier;
            if (FixGlobalSv) {
                foreach (HitObject ho in patternBeatmap.HitObjects.Where(o => o.IsSlider)) {
                    ho.SliderVelocity *= globalSvFactor;
                }
                foreach (TimingPoint tp in svChanges) {
                    tp.MpB *= globalSvFactor;
                }
            }
            else {
                foreach (HitObject ho in patternBeatmap.HitObjects.Where(o => o.IsSlider)) {
                    ho.TemporalLength /= globalSvFactor;
                }
            }

            // Partition the pattern based on the timing in the pattern
            if (PatternOverwriteMode == PatternOverwriteMode.PartitionedOverwrite) {
                parts = PartitionBeatmap(patternBeatmap, scaleToNewTiming);
            } else {
                parts = new List<Part> {
                    new Part(patternBeatmap.HitObjects[0].Time, 
                        patternBeatmap.HitObjects[patternBeatmap.HitObjects.Count-1].Time, 
                        patternBeatmap.HitObjects)
                };
            }

            // Construct a new timing which is a mix of the beatmap and the pattern.
            // If scaleToNewTiming then use beat relative values to determine the duration of timing sections in the pattern.
            // scaleToNewTiming must scale all the partitions, timingpoints, hitobjects, and events (if applicable).
            Timing newTiming = new Timing(transformOriginalTiming.SliderMultiplier);

            var lastEndTime = double.NegativeInfinity;
            foreach (var part in parts) {
                var startTime = part.StartTime;
                var endTime = part.EndTime;  // Subtract one to omit BPM changes right on the end of the part.

                // Add the redlines in between patterns
                newTiming.AddRange(transformOriginalTiming.GetRedlinesInRange(lastEndTime, startTime, false));

                var startOriginalRedline = transformOriginalTiming.GetRedlineAtTime(startTime);

                // Minus 1 the offset so its possible to have a custom BPM redline right on the start time if you have 
                // the default BPM redline before it.
                var patternDefaultMpb = transformPatternTiming.GetMpBAtTime(startTime - 2 * Precision.DOUBLE_EPSILON);

                TimingPoint[] inPartRedlines;
                TimingPoint startPartRedline;
                switch (timingOverwriteMode) {
                    case TimingOverwriteMode.PatternTimingOnly:
                        // Subtract one from the end time to omit BPM changes right on the end of the part.
                        inPartRedlines = transformPatternTiming.GetRedlinesInRange(startTime,
                            Math.Max(startTime, endTime - 2 * Precision.DOUBLE_EPSILON)).ToArray();
                        startPartRedline = transformPatternTiming.GetRedlineAtTime(startTime);
                        break;
                    case TimingOverwriteMode.InPatternAbsoluteTiming:
                        var tempInPartRedlines = transformPatternTiming.GetRedlinesInRange(startTime, endTime - 2 * Precision.DOUBLE_EPSILON);

                        // Replace all parts in the pattern which have the default BPM to timing from the target beatmap.
                        inPartRedlines = tempInPartRedlines.Select(tp => {
                            if (Precision.AlmostEquals(tp.MpB, patternDefaultMpb)) {
                                var tp2 = transformOriginalTiming.GetRedlineAtTime(tp.Offset).Copy();
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
                        var tempInPartRedlines2 = transformPatternTiming.GetRedlinesInRange(startTime, endTime - 2 * Precision.DOUBLE_EPSILON);
                        var tempInOriginalRedlines = transformOriginalTiming.GetRedlinesInRange(startTime, endTime - 2 * Precision.DOUBLE_EPSILON);

                        // Replace all parts in the pattern which have the default BPM to timing from the target beatmap.
                        inPartRedlines = tempInPartRedlines2.Select(tp => {
                            var tp2 = tp.Copy();
                            tp2.MpB *= transformOriginalTiming.GetMpBAtTime(tp.Offset) / patternDefaultMpb;
                            return tp2;
                        }).Concat(tempInOriginalRedlines.Select(tp => {
                            var tp2 = tp.Copy();
                            tp2.MpB *= transformPatternTiming.GetMpBAtTime(tp.Offset) / patternDefaultMpb;
                            return tp2;
                        })).ToArray();

                        startPartRedline = transformPatternTiming.GetRedlineAtTime(startTime).Copy();
                        startPartRedline.MpB *= transformOriginalTiming.GetMpBAtTime(startTime) / patternDefaultMpb;
                        break;
                    default:  // Original timing only
                        // Subtract one from the end time to omit BPM changes right on the end of the part.
                        inPartRedlines = transformOriginalTiming.GetRedlinesInRange(startTime,
                            Math.Max(startTime, endTime - 2 * Precision.DOUBLE_EPSILON)).ToArray();
                        startPartRedline = transformOriginalTiming.GetRedlineAtTime(startTime);
                        break;
                }

                // Add the redlines for inside the part
                newTiming.AddRange(inPartRedlines);

                // If the pattern starts with different BPM than the map add an extra redline at the start of the pattern
                // to make sure it the pattern starts out at the right BPM as we only copy the timingpoints during the pattern itself
                // and the redline may be way before that.
                // This will probably only do something on the PatternTimingOnly mode as the other modes make sure
                // the BPM at the start of the pattern will be the same as the original beatmap anyways.
                if (Math.Abs(startPartRedline.MpB * bpmMultiplier - startOriginalRedline.MpB) > Precision.DOUBLE_EPSILON) {
                    // We dont have to add the redline again if its already during the pattern.
                    if (Math.Abs(startPartRedline.Offset - startTime) > Precision.DOUBLE_EPSILON) {
                        var copy = startPartRedline.Copy();
                        copy.Offset = startTime;
                        newTiming.Add(copy);
                    }
                }

                // Fix SV for the new BPM, so the SV effect of the new BPM is cancelled
                if (FixBpmSv) {
                    if (scaleToNewTiming) {
                        foreach (HitObject ho in patternBeatmap.HitObjects.Where(o => o.IsSlider)) {
                            var bpmSvFactor = SnapToNewTiming ? 
                                transformPatternTiming.GetMpBAtTime(ho.Time) /
                                newTiming.GetMpBAtTime(newTiming.ResnapBeatTime(ho.Time, BeatDivisors)) :
                                transformPatternTiming.GetMpBAtTime(ho.Time) / 
                                newTiming.GetMpBAtTime(ho.Time);
                            ho.SliderVelocity *= bpmSvFactor;
                        }
                    }
                    else {
                        foreach (HitObject ho in patternBeatmap.HitObjects.Where(o => o.IsSlider)) {
                            var bpmSvFactor = SnapToNewTiming ?
                                transformPatternTiming.GetMpBAtTime(ho.Time) / newTiming.GetMpBAtTime(newTiming.Resnap(ho.Time, BeatDivisors)) :
                                transformPatternTiming.GetMpBAtTime(ho.Time) / newTiming.GetMpBAtTime(ho.Time);
                            ho.SliderVelocity *= bpmSvFactor;
                        }
                    }
                }

                // Recalculate temporal length and re-assign redline for the sliderend resnapping later
                foreach (var ho in part.HitObjects) {
                    ho.UnInheritedTimingPoint = newTiming.GetRedlineAtTime(ho.Time);
                    if (ho.IsSlider) {
                        // If scaleToNewTiming then the end time is already at the correct beat time
                        // The SV has to be adjusted so the sliderend is really on the end time
                        if (scaleToNewTiming) {
                            var wantedMsDuration = (newTiming.GetMilliseconds(ho.GetEndTime(false), patternStartTime) -
                                                    newTiming.GetMilliseconds(ho.Time, patternStartTime)) / ho.Repeat;
                            var trueMsDuration = newTiming.CalculateSliderTemporalLength(SnapToNewTiming ? newTiming.ResnapBeatTime(ho.Time, BeatDivisors) : ho.Time, ho.PixelLength, ho.SliderVelocity);
                            ho.SliderVelocity /= trueMsDuration / wantedMsDuration;
                        }
                        else {
                            ho.TemporalLength = newTiming.CalculateSliderTemporalLength(SnapToNewTiming ? newTiming.Resnap(ho.Time, BeatDivisors) : ho.Time, ho.PixelLength, ho.SliderVelocity);
                        }
                    }
                }

                // Update the end time because the lengths of sliders changed
                endTime = part.HitObjects.Max(o => o.GetEndTime(!scaleToNewTiming));
                part.EndTime = endTime;

                // Add a redline at the end of the pattern to make sure the BPM goes back to normal after the pattern.
                var endOriginalRedline = transformOriginalTiming.GetRedlineAtTime(endTime);
                var endPartRedline = inPartRedlines.LastOrDefault() ?? startPartRedline;
                if (Math.Abs(endPartRedline.MpB * bpmMultiplier - endOriginalRedline.MpB) > Precision.DOUBLE_EPSILON) {
                    // We dont have to add the redline again if its already during the parts in between parts.
                    if (Math.Abs(endOriginalRedline.Offset - endTime) > Precision.DOUBLE_EPSILON) {
                        var copy = endOriginalRedline.Copy();
                        copy.Offset = endTime;
                        newTiming.Add(copy);
                    }
                }

                lastEndTime = endTime;
            }

            // Transform the beat time back to millisecond time
            Timing transformNewTiming = newTiming;
            if (scaleToNewTiming) {
                // Transform back the timing
                transformNewTiming = newTiming.Copy();
                foreach (var tp in transformNewTiming.TimingPoints) {
                    tp.Offset = Math.Floor(newTiming.GetMilliseconds(tp.Offset, patternStartTime) + Precision.DOUBLE_EPSILON);
                }

                // Transform back the parts
                foreach (Part part in parts) {
                    part.StartTime = Math.Floor(newTiming.GetMilliseconds(part.StartTime, patternStartTime));
                    part.EndTime = Math.Floor(newTiming.GetMilliseconds(part.EndTime, patternStartTime));
                }

                // Transform everything to millisecond time relative to pattern start time
                foreach (var ho in patternBeatmap.HitObjects) {
                    // Calculate the millisecond end time before changing the start time because the end time getter uses the beat time start time
                    var msEndTime = newTiming.GetMilliseconds(ho.GetEndTime(false), patternStartTime);

                    ho.Time = newTiming.GetMilliseconds(ho.Time, patternStartTime);

                    // End time has to be set after the time because the end time setter uses the millisecond start time
                    ho.EndTime = msEndTime;

                    foreach (var tp in ho.BodyHitsounds) {
                        tp.Offset = newTiming.GetMilliseconds(tp.Offset, patternStartTime);
                    }

                    // It is necessary to resnap early so it can recalculate the duration using the right offset
                    if (SnapToNewTiming)
                        ho.ResnapSelf(transformNewTiming, BeatDivisors);

                    if (ho.IsSlider)
                        ho.CalculateSliderTemporalLength(transformNewTiming, true);

                    ho.UnInheritedTimingPoint = transformNewTiming.GetRedlineAtTime(ho.Time);
                    ho.UpdateTimelineObjectTimes();
                }

                foreach (var tp in kiaiToggles.Concat(svChanges)) {
                    tp.Offset = Math.Floor(newTiming.GetMilliseconds(tp.Offset, patternStartTime));
                }
            }

            // Apply custom scale and rotate
            if (Math.Abs(spatialScale - 1) > Precision.DOUBLE_EPSILON ||
                Math.Abs(CustomRotate) > Precision.DOUBLE_EPSILON) {
                // Create a transformation matrix for the custom scale and rotate
                // The rotation is inverted because the default osu! rotation goes clockwise
                Matrix2 transform = Matrix2.Mult(Matrix2.CreateScale(spatialScale), Matrix2.CreateRotation(-CustomRotate));
                Vector2 centre = new Vector2(256, 192);
                foreach (var ho in patternBeatmap.HitObjects) {
                    ho.Move(-centre);
                    ho.Transform(transform);
                    ho.Move(centre);

                    // Scale pixel length and SV for sliders aswell
                    if (ho.IsSlider) {
                        ho.PixelLength *= spatialScale;
                        ho.SliderVelocity /= spatialScale;
                    }
                }
                
                // osu! clips coordinates to the bounds (0,512), so there is some space downwards to still place the pattern
                // Calculate the new bounds of the pattern and try to place it in the playfield
                var minX = patternBeatmap.HitObjects.Min(o => o.Pos.X);
                var minY = patternBeatmap.HitObjects.Min(o => o.Pos.Y);
                Vector2 offset = new Vector2(Math.Max(-minX, 0), Math.Max(-minY, 0));
                if (offset.LengthSquared > 0) {
                    foreach (var ho in patternBeatmap.HitObjects) {
                        ho.Move(offset);
                    }
                }
            }

            // Manualify stacks
            if (FixStackLeniency) {
                // If scale to new timing was used update the circle size of the pattern,
                // so it calculates stacks at the new size of the pattern.
                if (ScaleToNewCircleSize) {
                    patternBeatmap.Difficulty["CircleSize"].DoubleValue = originalCircleSize;
                }

                patternBeatmap.CalculateEndPositions();
                patternBeatmap.UpdateStacking(rounded: true);

                // Manualify by setting the base position to the stacked position
                foreach (var ho in patternBeatmap.HitObjects) {
                    var offset = ho.StackedPos - ho.Pos;
                    ho.Move(offset);
                }
            }

            // Resnap everything to the new timing.
            if (SnapToNewTiming) {
                // Resnap all objects
                foreach (HitObject ho in patternBeatmap.HitObjects) {
                    ho.ResnapSelf(transformNewTiming, BeatDivisors);
                    ho.ResnapEnd(transformNewTiming, BeatDivisors);
                    ho.ResnapPosition(targetMode, patternCircleSize);  // Resnap to column X positions for mania only
                }
                // Resnap Kiai toggles
                foreach (TimingPoint tp in kiaiToggles) {
                    tp.ResnapSelf(transformNewTiming, BeatDivisors);
                }

                // Resnap SliderVelocity changes
                foreach (TimingPoint tp in svChanges) {
                    tp.ResnapSelf(transformNewTiming, BeatDivisors);
                }
            }

            // Multiply BPM and divide SV
            foreach (var part in parts) {
                foreach (var tp in transformNewTiming.GetRedlinesInRange(part.StartTime - 2 * Precision.DOUBLE_EPSILON, part.EndTime, false)) {
                    tp.MpB /= bpmMultiplier;  // MpB is the inverse of the BPM
                }

                foreach (var ho in part.HitObjects) {
                    ho.SliderVelocity *= bpmMultiplier;  // SliderVelocity is the inverse of the multiplier
                }
            }

            // Make new timingpoints changes for the hitsounds and other stuff

            // Add redlines
            timingPointsChanges = transformNewTiming.Redlines.Select(tp => 
                new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true, fuzzyness:Precision.DOUBLE_EPSILON)).ToList();

            // Add SliderVelocity changes for taiko and mania
            if (includePatternSliderVelocity && (targetMode == GameMode.Taiko || targetMode == GameMode.Mania)) {
                timingPointsChanges.AddRange(svChanges.Select(tp => new TimingPointsChange(tp, mpb: true)));
            }

            // Add Kiai toggles
            timingPointsChanges.AddRange(kiaiToggles.Select(tp => new TimingPointsChange(tp, kiai: true)));

            // Add Hitobject stuff
            foreach (HitObject ho in patternBeatmap.HitObjects) {
                if (ho.IsSlider) // SliderVelocity changes
                {
                    TimingPoint tp = ho.TimingPoint.Copy();
                    tp.Offset = ho.Time;
                    tp.MpB = ho.SliderVelocity;
                    timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true));
                }

                if (!IncludeHitsounds) {
                    // Remove hitsounds and skip adding body hitsounds
                    ho.ResetHitsounds();
                    continue;
                }

                if (includeTimingPointHitsounds) {
                    // Body hitsounds
                    bool vol = ho.IsSlider || ho.IsSpinner;
                    bool sam = ho.IsSlider && ho.SampleSet == 0;
                    bool ind = ho.IsSlider;
                    timingPointsChanges.AddRange(ho.BodyHitsounds.Select(tp =>
                        new TimingPointsChange(tp, volume: vol, index: ind, sampleset: sam)));
                }
            }

            // Add timeline hitsounds
            if (includeTimingPointHitsounds) {
                foreach (TimelineObject tlo in patternTimeline.TimelineObjects) {
                    if (tlo.HasHitsound) {
                        // Add greenlines for hitsounds
                        TimingPoint tp = tlo.HitsoundTimingPoint.Copy();
                        tp.Offset = tlo.Time;
                        timingPointsChanges.Add(new TimingPointsChange(tp, sampleset: true, volume: true, index: true));
                    }
                }
            }
            
            // Replace the old timingpoints
            patternTiming.Clear();
            TimingPointsChange.ApplyChanges(patternTiming, timingPointsChanges);

            patternBeatmap.GiveObjectsGreenlines();
            patternBeatmap.CalculateSliderEndTimes();
        }
    }
}