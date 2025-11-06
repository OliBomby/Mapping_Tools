using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Contexts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.BeatmapHelper.Sections;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.BeatmapHelper.Types;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Class for placing a <see cref="OsuPattern"/> into a <see cref="Beatmap"/>.
/// </summary>
public class OsuPatternPlacer {
    /// <summary>
    /// Extra time in milliseconds around patterns for removing a wider range of objects in the destination beatmap.
    /// </summary>
    public double Padding = 5;

    /// <summary>
    /// Minimum number of beats in between partitions of a pattern.
    /// </summary>
    public double PartingDistance = 4;

    /// <summary>
    /// Determines how to remove the objects in the destination beatmap which overlap with the pattern.
    /// </summary>
    public PatternOverwriteMode PatternOverwriteMode = PatternOverwriteMode.PartitionedOverwrite;

    /// <summary>
    /// Determines which timing stuff to keep from the pattern.
    /// </summary>
    public TimingOverwriteMode TimingOverwriteMode = TimingOverwriteMode.DestinationTimingOnly;

    /// <summary>
    /// Whether to copy over the hitsounds of the pattern.
    /// </summary>
    public bool IncludeHitsounds = false;

    /// <summary>
    /// Whether to coper over the kiais of the pattern.
    /// </summary>
    public bool IncludeKiai = false;

    /// <summary>
    /// Whether to scale the pattern to the circle size of the beatmap so the relative spacing stays the same.
    /// </summary>
    public bool ScaleToNewCircleSize = false;

    /// <summary>
    /// Whether to scale the offsets of hit objects in the pattern such that the relative number of beats between hit objects stays the same.
    /// </summary>
    public bool ScaleToNewTiming = true;

    /// <summary>
    /// Whether to resnap hit objects to the new timing.
    /// </summary>
    public bool SnapToNewTiming = true;

    /// <summary>
    /// The beat divisors to use when resnapping hit objects or scaling hit object timing.
    /// </summary>
    public IBeatDivisor[] BeatDivisors = RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>
    /// Whether to scale SV of the pattern such that the difference in global SV gets nullified.
    /// </summary>
    public bool FixGlobalSv = true;

    /// <summary>
    /// Whether to scale SV of the pattern such that the difference in BPM has no effect on the SV.
    /// Don't use this if you intent to scale the pattern to the new timing.
    /// </summary>
    public bool FixBpmSv = false;

    /// <summary>
    /// Whether to adjust combo skip such that the combo colours in the destination beatmap stay the same.
    /// </summary>
    public bool FixColourHax = true;

    /// <summary>
    /// Whether to manualify stacks in the pattern such that objects that are stacked in the pattern stay stacked in the destination beatmap.
    /// </summary>
    public bool FixStackLeniency = false;

    /// <summary>
    /// Whether to adjust BPM such that the tick rate of the pattern stays the same in the destination beatmap.
    /// </summary>
    public bool FixTickRate = false;

    /// <summary>
    /// Scaling factor for changing the size of the pattern before placing it into the destination beatmap.
    /// </summary>
    public double CustomScale = 1;

    /// <summary>
    /// Rotation in radians for rotating the pattern before placing it into the destination beatmap.
    /// </summary>
    public double CustomRotate = 0;

    /// <summary>
    /// Places the pattern beatmap into the destination beatmap at the specified time and applies all the configured move refactoring.
    /// The given pattern beatmap could be modified by this method if protectPatternBeatmap is false.
    /// </summary>
    /// <param name="patternBeatmap">The pattern beatmap to be placed into the beatmap.</param>
    /// <param name="targetBeatmap">The beatmap to place the pattern in.</param>
    /// <param name="time">The time at which to place the first hit object of the pattern beatmap.</param>
    /// <param name="protectPatternBeatmap">If true, copies the pattern beatmap to prevent the pattern beatmap from being modified by this method.</param>
    /// <param name="overwriteStartTime">Set the start time of the overwrite window in the pattern beatmap. Will ignore partitioned overwrite mode.</param>
    /// <param name="overwriteEndTime">Set the end time of the overwrite window in the pattern beatmap. Will ignore partitioned overwrite mode.</param>
    /// <exception cref="ArgumentException">If pattern beatmap contains no hit objects.</exception>
    public void PlaceOsuPatternAtTime(
        IBeatmap patternBeatmap,
        IBeatmap targetBeatmap,
        double time,
        bool protectPatternBeatmap = true,
        double? overwriteStartTime = null,
        double? overwriteEndTime = null
    ) {
        if (patternBeatmap.HitObjects.Count == 0) {
            throw new ArgumentException("Pattern Beatmap should contain at least one hit object.", nameof(patternBeatmap));
        }

        double offset = double.IsNaN(time) ? 0 : time - patternBeatmap.GetHitObjectStartTime();
        PlaceOsuPattern(patternBeatmap, targetBeatmap, offset, protectPatternBeatmap, overwriteStartTime, overwriteEndTime);
    }

