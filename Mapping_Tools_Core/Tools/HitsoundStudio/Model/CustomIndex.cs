using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.BeatmapHelper;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {

    /// <summary>
    /// 
    /// </summary>
    public class CustomIndex : ICustomIndex {
        public int Index { get; set; }

        public Dictionary<string, HashSet<ISampleGeneratingArgs>> Samples { get; }
        
        /// <summary>
        /// 
        /// </summary>
        private static IEnumerable<string> AllKeys => new[] { "normal-hitnormal", "normal-hitwhistle", "normal-hitfinish", "normal-hitclap",
                                                                         "soft-hitnormal", "soft-hitwhistle", "soft-hitfinish", "soft-hitclap",
                                                                         "drum-hitnormal", "drum-hitwhistle", "drum-hitfinish", "drum-hitclap" };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public CustomIndex(int index) {
            Index = index;
            Samples = new Dictionary<string, HashSet<ISampleGeneratingArgs>>();
            foreach (string key in AllKeys) {
                Samples[key] = new HashSet<ISampleGeneratingArgs>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public CustomIndex() : this(-1) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool CheckSupport(HashSet<ISampleGeneratingArgs> s1, HashSet<ISampleGeneratingArgs> s2) {
            // s2 fits in s1 or s2 is empty
            return s2.Count <= 0 || s1.SetEquals(s2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static bool CheckCanSupport(HashSet<ISampleGeneratingArgs> s1, HashSet<ISampleGeneratingArgs> s2) {
            // s2 fits in s1 or s1 is empty or s2 is empty
            return s1.Count <= 0 || s2.Count <= 0 || s1.SetEquals(s2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Fits(ICustomIndex other) {
            // Every non-empty set from other == set from self
            return Samples.All(kvp => CheckSupport(kvp.Value, other.Samples[kvp.Key]));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CanMerge(ICustomIndex other) {
            // Every non-empty set from other == non-empty set from self
            return Samples.All(kvp => CheckCanSupport(kvp.Value, other.Samples[kvp.Key]));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Clone() {
            ICustomIndex ci = new CustomIndex(Index);
            ci.MergeWith(this);
            return ci;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadedSamples"></param>
        public void CleanInvalids(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples = null) {
            // Remove all invalid paths but leave one invalid path in to mark the sample as occupied
            foreach (HashSet<ISampleGeneratingArgs> paths in Samples.Values) {
                int initialCount = paths.Count;
                paths.RemoveWhere(o => !o.IsValid(loadedSamples));

                if (paths.Count == 0 && initialCount != 0) {
                    // All the paths were invalid and it didn't just start out empty
                    paths.Add(new SampleGeneratingArgs());  // This invalid path is here to prevent this hashset from getting new paths
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var accumulator = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<ISampleGeneratingArgs>> kvp in Samples) {
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
