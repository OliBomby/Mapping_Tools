using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.RhythmGuide {
    public class RhythmGuideGenerator {
        public static Beatmap NewRhythmGuide(IRhythmGuideArgs args, Beatmap templateBeatmap, GameMode gameMode, string diffName) {
            // Scuffed beatmap copy
            var newBeatmap = templateBeatmap.DeepClone();

            // Remove all greenlines
            newBeatmap.BeatmapTiming.RemoveAll(o => !o.Uninherited);

            // Remove all hitobjects
            newBeatmap.HitObjects.Clear();

            // Change some parameters;
            newBeatmap.General["StackLeniency"] = new TValue("0.0");
            newBeatmap.General["Mode"] = new TValue(((int)gameMode).ToString());
            newBeatmap.Metadata["Version"] = new TValue(diffName);
            newBeatmap.Difficulty["CircleSize"] = new TValue("4");

            // Add hitobjects
            AddRhythmGuideToBeatmap(newBeatmap, args);

            return newBeatmap;
        }

        public static void AddRhythmGuideToBeatmap(Beatmap beatmap, IRhythmGuideArgs args) {
            // Get the times from all beatmaps
            var times = new HashSet<double>();
            foreach (var b in args.InputBeatmaps) {
                foreach (var t in GetTimesFromBeatmap(b, args)) {
                    times.Add(t);
                }
            }

            // Import hitcircles at those times
            foreach (var ho in times.Select(time => new HitObject(time, 0, SampleSet.Auto, SampleSet.Auto))) {
                ho.NewCombo = args.NcEverything;
                beatmap.HitObjects.Add(ho);
            }
        }

        private static IEnumerable<double> GetTimesFromBeatmap(Beatmap beatmap, IRhythmGuideArgs args) {
            var timeline = beatmap.GetTimeline();
            foreach (var timelineObject in timeline.TimelineObjects) {
                // Handle different selection modes
                switch (args.SelectionMode) {
                    case SelectionMode.AllEvents:
                        yield return beatmap.BeatmapTiming.Resnap(timelineObject.Time, args.BeatDivisors);

                        break;
                    case SelectionMode.HitsoundEvents:
                        if (timelineObject.HasHitsound) {
                            yield return beatmap.BeatmapTiming.Resnap(timelineObject.Time, args.BeatDivisors);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}