    /// <summary>
    /// Places the pattern beatmap into the destination beatmap and applies all the configured move refactoring.
    /// The given pattern beatmap could be modified by this method if protectPatternBeatmap is false.
    /// </summary>
    /// <param name="patternBeatmap">The pattern beatmap to be placed into the beatmap.</param>
    /// <param name="targetBeatmap">The beatmap to place the pattern in.</param>
    /// <param name="offset">An offset in milliseconds to move the pattern beatmap in time.</param>
    /// <param name="protectPatternBeatmap">If true, copies the pattern beatmap to prevent the pattern beatmap from being modified by this method.</param>
    /// <param name="overwriteStartTime">Set the start time of the overwrite window in the pattern beatmap. Will ignore partitioned overwrite mode.</param>
    /// <param name="overwriteEndTime">Set the end time of the overwrite window in the pattern beatmap. Will ignore partitioned overwrite mode.</param>
    /// <exception cref="ArgumentException">If pattern beatmap contains no hit objects.</exception>
    public void PlaceOsuPattern(
        IBeatmap patternBeatmap,
        IBeatmap targetBeatmap,
        double offset = 0,
        bool protectPatternBeatmap = true,
        double? overwriteStartTime = null,
        double? overwriteEndTime = null
    ) {
        if (patternBeatmap.HitObjects.Count == 0) {
            throw new ArgumentException("Pattern Beatmap should contain at least one hit object.", nameof(patternBeatmap));
        }

        if (protectPatternBeatmap) {
            // Copy so the original pattern doesnt get changed
            patternBeatmap = patternBeatmap.DeepClone();
        }

        // Make sure the beatmaps have timing context.
        targetBeatmap.GiveObjectsTimingContext();
        patternBeatmap.GiveObjectsTimingContext();

        // Do the offset
        if (Math.Abs(offset) > Precision.DOUBLE_EPSILON) {
            patternBeatmap.OffsetTime(offset);
            overwriteStartTime += offset;
            overwriteEndTime += offset;
        }

        // We adjust the pattern first so it alligns with the beatmap.
        // The right timing is applied and optional pre-processing is applied.
        // Sliderends and object timingpoints get recalculated.
        PreparePattern(
            patternBeatmap,
            targetBeatmap,
            out var parts,
            out var controlChanges,
            overwriteStartTime,
            overwriteEndTime
        );

        // Keep just the timing point changes which are inside the parts.
        // These timing point changes have everything that is necessary for inside the parts of the pattern. (even timing)
        controlChanges = controlChanges.Where(tpc => parts.Any(part =>
            part.StartTime <= tpc.MyTP.Offset && part.EndTime >= tpc.MyTP.Offset)).ToList();

        // Remove hit objects and timing points in the destination beatmap that overlap with the pattern beatmap.
        if (PatternOverwriteMode != PatternOverwriteMode.NoOverwrite) {
            foreach (var part in parts) {
                RemovePartOfBeatmap(targetBeatmap, part.StartTime - Padding, part.EndTime + Padding);
            }
        }

        // Add timingpoint changes for each hitobject to make sure they still have the wanted SV and hitsounds (especially near the edges of parts)
        // It is possible for the timingpoint of a hitobject at the start of a part to be outside of the part, so this fixes issues related to that
        controlChanges.AddRange(targetBeatmap.HitObjects.Select(GetSvChange));

        // Also make sure to preserve the hitsounds of the destination beatmap if the pattern beatmap changes hitsounds in its parts.
        if (IncludeHitsounds) {
            controlChanges.AddRange(targetBeatmap.HitObjects.Select(GetHitsoundChange));
        }

        // Apply the control changes
        ControlChange.ApplyChanges(targetBeatmap.BeatmapTiming, controlChanges);

        // Add the hitobjects of the pattern
        targetBeatmap.HitObjects.AddRange(patternBeatmap.HitObjects);

        // Sort hitobjects
        targetBeatmap.SortHitObjects();

        // Fix the combo skip for combo colours.
        if (FixColourHax) {
            targetBeatmap.FixComboSkip();
        }

        targetBeatmap.GiveObjectsTimingContext();
    }

