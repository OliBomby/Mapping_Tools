using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public class OsuBeatmapEncoder : IEncoder<Beatmap> {
        private readonly IEncoder<Storyboard> storyboardParser = new OsuStoryboardEncoder();

        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public bool SaveWithFloatPrecision { get; set; } = false;

        public IEnumerable<string> Encode(Beatmap beatmap) {
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
            foreach (string s in storyboardParser.Encode(beatmap.StoryBoard)) yield return s;
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