using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class CustomIndex {
        public int Index;
        public Dictionary<string, HashSet<string>> Samples;
        public static readonly List<string> AllKeys = new List<string> { "normal-hitnormal", "normal-hitwhistle", "normal-hitfinish", "normal-hitclap",
                                                                         "soft-hitnormal", "soft-hitwhistle", "soft-hitfinish", "soft-hitclap",
                                                                         "drum-hitnormal", "drum-hitwhistle", "drum-hitfinish", "drum-hitclap" };

        public CustomIndex(int index) {
            Index = index;
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<string>();
            }
        }

        public CustomIndex() {
            Index = 0;
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<string>();
            }
        }

        public static bool CheckSupport(HashSet<string> s1, HashSet<string> s2) {
            // s2 fits in s1 or s2 is empty
            return s2.Count > 0 ? s1 == s2 : true;
        }

        public static bool CheckCanSupport(HashSet<string> s1, HashSet<string> s2) {
            // s2 fits in s1 or s1 is empty or s2 is empty
            return s1.Count > 0 && s2.Count > 0 ? s1 == s2 : true;
        }

        public bool CheckSupport(CustomIndex other) {
            // Every non-empty set from other == set from self
            // True until false
            bool support = true;
            foreach (KeyValuePair<string, HashSet<string>> kvp in Samples) {
                support = CheckSupport(kvp.Value, other.Samples[kvp.Key]) && support; 
            }
            return support;
        }

        public bool CheckCanSupport(CustomIndex other) {
            // Every non-empty set from other == non-empty set from self
            // True until false
            bool support = true;
            foreach (KeyValuePair<string, HashSet<string>> kvp in Samples) {
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
    }
}
