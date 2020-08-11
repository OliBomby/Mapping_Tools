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

        public void PlaceOsuPatternAtTime(OsuPattern pattern, Beatmap beatmap, double time = double.NaN) {
            double offset = double.IsNaN(time) ? 0 : time - pattern.GetHitObjectStartTime();
            PlaceOsuPattern(pattern, beatmap, offset);
        }

        public void PlaceOsuPattern(OsuPattern pattern, Beatmap beatmap, double offset = 0) {
            // Remove stuff
            switch (OverwriteMode) {
                case PatternOverwriteMode.BasicOverwrite:
                    MakeSpaceForOsuPatternBasic(pattern, beatmap, offset);
                    break;
                case PatternOverwriteMode.SmartOverwrite:
                    MakeSpaceForOsuPatternSmart(pattern, beatmap, offset);
                    break;
                case PatternOverwriteMode.NoOverwrite:
                    break;
            }

            AddOsuPattern(pattern, beatmap, offset);
        }

        /// <summary>
        /// Removes hitobjects and timingpoints in the beatmap between the start and the end time of the pattern
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="beatmap"></param>
        /// <param name="offset"></param>
        public void MakeSpaceForOsuPatternBasic(OsuPattern pattern, Beatmap beatmap, double offset = 0) {
            var startTime = pattern.GetHitObjectStartTime() + offset - Padding;
            var endTime = pattern.GetHitObjectEndTime() + offset + Padding;

            beatmap.HitObjects.RemoveAll(h => h.Time >= startTime && h.Time <= endTime);
            beatmap.BeatmapTiming.TimingPoints.RemoveAll(tp => tp.Offset >= startTime && tp.Offset <= endTime);
        }

        /// <summary>
        /// Removes all hitobjects and timingpoints from the beatmap where it overlaps with objects of the pattern.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="beatmap"></param>
        /// <param name="offset"></param>
        public void MakeSpaceForOsuPatternSmart(OsuPattern pattern, Beatmap beatmap, double offset = 0) {
            beatmap.HitObjects = beatmap.HitObjects
                .Where(ho => !pattern.HitObjects.Any(pho => pho.Time + offset - Padding <= ho.Time && pho.EndTime + offset + Padding >= ho.Time)).ToList();
            beatmap.BeatmapTiming.TimingPoints = beatmap.BeatmapTiming.TimingPoints
                .Where(tp => !pattern.HitObjects.Any(pho => pho.Time + offset - Padding <= tp.Offset && pho.EndTime + offset + Padding >= tp.Offset)).ToList();

        }

        private void AddOsuPattern(OsuPattern pattern, Beatmap beatmap, double offset = 0) {
            var startTime = pattern.GetHitObjectStartTime() + offset - Padding;
            var endTime = pattern.GetHitObjectEndTime() + offset + Padding;

            // Copy so the original pattern doesnt get changed
            pattern = pattern.DeepCopy();

            if (offset != 0) {
                pattern.Offset(offset);
            }

            // Do some kind of processing to fix timing etc

            var timingPointsChanges = new List<TimingPointsChange>();

            beatmap.HitObjects.AddRange(pattern.HitObjects);
            beatmap.SortHitObjects();

            beatmap.BeatmapTiming.TimingPoints.AddRange(pattern.TimingPoints);
            beatmap.BeatmapTiming.Sort();

            if (OverwriteMode == PatternOverwriteMode.SmartOverwrite) {
                timingPointsChanges.AddRange(beatmap.HitObjects.Where(ho => ho.Time >= startTime && ho.EndTime <= endTime && ho.TimingPoint != null)
                    .Select(GetSvHitsoundChange));
                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);
            }
        }

        private TimingPointsChange GetSvHitsoundChange(HitObject ho) {
            var tp = ho.TimingPoint.Copy();
            tp.Offset = ho.Time;
            tp.Uninherited = false;
            tp.MpB = ho.SliderVelocity;
            return new TimingPointsChange(tp, true, false, true, true, true);
        }
    }
}