    /// <summary>
    /// Creates parts that have at least PartingDistance number of beats of a gap between the parts.
    /// </summary>
    /// <param name="beatmap">The beatmap to partition.</param>
    /// <param name="beatMode">Whether the beatmap uses beat time.</param>
    /// <returns>List of parts with start time, end time, and a list of hit objects in the part.</returns>
    private List<Part> PartitionBeatmap(IBeatmap beatmap, bool beatMode) {
        var parts = new List<Part>();

        int startIndex = 0;
        for (int i = 1; i < beatmap.HitObjects.Count; i++) {
            var timeGap = beatmap.HitObjects[i].GetContext<TransformTimeContext>().StartTime - beatmap.HitObjects[i - 1].GetContext<TransformTimeContext>().EndTime;
            var beatGap = beatmap.BeatmapTiming.GetBeatLength(beatmap.HitObjects[i - 1].GetContext<TransformTimeContext>().EndTime, beatmap.HitObjects[i].GetContext<TransformTimeContext>().StartTime);
            var gap = beatMode ?
                timeGap :
                Math.Min(beatGap, timeGap);

            if (Precision.AlmostBigger(gap, PartingDistance)) {
                parts.Add(new Part(beatmap.HitObjects[startIndex].GetContext<TransformTimeContext>().StartTime,
                    GetEndTimeNoZeroLength(beatmap.HitObjects[i-1], beatMode),
                    beatmap.HitObjects.GetRange(startIndex, i - startIndex)));

                startIndex = i;
            }
        }
        parts.Add(new Part(beatmap.HitObjects[startIndex].GetContext<TransformTimeContext>().StartTime,
            GetEndTimeNoZeroLength(beatmap.HitObjects[^1], beatMode),
            beatmap.HitObjects.GetRange(startIndex, beatmap.HitObjects.Count - startIndex)));

        return parts;
    }

    private static double GetEndTimeNoZeroLength(HitObject ho, bool beatMode) {
        var endTime = ho.GetContext<TransformTimeContext>().EndTime;
        // We want sliders to have at least 1 ms of length so the start time can always get a redline and have the part end after the slider start
        return Precision.AlmostEquals(endTime, ho.StartTime, 1) && ho is Slider && !beatMode ? endTime + 1 : endTime;
    }

    private class Part {
        internal double StartTime;
        internal double EndTime;
        internal readonly List<HitObject> HitObjects;

        public Part(double startTime, double endTime, List<HitObject> hitObjects) {
            StartTime = startTime;
            EndTime = endTime;
            HitObjects = hitObjects;
        }
    }

    /// <summary>
    /// Removes hitobjects and timingpoints in the beatmap between the start and the end time.
    /// </summary>
    private static void RemovePartOfBeatmap(IBeatmap beatmap, double startTime, double endTime) {
        beatmap.HitObjects.RemoveAll(h => h.StartTime >= startTime && h.StartTime <= endTime);
        beatmap.BeatmapTiming.RemoveAll(tp => tp.Offset >= startTime && tp.Offset <= endTime);
    }

    private static ControlChange GetSvChange(HitObject ho) {
        var tc = ho.GetContext<TimingContext>();
        var tp = tc.TimingPoint.Copy();
        tp.Offset = ho.StartTime;
        tp.Uninherited = false;
        tp.SetSliderVelocity(tc.SliderVelocity);
        return new ControlChange(tp, true);
    }

