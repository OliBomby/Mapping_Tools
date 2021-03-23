using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Events;

namespace Mapping_Tools_Core.BeatmapHelper.Parsing {
    public class OsuBeatmapParser : IParser<Beatmap> {
        private readonly OsuStoryboardParser storyboardParser = new OsuStoryboardParser();

        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public bool SaveWithFloatPrecision { get; set; } = false;

        public void Parse(Beatmap beatmap, IReadOnlyCollection<string> lines) {
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
            storyboardParser.Parse(beatmap.StoryBoard, lines);

            // Set the timing object
            beatmap.BeatmapTiming = new Timing(timingLines, beatmap.Difficulty["SliderMultiplier"].DoubleValue);

            beatmap.SortHitObjects();
            beatmap.CalculateHitObjectComboStuff();
            beatmap.CalculateSliderEndTimes();
            beatmap.GiveObjectsGreenlines();
        }

        public Beatmap ParseNew(IReadOnlyCollection<string> lines) {
            var beatmap = new Beatmap();
            Parse(beatmap, lines);

            return beatmap;
        }

        public IEnumerable<string> Serialize(Beatmap beatmap) {
            // Getting all the stuff
            yield return "osu file format v14";
            yield return "";
            yield return "[General]";
            foreach (string s in FileFormatHelper.EnumerateDictionary(beatmap.General)) yield return s;
            yield return "";
            yield return "[Editor]";
            foreach (string s in FileFormatHelper.EnumerateDictionary(beatmap.Editor)) yield return s;
            yield return "";
            yield return "[Metadata]";
            foreach (string s in FileFormatHelper.EnumerateDictionary(beatmap.Metadata)) yield return s;
            yield return "";
            yield return "[Difficulty]";
            foreach (string s in FileFormatHelper.EnumerateDictionary(beatmap.Difficulty)) yield return s;
            yield return "";
            foreach (string s in storyboardParser.Serialize(beatmap.StoryBoard)) yield return s;
            yield return "[TimingPoints]";
            foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints.Where(tp => tp != null)) {
                tp.SaveWithFloatPrecision = SaveWithFloatPrecision;
                yield return tp.GetLine();
            }
            yield return "";
            if (beatmap.ComboColoursList.Any()) {
                yield return "";
                yield return "[Colours]";
                foreach (string s in beatmap.ComboColoursList.Select((comboColour, i) => "Combo" + (i + 1) + " : " +
                                                                                       ComboColour.SerializeComboColour(comboColour)))
                    yield return s;
                foreach (string s in beatmap.SpecialColours.Select(specialColour => specialColour.Key + " : " +
                                                                                  ComboColour.SerializeComboColour(specialColour.Value)))
                    yield return s;
            }
            yield return "";
            yield return "[HitObjects]";
            foreach (HitObject ho in beatmap.HitObjects) {
                ho.SaveWithFloatPrecision = SaveWithFloatPrecision;
                yield return ho.GetLine();
            }
        }
    }
}