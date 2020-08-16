using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    /// <summary>
    /// Helper class for placing a <see cref="OsuPattern"/> into a <see cref="Beatmap"/>.
    /// </summary>
    public class OsuPatternPlacer {
        public double Padding { get; set; } = 5;
        public PatternOverwriteMode OverwriteMode { get; set; } = PatternOverwriteMode.SmartOverwrite;

        public void PlaceOsuPatternAtTime(Beatmap patternBeatmap, Beatmap beatmap, double time = double.NaN) {
            double offset = double.IsNaN(time) ? 0 : time - patternBeatmap.GetHitObjectStartTime();
            PlaceOsuPattern(patternBeatmap, beatmap, offset);
        }

        public void PlaceOsuPattern(Beatmap patternBeatmap, Beatmap beatmap, double offset = 0) {
            // Remove stuff
            switch (OverwriteMode) {
                case PatternOverwriteMode.BasicOverwrite:
                    MakeSpaceForOsuPatternBasic(patternBeatmap, beatmap, offset);
                    break;
                case PatternOverwriteMode.SmartOverwrite:
                    MakeSpaceForOsuPatternSmart(patternBeatmap, beatmap, offset);
                    break;
                case PatternOverwriteMode.NoOverwrite:
                    break;
            }

            AddOsuPattern(patternBeatmap, beatmap, offset);
        }

        /// <summary>
        /// Removes hitobjects and timingpoints in the beatmap between the start and the end time of the pattern
        /// </summary>
        /// <param name="patternBeatmap"></param>
        /// <param name="beatmap"></param>
        /// <param name="offset"></param>
        public void MakeSpaceForOsuPatternBasic(Beatmap patternBeatmap, Beatmap beatmap, double offset = 0) {
            var startTime = patternBeatmap.GetHitObjectStartTime() + offset - Padding;
            var endTime = patternBeatmap.GetHitObjectEndTime() + offset + Padding;

            beatmap.HitObjects.RemoveAll(h => h.Time >= startTime && h.Time <= endTime);
            beatmap.BeatmapTiming.TimingPoints.RemoveAll(tp => tp.Offset >= startTime && tp.Offset <= endTime);
        }

        /// <summary>
        /// Removes all hitobjects and timingpoints from the beatmap where it overlaps with objects of the pattern.
        /// </summary>
        /// <param name="patternBeatmap"></param>
        /// <param name="beatmap"></param>
        /// <param name="offset"></param>
        public void MakeSpaceForOsuPatternSmart(Beatmap patternBeatmap, Beatmap beatmap, double offset = 0) {
            beatmap.HitObjects = beatmap.HitObjects
                .Where(ho => !patternBeatmap.HitObjects.Any(pho => pho.Time + offset - Padding <= ho.Time && pho.EndTime + offset + Padding >= ho.Time)).ToList();
            beatmap.BeatmapTiming.TimingPoints = beatmap.BeatmapTiming.TimingPoints
                .Where(tp => !patternBeatmap.HitObjects.Any(pho => pho.Time + offset - Padding <= tp.Offset && pho.EndTime + offset + Padding >= tp.Offset)).ToList();

        }

        private void AddOsuPattern(Beatmap patternBeatmap, Beatmap beatmap, double offset = 0, bool protectBeatmapPattern = true) {
            if (protectBeatmapPattern) {
                // Copy so the original pattern doesnt get changed
                patternBeatmap = patternBeatmap.DeepCopy();
            }

            var startTime = patternBeatmap.GetHitObjectStartTime() + offset - Padding;
            var endTime = patternBeatmap.GetHitObjectEndTime() + offset + Padding;

            if (offset != 0) {
                patternBeatmap.OffsetTime(offset);
            }

            // Do some kind of processing to fix timing etc

            var timingPointsChanges = new List<TimingPointsChange>();

            beatmap.HitObjects.AddRange(patternBeatmap.HitObjects);
            beatmap.SortHitObjects();

            beatmap.BeatmapTiming.TimingPoints.AddRange(patternBeatmap.BeatmapTiming.TimingPoints);
            beatmap.BeatmapTiming.Sort();

            if (OverwriteMode == PatternOverwriteMode.SmartOverwrite) {
                timingPointsChanges.AddRange(beatmap.HitObjects.Where(ho => ho.Time >= startTime && ho.EndTime <= endTime && ho.TimingPoint != null)
                    .Select(GetSvHitsoundChange));
                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);
            }
        }

        private static TimingPointsChange GetSvHitsoundChange(HitObject ho) {
            var tp = ho.TimingPoint.Copy();
            tp.Offset = ho.Time;
            tp.Uninherited = false;
            tp.MpB = ho.SliderVelocity;
            return new TimingPointsChange(tp, true, false, true, true, true);
        }
    }
}