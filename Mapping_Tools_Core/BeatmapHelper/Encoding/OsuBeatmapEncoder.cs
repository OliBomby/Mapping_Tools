using Mapping_Tools_Core.BeatmapHelper.Encoding.HitObject;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.ComboColours;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public class OsuBeatmapEncoder : IEnumeratingEncoder<IBeatmap> {
        private readonly IEnumeratingEncoder<IStoryboard> storyboardEncoder;
        private readonly IEncoder<BeatmapHelper.HitObject> hitObjectEncoder;
        private readonly IEncoder<TimingPoint> timingPointEncoder;

        public OsuBeatmapEncoder() : this(new OsuStoryboardEncoder(), new HitObjectEncoder(), new TimingPointEncoder()) { }

        public OsuBeatmapEncoder(IEnumeratingEncoder<IStoryboard> storyboardEncoder,
            IEncoder<BeatmapHelper.HitObject> hitObjectEncoder,
            IEncoder<TimingPoint> timingPointEncoder) {
            this.storyboardEncoder = storyboardEncoder;
            this.hitObjectEncoder = hitObjectEncoder;
            this.timingPointEncoder = timingPointEncoder;
        }

        public IEnumerable<string> EncodeEnumerable(IBeatmap beatmap) {
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
            foreach (string s in storyboardEncoder.EncodeEnumerable(beatmap.StoryBoard)) yield return s;
            yield return "[TimingPoints]";
            foreach (TimingPoint tp in beatmap.BeatmapTiming.TimingPoints.Where(tp => tp != null)) {
                yield return timingPointEncoder.Encode(tp);
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
            foreach (BeatmapHelper.HitObject ho in beatmap.HitObjects) {
                yield return hitObjectEncoder.Encode(ho);
            }
        }

        public string Encode(IBeatmap obj) {
            return string.Join(Environment.NewLine, EncodeEnumerable(obj));
        }
    }
}