using JetBrains.Annotations;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    /// <summary>
    /// Stores a dictionary with pairs (filename without ext., list of sample args which are satisfied by that file)
    /// Represents a schema on how to exports sample packages.
    /// </summary>
    public class SampleSchema : Dictionary<string, ISet<ISampleGenerator>>, ISampleSchema {
        [UsedImplicitly]
        public SampleSchema() { }

        public SampleSchema(IEnumerable<CustomIndex> customIndices) {
            foreach (var customIndex in customIndices) {
                foreach (var customIndexSample in customIndex.Samples) {
                    Add(customIndexSample.Key + customIndex.GetNumberExtension(), customIndexSample.Value);
                }
            }
        }

        public SampleSchema(IDictionary<ISampleGenerator, string> sampleNames) {
            foreach (var sample in sampleNames) {
                Add(sample.Value, new HashSet<ISampleGenerator> {sample.Key});
            }
        }

        /// <summary>
        /// Make sure a certain hitsound with a certain sound is in the <see cref="SampleSchema"/>.
        /// If it already exists, then it simply returns the index and sampleset of that filename.
        /// </summary>
        /// <param name="samples">List of <see cref="ISampleGenerator"/> that represents the sound that has to be made.</param>
        /// <param name="hitsoundName">Name of the hitsound. For example "hitwhistle" or "slidertick".</param>
        /// <param name="sampleSet">Sample set for the hitsound for if it adds a new sample to the sample schema.</param>
        /// <param name="newIndex">Index to start searching from. It will start at this value and go up until a slot is available.</param>
        /// <param name="newSampleSet">The sample set of the added sample.</param>
        /// <param name="startIndex">The index of the added sample.</param>
        /// <returns>True if it added a new entry.</returns>
        public bool AddHitsound(ISet<ISampleGenerator> samples, string hitsoundName, SampleSet sampleSet, out int newIndex,
            out SampleSet newSampleSet, int startIndex = 1) {

            // Check if our sample schema already has a sample for this
            var filename = FindFilename(samples, "^(normal|soft|drum)-" + hitsoundName);
            if (filename != null) {
                newIndex = Helpers.GetIndexFromFilename(filename);
                newSampleSet = Helpers.GetSamplesetFromFilename(filename);
                return false;
            }

            // Make a new sample with the same sound as all the samples mixed and add it to the sample schema
            int index = startIndex;
            newSampleSet = sampleSet;

            // Find an index which is not taken in the sample schema
            while (Keys.Any(o => Regex.IsMatch(o, "^(normal|soft|drum)-" + hitsoundName) &&
                                 Helpers.GetIndexFromFilename(o) == index &&
                                 Helpers.GetSamplesetFromFilename(o) == sampleSet)) {
                index++;
            }

            newIndex = index;
            filename = $"{sampleSet.ToString().ToLower()}-{hitsoundName}{(index == 1 ? string.Empty : index.ToInvariant())}";

            Add(filename, samples);
            return true;
        }

        public string FindFilename(ISet<ISampleGenerator> samples) {
            return (from kvp 
                in this 
                where kvp.Value.SequenceEqual(samples)
                select kvp.Key).FirstOrDefault();
        }

        public string FindFilename(ISet<ISampleGenerator> samples, string regexPattern) {
            return (from kvp
                    in this
                where kvp.Value.SequenceEqual(samples) && Regex.IsMatch(kvp.Key, regexPattern)
                select kvp.Key).FirstOrDefault();
        }

        /// <summary>
        /// Generates a dictionary which maps <see cref="ISampleGenerator"/> to their corresponding filename which makes that sample sound.
        /// Only maps the <see cref="ISampleGenerator"/> which are non-mixed.
        /// </summary>
        /// <returns></returns>
        public IDictionary<ISampleGenerator, string> GetSampleNames() {
            var sampleNames = new Dictionary<ISampleGenerator, string>();

            foreach (var kvp in this.Where(kvp => kvp.Value.Count == 1)) {
                if (!sampleNames.ContainsKey(kvp.Value.First())) {
                    sampleNames.Add(kvp.Value.First(), kvp.Key);
                }
            }

            return sampleNames;
        }

        public IList<ICustomIndex> GetCustomIndices() {
            var customIndices = new Dictionary<int, ICustomIndex>();

            foreach (var kvp in this) {
                var name = Path.GetFileNameWithoutExtension(kvp.Key);
                if (name == null) continue;

                var match = Regex.Match(name, "^(normal|soft|drum)-hit(normal|whistle|finish|clap)");
                if (!match.Success) continue;

                var hitsound = match.Value;

                var remainder = name.Substring(match.Index + match.Length);
                int index = 1;
                if (!string.IsNullOrEmpty(remainder)) {
                    if (!InvariantHelper.TryParseInt(remainder, out index)) {
                        continue;
                    }
                }

                if (customIndices.ContainsKey(index)) {
                    customIndices[index].Samples[hitsound] = new HashSet<ISampleGenerator>(kvp.Value);
                } else {
                    var ci = new CustomIndex(index);
                    customIndices.Add(index, ci);
                    ci.Samples[hitsound] = new HashSet<ISampleGenerator>(kvp.Value);
                }
            }

            return customIndices.Values.ToList();
        }

        public void MergeWith(ISampleSchema other) {
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