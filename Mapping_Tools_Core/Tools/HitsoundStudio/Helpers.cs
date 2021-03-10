using Mapping_Tools_Core.BeatmapHelper.Enums;
using System.Text.RegularExpressions;

namespace Mapping_Tools_Core.Tools.HitsoundStudio {
    public static class Helpers {
        public static SampleSet GetSamplesetFromFilename(string filename) {
            string[] split = filename.Split('-');
            if (split.Length < 1)
                return SampleSet.Soft;
            string sampleset = split[0];
            switch (sampleset) {
                case "auto":
                    return SampleSet.Auto;
                case "normal":
                    return SampleSet.Normal;
                case "soft":
                    return SampleSet.Soft;
                case "drum":
                    return SampleSet.Drum;
                default:
                    return SampleSet.Soft;
            }
        }

        public static Hitsound GetHitsoundFromFilename(string filename) {
            string[] split = filename.Split('-');
            if (split.Length < 2)
                return Hitsound.Normal;
            string hitsound = split[1];
            if (hitsound.Contains("hitnormal"))
                return Hitsound.Normal;
            if (hitsound.Contains("hitwhistle"))
                return Hitsound.Whistle;
            if (hitsound.Contains("hitfinish"))
                return Hitsound.Finish;
            if (hitsound.Contains("hitclap"))
                return Hitsound.Clap;
            return Hitsound.Normal;
        }

        public static int GetIndexFromFilename(string filename) {
            var match = Regex.Match(filename, "^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide)");

            var remainder = filename.Substring(match.Index + match.Length);
            int index = 0;
            if (!string.IsNullOrEmpty(remainder)) {
                InvariantHelper.TryParseInt(remainder, out index);
            }

            return index;
        }
    }
}