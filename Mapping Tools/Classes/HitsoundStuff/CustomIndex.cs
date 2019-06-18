using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class CustomIndex {
        public int Index;
        public Dictionary<string, HashSet<SampleGeneratingArgs>> Samples;
        public static readonly List<string> AllKeys = new List<string> { "normal-hitnormal", "normal-hitwhistle", "normal-hitfinish", "normal-hitclap",
                                                                         "soft-hitnormal", "soft-hitwhistle", "soft-hitfinish", "soft-hitclap",
                                                                         "drum-hitnormal", "drum-hitwhistle", "drum-hitfinish", "drum-hitclap" };

        public CustomIndex(int index) {
            Index = index;
            Samples = new Dictionary<string, HashSet<SampleGeneratingArgs>>();
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<SampleGeneratingArgs>();
            }
        }

        public CustomIndex() {
            Index = -1;
            Samples = new Dictionary<string, HashSet<SampleGeneratingArgs>>();
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<SampleGeneratingArgs>();
            }
        }

        public static bool CheckSupport(HashSet<SampleGeneratingArgs> s1, HashSet<SampleGeneratingArgs> s2) {
            // s2 fits in s1 or s2 is empty
            return s2.Count > 0 ? s1.SetEquals(s2) : true;
        }

        public static bool CheckCanSupport(HashSet<SampleGeneratingArgs> s1, HashSet<SampleGeneratingArgs> s2) {
            // s2 fits in s1 or s1 is empty or s2 is empty
            return s1.Count > 0 && s2.Count > 0 ? s1.SetEquals(s2) : true;
        }

        public bool CheckSupport(CustomIndex other) {
            // Every non-empty set from other == set from self
            // True until false
            bool support = true;
            foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in Samples) {
                support = CheckSupport(kvp.Value, other.Samples[kvp.Key]) && support; 
            }
            return support;
        }

        public bool CheckCanSupport(CustomIndex other) {
            // Every non-empty set from other == non-empty set from self
            // True until false
            bool support = true;
            foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in Samples) {
                support = CheckCanSupport(kvp.Value, other.Samples[kvp.Key]) && support;
            }
            return support;
        }

        public void MergeWith(CustomIndex other) {
            foreach (string key in AllKeys) {
                Samples[key].UnionWith(other.Samples[key]);
            }
        }

        public CustomIndex Merge(CustomIndex other) {
            CustomIndex ci = new CustomIndex();
            foreach (string key in AllKeys) {
                ci.Samples[key].UnionWith(other.Samples[key]);
            }
            return ci;
        }

        public void CleanInvalids() {
            // Replace all invalid paths with "" and remove the invalid path if another valid path is also in the hashset
            foreach (HashSet<SampleGeneratingArgs> paths in Samples.Values) {
                if (paths.Any(o => SampleImporter.ValidateSampleArgs(o))) {
                    paths.RemoveWhere(o => !SampleImporter.ValidateSampleArgs(o));
                } else if (paths.Count > 0) {
                    paths.Clear();
                    paths.Add(new SampleGeneratingArgs());  // This "" is here to prevent this hashset from getting new paths
                }
            }
        }
    }
}
