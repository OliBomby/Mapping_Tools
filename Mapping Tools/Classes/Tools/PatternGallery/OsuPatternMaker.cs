using System;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternMaker {
        public OsuPattern FromSelected(Beatmap beatmap, string name="") {
            var hitObjects = beatmap.HitObjects.Where(h => h.IsSelected).ToList();

            if (hitObjects.Count == 0)
                throw new Exception("No selected hit objects found.");

            var startTime = hitObjects.Min(h => h.Time) - 5;
            var endTime = hitObjects.Max(h => h.EndTime) + 5;

            var timingPoints = beatmap.BeatmapTiming.TimingPoints
                .Where(tp => tp.Offset >= startTime && tp.Offset <= endTime).ToList();

            var firstUnInheritedTimingPoint = hitObjects.First().UnInheritedTimingPoint;

            return new OsuPattern {
                Name = name,
                SaveDateTime = DateTime.UtcNow,
                Title = beatmap.Metadata["Title"].Value,
                Artist = beatmap.Metadata["Artist"].Value,
                Creator = beatmap.Metadata["Creator"].Value,
                Version = beatmap.Metadata["Version"].Value,
                HitObjects = hitObjects,
                TimingPoints = timingPoints,
                FirstUnInheritedTimingPoint = firstUnInheritedTimingPoint,
                SliderMultiplier = beatmap.Difficulty["SliderMultiplier"].DoubleValue,
                SliderTickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue,
                HpDrainRate = beatmap.Difficulty["HPDrainRate"].DoubleValue,
                CircleSize = beatmap.Difficulty["CircleSize"].DoubleValue,
                OverallDifficulty = beatmap.Difficulty["OverallDifficulty"].DoubleValue,
                ApproachRate = beatmap.Difficulty["ApproachRate"].DoubleValue,
                GameMode = (GameMode) beatmap.General["Mode"].IntValue,
                DefaultSampleSet = (SampleSet) Enum.Parse(typeof(SampleSet), beatmap.General["SampleSet"].Value),
                StackLeniency = beatmap.General["StackLeniency"].DoubleValue
            };
        }
    }
}