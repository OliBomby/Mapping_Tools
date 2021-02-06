using Mapping_Tools_Core.Audio;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using System.Collections.Generic;
using System.Linq;
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
                FileFormatHelper.TryParseInt(remainder, out index);
            }

            return index;
        }

        public static Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> LoadSampleSoundGenerators(ICollection<ISampleGeneratingArgs> argsList) {
            // Import all the samples
            var importedSamples = SampleImporter.ImportSamples(argsList.Select(o => o.ImportArgs));

            // Apply effects
            var samples = new Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator>();
            foreach (var args in argsList) {
                if (args.ImportArgs == null) {
                    samples[args] = null;
                    continue;
                }

                if (importedSamples.ContainsKey(args.ImportArgs)) {
                    samples[args] = args.ApplyEffects(importedSamples[args.ImportArgs]);
                }
            }

            return samples;
        }
    }
}