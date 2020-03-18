using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SampleSchema : Dictionary<string, List<SampleGeneratingArgs>> {
        [UsedImplicitly]
        public SampleSchema() { }

        public SampleSchema(IEnumerable<CustomIndex> customIndices) {
            foreach (var customIndex in customIndices) {
                foreach (var customIndexSample in customIndex.Samples) {
                    Add(customIndexSample.Key + customIndex.GetNumberExtension() + ".wav", customIndexSample.Value.ToList());
                }
            }
        }

        public SampleSchema(Dictionary<SampleGeneratingArgs, string> sampleNames) {
            foreach (var sample in sampleNames) {
                Add(sample.Value, new List<SampleGeneratingArgs> {sample.Key});
            }
        }

        public Dictionary<SampleGeneratingArgs, string> GetSampleNames() {
            var sampleNames = new Dictionary<SampleGeneratingArgs, string>();

            foreach (var kvp in this.Where(kvp => kvp.Value.Count == 1)) {
                if (!sampleNames.ContainsKey(kvp.Value[0])) {
                    sampleNames.Add(kvp.Value[0], kvp.Key);
                }
            }

            return sampleNames;
        }

        public List<CustomIndex> GetCustomIndices() {
            var customIndices = new Dictionary<int, CustomIndex>();

            foreach (var kvp in this) {
                var name = Path.GetFileNameWithoutExtension(kvp.Key);
                if (name == null) continue;

                var match = Regex.Match(name, "^(normal|soft|drum)-hit(normal|whistle|finish|clap)");
                if (!match.Success) continue;

                var hitsound = match.Value;

                var remainder = name.Substring(match.Index + match.Length);
                int index = 1;
                if (!string.IsNullOrEmpty(remainder)) {
                    if (!FileFormatHelper.TryParseInt(remainder, out index)) {
                        continue;
                    }
                }

                if (customIndices.ContainsKey(index)) {
                    customIndices[index].Samples[hitsound] = new HashSet<SampleGeneratingArgs>(kvp.Value);
                } else {
                    var ci = new CustomIndex(index);
                    customIndices.Add(index, ci);
                    ci.Samples[hitsound] = new HashSet<SampleGeneratingArgs>(kvp.Value);
                }
            }

            return customIndices.Values.ToList();
        }

        public void MergeWith(SampleSchema other) {
            foreach (var kvp in other.Where(kvp => !ContainsKey(kvp.Key))) {
                Add(kvp.Key, kvp.Value);
            }
        }

        public override string ToString() {
            var builder = new StringBuilder();

            foreach (var kvp in this) {
                var sampleList = new StringBuilder();
                foreach (var sga in kvp.Value) {
                    sampleList.Append($"{sga}|");
                }
                if (sampleList.Length > 0)
                    sampleList.Remove(sampleList.Length - 1, 1);
                builder.AppendLine($"{kvp.Key}: [{sampleList}]");
            }

            return builder.ToString();
        }
    }
}