    private static ControlChange GetHitsoundChange(HitObject ho) {
        var tp = ho.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
        tp.Offset = ho.StartTime;
        tp.Uninherited = false;
        return new ControlChange(tp, sampleset: true, index: true, volume: true);
    }

    /// <summary>
    /// Applies move refactoring to the pattern beatmap.
    /// It does so according to the options selected in this.
    /// </summary>
    private void PreparePattern(
        IBeatmap patternBeatmap,
        IBeatmap targetBeatmap,
        out List<Part> parts,
        out List<ControlChange> controlChanges,
        double? overwriteStartTime = null,
        double? overwriteEndTime = null
    ) {
        double patternStartTime = overwriteStartTime ?? patternBeatmap.GetHitObjectStartTime();

        Timing originalTiming = targetBeatmap.BeatmapTiming;
        Timing patternTiming = patternBeatmap.BeatmapTiming;

        GameMode targetMode = targetBeatmap.General.Mode;

        float originalCircleSize = targetBeatmap.Difficulty.CircleSize;
        float patternCircleSize = patternBeatmap.Difficulty.CircleSize;

        double originalTickRate = targetBeatmap.Difficulty.SliderTickRate;
        double patternTickRate = patternBeatmap.Difficulty.SliderTickRate;

        // Don't include SV changes if it is based on nothing
        bool includePatternSliderVelocity = patternTiming.Count > 0;

        // Avoid including hitsounds if there are no timingpoints to get hitsounds from
        bool includeTimingPointHitsounds = IncludeHitsounds && patternTiming.Count > 0;

        // Don't scale to new timing if the pattern has no timing to speak of
        bool scaleToNewTiming = ScaleToNewTiming && patternTiming.Redlines.Count > 0;

        // Avoid overwriting timing if the pattern has no redlines
        TimingOverwriteMode timingOverwriteMode = patternTiming.Redlines.Count > 0
            ? TimingOverwriteMode
            : TimingOverwriteMode.DestinationTimingOnly;

        // Get the scale for custom scale x CS scale
        double csScale = SectionDifficulty.GetHitObjectRadius(originalCircleSize) /
                         SectionDifficulty.GetHitObjectRadius(patternCircleSize);
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
        double lastSv = 1;
        // If not including the SV of the pattern, add the SV of the original map.
        // This has to be done because this part of the original map might get deleted.
        foreach (TimingPoint tp in includePatternSliderVelocity ? patternTiming.TimingPoints : originalTiming.TimingPoints) {
            if (tp.Uninherited) {
                lastSv = 1;
            } else {
                var sv = tp.GetSliderVelocity();
                if (Math.Abs(sv - lastSv) > Precision.DOUBLE_EPSILON) {
                    svChanges.Add(tp.Copy());
                    lastSv = sv;
                }
            }
        }

        // Fix SV for the new global SV
        var globalSvFactor = originalTiming.GlobalSliderMultiplier / patternTiming.GlobalSliderMultiplier;
        foreach (var tc in patternBeatmap.HitObjects.Select(ho => ho.GetContext<TimingContext>())) {
            tc.GlobalSliderVelocity = originalTiming.GlobalSliderMultiplier;

            if (FixGlobalSv) {
                tc.SliderVelocity /= globalSvFactor;
            }
        }

        // If not including the SV of the pattern, set the SV of sliders to that of the original beatmap,
        // so the pattern will take over the SV of the original beatmap.
        if (!includePatternSliderVelocity) {
            foreach (var ho in patternBeatmap.HitObjects.Where(ho => ho is Slider)) {
                ho.GetContext<TimingContext>().SliderVelocity = originalTiming.GetSvAtTime(ho.StartTime);
            }
        }

        // Get the timeline before moving all objects so it has the correct hitsounds
        // Make sure that moving the objects in the pattern moves the timeline objects aswell
        // This method is NOT safe to use in beat time
        Timeline patternTimeline = patternBeatmap.GetTimeline();
        Timing transformOriginalTiming = originalTiming.Copy();
        Timing transformPatternTiming = patternTiming.Copy();
        if (scaleToNewTiming) {
            // Transform everything to beat time relative to pattern start time
            foreach (var ho in patternBeatmap.HitObjects) {
                var tc = ho.GetContext<TimingContext>();

                ho.SetContext(new TransformTimeContext(
                    patternTiming.GetBeatLength(patternStartTime, ho.StartTime),
                    patternTiming.GetBeatLength(patternStartTime, ho.EndTime)));

                // Convert all the timing context timing points to beat time
                foreach (var tp in tc.BodyHitsounds) {
                    tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
                }

                tc.UninheritedTimingPoint.Offset = patternTiming.GetBeatLength(patternStartTime, tc.UninheritedTimingPoint.Offset);
                tc.TimingPoint.Offset = patternTiming.GetBeatLength(patternStartTime, tc.TimingPoint.Offset);
                tc.HitsoundTimingPoint.Offset = patternTiming.GetBeatLength(patternStartTime, tc.HitsoundTimingPoint.Offset);
            }

            // Transform kiai toggles and SV changes
            foreach (var tp in kiaiToggles.Concat(svChanges)) {
                tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
            }

            // Transform the override pattern start and end time to beat time
            if (overwriteStartTime.HasValue)
                overwriteStartTime = patternTiming.GetBeatLength(patternStartTime, overwriteStartTime.Value);
            if (overwriteEndTime.HasValue)
                overwriteEndTime = patternTiming.GetBeatLength(patternStartTime, overwriteEndTime.Value);

            // Transform the pattern redlines to beat time
            // This will not change the order of redlines (unless negative BPM exists)
            foreach (var tp in transformPatternTiming.Redlines) {
                tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
            }

            // Transform the original timingpoints to beat time
            // This will not change the order of timingpoints (unless negative BPM exists)
            foreach (var tp in transformOriginalTiming.TimingPoints) {
                tp.Offset = patternTiming.GetBeatLength(patternStartTime, tp.Offset);
            }
        } else {
            // Make sure every hit object has a transform time context
            foreach (var ho in patternBeatmap.HitObjects) {
                ho.SetContext(new TransformTimeContext(ho.StartTime, ho.EndTime));
            }
        }

        // Partition the pattern based on the timing in the pattern
        if (PatternOverwriteMode == PatternOverwriteMode.PartitionedOverwrite && !overwriteStartTime.HasValue && !overwriteEndTime.HasValue) {
            parts = PartitionBeatmap(patternBeatmap, scaleToNewTiming);
        } else {
            double partStartTime = overwriteStartTime ?? patternBeatmap.HitObjects.Min(o => o.GetContext<TransformTimeContext>().StartTime);
            double partEndTime = overwriteEndTime ?? patternBeatmap.HitObjects.Max(o => o.GetContext<TransformTimeContext>().EndTime);
            parts = [
                new Part(partStartTime, partEndTime,
                    patternBeatmap.HitObjects.FindAll(o => o.GetContext<TransformTimeContext>().StartTime >= partStartTime && o.GetContext<TransformTimeContext>().EndTime <= partEndTime)),
            ];
        }

        // Construct a new timing which is a mix of the beatmap and the pattern.
        // If scaleToNewTiming then use beat relative values to determine the duration of timing sections in the pattern.
        // scaleToNewTiming must scale all the partitions, timingpoints, hitobjects, and events (if applicable).
        Timing newTiming = new Timing(transformOriginalTiming.GlobalSliderMultiplier);

        var lastEndTime = double.NegativeInfinity;
        foreach (var part in parts) {
            var startTime = part.StartTime;
            var endTime = part.EndTime;

            // Add the redlines in between patterns
            newTiming.AddRange(transformOriginalTiming.GetRedlinesInRange(lastEndTime, startTime - 2 * Precision.DOUBLE_EPSILON));

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
                            tp2.Offset = tp.Offset;
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
                case TimingOverwriteMode.DestinationTimingOnly:
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
            foreach (HitObject ho in patternBeatmap.HitObjects.Where(o => o is Slider)) {
                var ttc = ho.GetContext<TransformTimeContext>();
                var transformStartTime = ttc.StartTime;

                var newStartTime = SnapToNewTiming
                    ? newTiming.ResnapBeatTime(transformStartTime, BeatDivisors)
                    : transformStartTime;
                var bpmFactor = newTiming.GetBpmAtTime(newStartTime) / transformPatternTiming.GetBpmAtTime(transformStartTime);

                if (FixBpmSv) {
                    ho.GetContext<TimingContext>().SliderVelocity /= bpmFactor;
                    if (scaleToNewTiming) {
                        ttc.Duration *= bpmFactor;
                    }
                } else if (!scaleToNewTiming) {
                    ttc.Duration /= bpmFactor;
                }
            }


            // Recalculate temporal length and re-assign redline for the sliderend resnapping later
            foreach (var ho in part.HitObjects) {
                var tc = ho.GetContext<TimingContext>();
                var transformStartTime = ho.GetContext<TransformTimeContext>().StartTime;
                var transformEndTime = ho.GetContext<TransformTimeContext>().EndTime;

                tc.UninheritedTimingPoint = newTiming.GetRedlineAtTime(transformStartTime).Copy();
                if (ho is Slider slider) {
                    // If scaleToNewTiming then the end time is already at the correct beat time
                    // The SV has to be adjusted so the sliderend is really on the end time
                    if (scaleToNewTiming) {
                        var wantedMsDuration = (newTiming.GetMilliseconds(transformEndTime, patternStartTime) -
                                                newTiming.GetMilliseconds(transformStartTime, patternStartTime)) / slider.SpanCount;
                        var trueMsDuration = newTiming.CalculateSliderDuration(SnapToNewTiming ? newTiming.ResnapBeatTime(transformStartTime, BeatDivisors) : transformStartTime, slider.PixelLength, tc.SliderVelocity);
                        ho.GetContext<TimingContext>().SliderVelocity *= trueMsDuration / wantedMsDuration;
                    }
                }
            }

            // Update the end time because the lengths of sliders changed
            // Except if the end time is overwritten, because then the end time is not calculated from the hit objects.
            if (!overwriteEndTime.HasValue) {
                endTime = part.HitObjects.Max(o => o.GetContext<TransformTimeContext>().EndTime);
                part.EndTime = endTime;
            }

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

        // Add the redlines after all the parts
        newTiming.AddRange(transformOriginalTiming.GetRedlinesInRange(lastEndTime, double.PositiveInfinity));

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
                var tcc = ho.GetContext<TransformTimeContext>();
                var tc = ho.GetContext<TimingContext>();

                ho.StartTime = newTiming.GetMilliseconds(tcc.StartTime, patternStartTime);

                // End time has to be set after the time because the end time setter uses the millisecond start time
                if (ho is Slider) {
                    Debug.Assert(Precision.AlmostEquals(ho.EndTime,
                        newTiming.GetMilliseconds(tcc.EndTime, patternStartTime)));
                }
                if (ho is IDuration durationHo) {
                    durationHo.SetEndTime(newTiming.GetMilliseconds(tcc.EndTime, patternStartTime));
                }

                foreach (var tp in tc.BodyHitsounds) {
                    tp.Offset = newTiming.GetMilliseconds(tp.Offset, patternStartTime);
                }

                tc.UninheritedTimingPoint.Offset = newTiming.GetMilliseconds(tc.UninheritedTimingPoint.Offset, patternStartTime);
                tc.TimingPoint.Offset = newTiming.GetMilliseconds(tc.TimingPoint.Offset, patternStartTime);
                tc.HitsoundTimingPoint.Offset = newTiming.GetMilliseconds(tc.HitsoundTimingPoint.Offset, patternStartTime);

                ho.GetContext<TimelineContext>().UpdateTimelineObjectTimes(ho);
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
                if (ho is Slider slider) {
                    slider.PixelLength *= spatialScale;
                    slider.GetContext<TimingContext>().SliderVelocity *= spatialScale;
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
                patternBeatmap.Difficulty.CircleSize = originalCircleSize;
            }

            patternBeatmap.CalculateEndPositions();
            patternBeatmap.UpdateStacking(rounded: true);

            // Manualify by setting the base position to the stacked position
            foreach (var ho in patternBeatmap.HitObjects) {
                var offset = ho.GetContext<StackingContext>().StackOffset();
                ho.Move(offset);
            }
        }

        // Resnap everything to the new timing.
        if (SnapToNewTiming) {
            // Resnap all objects
            foreach (HitObject ho in patternBeatmap.HitObjects) {
                ho.ResnapSelf(transformNewTiming, BeatDivisors);

                switch (ho) {
                    case Slider sliderHo:
                        sliderHo.ResnapEndTimeSmart(transformNewTiming, BeatDivisors);
                        break;
                    case IDuration durationHo:
                        durationHo.ResnapEndTime(transformNewTiming, BeatDivisors);
                        break;
                }

                ho.ResnapPosition(targetMode, patternCircleSize);  // Resnap to column X positions for mania only

                ho.GetContext<TimelineContext>().UpdateTimelineObjectTimes(ho);
            }

            // Resnap Kiai toggles
            foreach (TimingPoint tp in kiaiToggles) {
                tp.ResnapSelf(transformNewTiming, BeatDivisors);
            }

            // Resnap Slider Velocity changes
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
                ho.GetContext<TimingContext>().SliderVelocity /= bpmMultiplier;
            }
        }

