using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapping_Tools_Core.Audio.SampleGeneration;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class CustomIndex : ICustomIndex {
        public int Index { get; set; }

        public Dictionary<string, HashSet<ISampleGenerator>> Samples { get; }
        
        private static IEnumerable<string> AllKeys => new[] { "normal-hitnormal", "normal-hitwhistle", "normal-hitfinish", "normal-hitclap",
                                                                         "soft-hitnormal", "soft-hitwhistle", "soft-hitfinish", "soft-hitclap",
                                                                         "drum-hitnormal", "drum-hitwhistle", "drum-hitfinish", "drum-hitclap" };

        public CustomIndex(int index) {
            Index = index;
            Samples = new Dictionary<string, HashSet<ISampleGenerator>>();
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<ISampleGenerator>();
            }
        }

        public CustomIndex() : this(-1) { }

        public static bool CheckSupport(HashSet<ISampleGenerator> s1, HashSet<ISampleGenerator> s2) {
            // s2 fits in s1 or s2 is empty
            return s2.Count <= 0 || s1.SetEquals(s2);
        }

        public static bool CheckCanSupport(HashSet<ISampleGenerator> s1, HashSet<ISampleGenerator> s2) {
            // s2 fits in s1 or s1 is empty or s2 is empty
            return s1.Count <= 0 || s2.Count <= 0 || s1.SetEquals(s2);
        }

        public bool Fits(ICustomIndex other) {
            // Every non-empty set from other == set from self
            return Samples.All(kvp => CheckSupport(kvp.Value, other.Samples[kvp.Key]));
        }

        public bool CanMerge(ICustomIndex other) {
            // Every non-empty set from other == non-empty set from self
            return Samples.All(kvp => CheckCanSupport(kvp.Value, other.Samples[kvp.Key]));
        }

        public void MergeWith(ICustomIndex other) {
            foreach (string key in AllKeys) {
                Samples[key].UnionWith(other.Samples[key]);
            }

            // If the other custom index has an assigned index and this one doesnt,
            // get the index, so optimised custom indices retain their indices.
            if (Index == -1 && other.Index != -1) {
                Index = other.Index;
            }
        }

        public object Clone() {
            ICustomIndex ci = new CustomIndex(Index);
            ci.MergeWith(this);
            return ci;
        }

        public void CleanInvalids() {
            // Remove all invalid generators but leave one invalid path in to mark the sample as occupied
            foreach (HashSet<ISampleGenerator> paths in Samples.Values) {
                int initialCount = paths.Count;
                paths.RemoveWhere(o => o == null || !o.IsValid());

                if (paths.Count == 0 && initialCount != 0) {
                    // All the generators were invalid and it didn't just start out empty
                    paths.Add(new DummySampleGenerator());  // Put this dummy here to prevent this hashset from getting new generators
                }
            }
        }

        public override string ToString() {
            var accumulator = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<ISampleGenerator>> kvp in Samples) {
                var sampleList = new StringBuilder();
                foreach (var sga in kvp.Value) {
                    sampleList.Append($"{sga}|");
                }
                if (sampleList.Length > 0)
                    sampleList.Remove(sampleList.Length - 1, 1);
                accumulator.Append($"{kvp.Key}: [{sampleList}]");
            }
            return accumulator.ToString();
        }

        public string GetNumberExtension() {
            return Index == 1 ? string.Empty : Index.ToInvariant();
        }
    }
}
