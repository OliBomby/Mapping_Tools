using Mapping_Tools_Core.BeatmapHelper.Events;
using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding {
    public class OsuBeatmapDecoder : IDecoder<Beatmap> {
        private readonly IDecoder<Storyboard> storyboardDecoder = new OsuStoryboardDecoder();

        public void Decode(Beatmap beatmap, IReadOnlyCollection<string> lines) {
            // Load up all the shit
            IEnumerable<string> generalLines = FileFormatHelper.GetCategoryLines(lines, "[General]");
            IEnumerable<string> editorLines = FileFormatHelper.GetCategoryLines(lines, "[Editor]");
            IEnumerable<string> metadataLines = FileFormatHelper.GetCategoryLines(lines, "[Metadata]");
            IEnumerable<string> difficultyLines = FileFormatHelper.GetCategoryLines(lines, "[Difficulty]");
            IEnumerable<string> timingLines = FileFormatHelper.GetCategoryLines(lines, "[TimingPoints]");
            IEnumerable<string> breakPeriodsLines = FileFormatHelper.GetCategoryLines(lines, "//Break Periods", new[] { "[", "//" });
            IEnumerable<string> colourLines = FileFormatHelper.GetCategoryLines(lines, "[Colours]");
            IEnumerable<string> hitobjectLines = FileFormatHelper.GetCategoryLines(lines, "[HitObjects]");

            FileFormatHelper.FillDictionary(beatmap.General, generalLines);
            FileFormatHelper.FillDictionary(beatmap.Editor, editorLines);
            FileFormatHelper.FillDictionary(beatmap.Metadata, metadataLines);
            FileFormatHelper.FillDictionary(beatmap.Difficulty, difficultyLines);

            foreach (string line in breakPeriodsLines) {
                beatmap.BreakPeriods.Add(new Break(line));
            }

            foreach (string line in colourLines) {
                if (line.Substring(0, 5) == "Combo") {
                    beatmap.ComboColoursList.Add(new ComboColour(line));
                } else {
                    beatmap.SpecialColours[FileFormatHelper.SplitKeyValue(line)[0].Trim()] = new ComboColour(line);
                }
            }

            foreach (string line in hitobjectLines) {
                beatmap.HitObjects.Add(new HitObject(line));
            }

            // Give the lines to the storyboard
            storyboardDecoder.Decode(beatmap.StoryBoard, lines);

            // Set the timing object
            beatmap.BeatmapTiming = new Timing(timingLines, beatmap.Difficulty["SliderMultiplier"].DoubleValue);

            beatmap.SortHitObjects();
            beatmap.CalculateHitObjectComboStuff();
            beatmap.CalculateSliderEndTimes();
            beatmap.GiveObjectsGreenlines();
        }

        public Beatmap DecodeNew(IReadOnlyCollection<string> lines) {
            var beatmap = new Beatmap();
            Decode(beatmap, lines);

            return beatmap;
        }
    }
}