        // Make new timingpoints changes for the hitsounds and other stuff

        // Add redlines
        controlChanges = transformNewTiming.Redlines.Select(tp =>
            new ControlChange(tp, mpb: true, meter: true, uninherited: true, omitFirstBarLine: true, fuzzyness:Precision.DOUBLE_EPSILON)).ToList();

        // Add SliderVelocity changes for taiko and mania
        if (includePatternSliderVelocity && (targetMode == GameMode.Taiko || targetMode == GameMode.Mania)) {
            controlChanges.AddRange(svChanges.Select(tp => new ControlChange(tp, mpb: true)));
        }

        // Add Kiai toggles
        controlChanges.AddRange(kiaiToggles.Select(tp => new ControlChange(tp, kiai: true)));

        // Add Hitobject stuff
        foreach (HitObject ho in patternBeatmap.HitObjects) {
            var tc = ho.GetContext<TimingContext>();

            // Slider Velocity changes
            if (ho is Slider) {
                TimingPoint tp = tc.TimingPoint.Copy();
                tp.Offset = ho.StartTime;
                tp.Uninherited = false;
                tp.SetSliderVelocity(tc.SliderVelocity);
                controlChanges.Add(new ControlChange(tp, mpb: true));
            }

            if (!IncludeHitsounds) {
                // Remove hitsounds and skip adding body hitsounds
                ho.ResetHitsounds();
                continue;
            }

            if (includeTimingPointHitsounds) {
                // Body hitsounds
                bool vol = ho is Slider || ho is Spinner;
                bool sam = ho is Slider && ho.Hitsounds.SampleSet == SampleSet.None;
                bool ind = ho is Slider;
                controlChanges.AddRange(tc.BodyHitsounds.Select(tp =>
                    new ControlChange(tp, volume: vol, index: ind, sampleset: sam)));
            }
        }

        // Add timeline hitsounds
        if (includeTimingPointHitsounds) {
            foreach (TimelineObject tlo in patternTimeline.TimelineObjects) {
                if (tlo.HasHitsound) {
                    // Add greenlines for hitsounds
                    TimingPoint tp = tlo.GetContext<TimingContext>().HitsoundTimingPoint.Copy();
                    tp.Offset = tlo.Time;
                    controlChanges.Add(new ControlChange(tp, sampleset: true, volume: true, index: true));
                }
            }
        }

        // Replace the old timingpoints
        patternTiming.Clear();
        ControlChange.ApplyChanges(patternTiming, controlChanges);

        patternBeatmap.GiveObjectsTimingContext();
    }

    private class TransformTimeContext : IContext {
        internal double StartTime;
        internal double EndTime;

        internal double Duration {
            get => EndTime - StartTime;
            set => EndTime = StartTime + value;
        }

        public TransformTimeContext(double startTime, double endTime) {
            StartTime = startTime;
            EndTime = endTime;
        }

        public IContext Copy() {
            return new TransformTimeContext(StartTime, EndTime);
        }
    }
}