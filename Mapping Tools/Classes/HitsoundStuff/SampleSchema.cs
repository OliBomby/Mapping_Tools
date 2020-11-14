using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Stores a dictionary with pairs (filename without ext., list of sample args which are satisfied by that file)
    /// Represents a schema on how to exports sample packages.
    /// </summary>
    public class SampleSchema : Dictionary<string, List<SampleGeneratingArgs>> {
        [UsedImplicitly]
        public SampleSchema() { }

        public SampleSchema(IEnumerable<CustomIndex> customIndices) {
            foreach (var customIndex in customIndices) {
                foreach (var customIndexSample in customIndex.Samples) {
                    Add(customIndexSample.Key + customIndex.GetNumberExtension(), customIndexSample.Value.ToList());
                }
            }
        }

        public SampleSchema(Dictionary<SampleGeneratingArgs, string> sampleNames) {
            foreach (var sample in sampleNames) {
                Add(sample.Value, new List<SampleGeneratingArgs> {sample.Key});
            }
        }

        /// <summary>
        /// Make sure a certain hitsound with a certain sound is in the <see cref="SampleSchema"/>.
        /// If it already exists, then it simply returns the index and sampleset of that filename.
        /// </summary>
        /// <param name="samples">List of <see cref="SampleGeneratingArgs"/> that represents the sound that has to be made.</param>
        /// <param name="hitsoundName">Name of the hitsound. For example "hitwhistle" or "slidertick".</param>
        /// <param name="sampleSet">Sample set for the hitsound for if it adds a new sample to the sample schema.</param>
        /// <param name="newIndex">Index to start searching from. It will start at this value and go up until a slot is available.</param>
        /// <param name="newSampleSet">The sample set of the added sample.</param>
        /// <param name="startIndex">The index of the added sample.</param>
        /// <returns>True if it added a new entry.</returns>
        public bool AddHitsound(List<SampleGeneratingArgs> samples, string hitsoundName, SampleSet sampleSet, out int newIndex,
            out SampleSet newSampleSet, int startIndex = 1) {

            // Check if our sample schema already has a sample for this
            var filename = FindFilename(samples, "^(normal|soft|drum)-" + hitsoundName);
            if (filename != null) {
                newIndex = HitsoundImporter.GetIndexFromFilename(filename);
                newSampleSet = HitsoundImporter.GetSamplesetFromFilename(filename);
                return false;
            }

            // Make a new sample with the same sound as all the samples mixed and add it to the sample schema
            int index = startIndex;
            newSampleSet = sampleSet;

            // Find an index which is not taken in the sample schema
            while (Keys.Any(o => Regex.IsMatch(o, "^(normal|soft|drum)-" + hitsoundName) &&
                                 HitsoundImporter.GetIndexFromFilename(o) == index &&
                                 HitsoundImporter.GetSamplesetFromFilename(o) == sampleSet)) {
                index++;
            }

            newIndex = index;
            filename = $"{sampleSet.ToString().ToLower()}-{hitsoundName}{(index == 1 ? string.Empty : index.ToInvariant())}";

            Add(filename, samples);
            return true;
        }

        public string FindFilename(List<SampleGeneratingArgs> samples) {
            return (from kvp 
                in this 
                where kvp.Value.SequenceEqual(samples)
                select kvp.Key).FirstOrDefault();
        }

        public string FindFilename(List<SampleGeneratingArgs> samples, string regexPattern) {
            return (from kvp
                    in this
                where kvp.Value.SequenceEqual(samples) && Regex.IsMatch(kvp.Key, regexPattern)
                select kvp.Key).FirstOrDefault();
        }

        /// <summary>
        /// Generates a dictionary which maps <see cref="SampleGeneratingArgs"/> to their corresponding filename which makes that sample sound.
        /// Only maps the <see cref="SampleGeneratingArgs"/> which are non-mixed.
        /// </summary>
        /// <returns></returns>
        public Dictionary<SampleGeneratingArgs, string> GetSampleNames(SampleGeneratingArgsComparer comparer = null) {
            var sampleNames = new Dictionary<SampleGeneratingArgs, string>(comparer ?? new SampleGeneratingArgsComparer());

            foreach (var kvp in this.Where(kvp => kvp.Value.Count == 1)) {
                if (!sampleNames.ContainsKey(kvp.Value[0])) {
                    sampleNames.Add(kvp.Value[0], kvp.Key);
                }
            }

            return sampleNames;
        }

        public List<CustomIndex> GetCustomIndices(SampleGeneratingArgsComparer comparer = null) {
            if (comparer == null)
                comparer = new SampleGeneratingArgsComparer();

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
                    var ci = new CustomIndex(index, comparer);
                    customIndices.Add(index, ci);
                    ci.Samples[hitsound] = new HashSet<SampleGeneratingArgs>(kvp.Value, comparer);
                }
            }

            return customIndices.Values.ToList();
        }

        public void MergeWith(SampleSchema other) {
            foreach (var kvp in other) {
                if (!ContainsKey(kvp.Key)) {
                    Add(kvp.Key, kvp.Value);
                } else if (this[kvp.Key].Count == 0) {
                    // Allow overwriting of value if the list of samples is empty, because those entries are useless
                    this[kvp.Key] = kvp.Value;
                }
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