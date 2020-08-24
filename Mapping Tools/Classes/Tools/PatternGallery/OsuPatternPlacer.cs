using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    /// <summary>
    /// Helper class for placing a <see cref="OsuPattern"/> into a <see cref="Beatmap"/>.
    /// </summary>
    public class OsuPatternPlacer : BindableBase {
        public double Padding = 5;
        public double PartingDistance = 4;
        public PatternOverwriteMode PatternOverwriteMode = PatternOverwriteMode.PartitionedOverwrite;
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
        public double CustomScale = 1;
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

            if (offset != 0) {
                patternBeatmap.OffsetTime(offset);
            }

            // Do some kind of processing to fix timing etc
            // Set the global SV and BPM in the pattern beatmap so the object end times can be calculated for the partitioning
            patternBeatmap.BeatmapTiming.SliderMultiplier = beatmap.BeatmapTiming.SliderMultiplier;
            patternBeatmap.BeatmapTiming.TimingPoints.RemoveAll(tp => tp.Uninherited);
            patternBeatmap.BeatmapTiming.TimingPoints.AddRange(beatmap.BeatmapTiming.GetAllRedlines());
            patternBeatmap.BeatmapTiming.Sort();
            patternBeatmap.CalculateSliderEndTimes();

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
    }
}