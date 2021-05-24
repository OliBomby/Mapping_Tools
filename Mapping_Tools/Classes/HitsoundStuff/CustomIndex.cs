using System;
using System.Collections.Generic;
using System.Text;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.HitsoundStuff {

    /// <summary>
    /// 
    /// </summary>
    public class CustomIndex {

        /// <summary>
        /// 
        /// </summary>
        public int Index;

        private SampleGeneratingArgsComparer Comparer;

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, HashSet<SampleGeneratingArgs>> Samples;
        
        /// <summary>
        /// 
        /// </summary>
        public static readonly List<string> AllKeys = new List<string> { "normal-hitnormal", "normal-hitwhistle", "normal-hitfinish", "normal-hitclap",
                                                                         "soft-hitnormal", "soft-hitwhistle", "soft-hitfinish", "soft-hitclap",
                                                                         "drum-hitnormal", "drum-hitwhistle", "drum-hitfinish", "drum-hitclap" };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public CustomIndex(int index, SampleGeneratingArgsComparer comparer = null) {
            Index = index;
            Comparer = comparer ?? new SampleGeneratingArgsComparer();
            Samples = new Dictionary<string, HashSet<SampleGeneratingArgs>>();
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<SampleGeneratingArgs>(Comparer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public CustomIndex(SampleGeneratingArgsComparer comparer = null) {
            Index = -1;
            Comparer = comparer ?? new SampleGeneratingArgsComparer();
            Samples = new Dictionary<string, HashSet<SampleGeneratingArgs>>();
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<SampleGeneratingArgs>(Comparer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool CheckSupport(HashSet<SampleGeneratingArgs> s1, HashSet<SampleGeneratingArgs> s2) {
            // s2 fits in s1 or s2 is empty
            return s2.Count <= 0 || s1.SetEquals(s2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool CheckCanSupport(HashSet<SampleGeneratingArgs> s1, HashSet<SampleGeneratingArgs> s2) {
            // s2 fits in s1 or s1 is empty or s2 is empty
            return s1.Count <= 0 || s2.Count <= 0 || s1.SetEquals(s2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Fits(CustomIndex other) {
            // Every non-empty set from other == set from self
            // True until false
            bool support = true;
            foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in Samples) {
                support = CheckSupport(kvp.Value, other.Samples[kvp.Key]) && support; 
            }
            return support;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CanMerge(CustomIndex other) {
            // Every non-empty set from other == non-empty set from self
            // True until false
            bool support = true;
            foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in Samples) {
                support = CheckCanSupport(kvp.Value, other.Samples[kvp.Key]) && support;
            }
            return support;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        public void MergeWith(CustomIndex other) {
            foreach (string key in AllKeys) {
                Samples[key].UnionWith(other.Samples[key]);
            }

            // If the other custom index has an assigned index and this one doesnt. Get the index, so optimised custom indices retain their indices.
            if (Index == -1 && other.Index != -1) {
                Index = other.Index;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public CustomIndex Merge(CustomIndex other) {
            CustomIndex ci = new CustomIndex(Math.Max(Index, other.Index));
            foreach (string key in AllKeys) {
                ci.Samples[key].UnionWith(other.Samples[key]);
            }
            return ci;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CustomIndex Copy() {
            CustomIndex ci = new CustomIndex(Index, Comparer);
            ci.MergeWith(this);
            return ci;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadedSamples"></param>
        public void CleanInvalids(Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null, bool validateSampleFile = true) {
            // Replace all invalid paths with "" and remove the invalid path if another valid path is also in the hashset
            foreach (HashSet<SampleGeneratingArgs> paths in Samples.Values) {
                int initialCount = paths.Count;
                int removed = paths.RemoveWhere(o => !SampleImporter.ValidateSampleArgs(o, loadedSamples, validateSampleFile));

                if (paths.Count == 0 && initialCount != 0) {
                    // All the paths where invalid and it didn't just start out empty
                    paths.Add(new SampleGeneratingArgs());  // This "" is here to prevent this hashset from getting new paths
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var accumulator = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in Samples) {
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
