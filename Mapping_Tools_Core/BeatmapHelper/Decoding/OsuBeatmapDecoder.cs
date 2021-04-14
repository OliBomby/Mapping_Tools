using System;
using Mapping_Tools_Core.BeatmapHelper.Events;
using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.ComboColours;
using Mapping_Tools_Core.BeatmapHelper.Decoding.HitObject;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding {
    public class OsuBeatmapDecoder : IDecoder<Beatmap> {
        private readonly IDecoder<Storyboard> storyboardDecoder;
        private readonly IDecoder<BeatmapHelper.HitObject> hitObjectDecoder;
        private readonly IDecoder<TimingPoint> timingPointDecoder;

        public OsuBeatmapDecoder() : this(new OsuStoryboardDecoder(), new HitObjectDecoder(), new TimingPointDecoder()) { }

        public OsuBeatmapDecoder(IDecoder<Storyboard> storyboardDecoder, IDecoder<BeatmapHelper.HitObject> hitObjectDecoder, IDecoder<TimingPoint> timingPointDecoder) {
            this.storyboardDecoder = storyboardDecoder;
            this.hitObjectDecoder = hitObjectDecoder;
            this.timingPointDecoder = timingPointDecoder;
        }

        public void Decode(Beatmap beatmap, string code) {
            var lines = code.Split(Environment.NewLine);

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
                beatmap.HitObjects.Add(hitObjectDecoder.DecodeNew(line));
            }

            // Give the lines to the storyboard
            storyboardDecoder.Decode(beatmap.StoryBoard, code);

            // Set the timing object
            beatmap.BeatmapTiming = new Timing(beatmap.Difficulty["SliderMultiplier"].DoubleValue);

            foreach (var timingLine in timingLines) {
                beatmap.BeatmapTiming.Add(timingPointDecoder.DecodeNew(timingLine));
            }

            beatmap.SortHitObjects();
            beatmap.CalculateHitObjectComboStuff();
            beatmap.GiveObjectsTimingContext();
        }

        public Beatmap DecodeNew(string code) {
            var beatmap = new Beatmap();
            Decode(beatmap, code);

            return beatmap;
        }
    }
}