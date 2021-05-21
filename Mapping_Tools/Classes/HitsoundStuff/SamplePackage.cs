using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class SamplePackage {
        public double Time;
        public HashSet<Sample> Samples;

        public double MaxOutsideVolume => Samples.Max(s => s.OutsideVolume);

        public SamplePackage(double time, HashSet<Sample> samples) {
            Time = time;
            Samples = samples;
        }

        public SamplePackage(double time) {
            Time = time;
            Samples = new HashSet<Sample>();
        }

        public void SetAllOutsideVolume(double outsideVolume) {
            foreach (var sample in Samples) {
                sample.OutsideVolume = outsideVolume;
            }
        }

        /// <summary>
        /// Grabs the <see cref="SampleSet"/> relying on priority with both itself and other layers of the same sample.
        /// </summary>
        /// <returns></returns>
        public SampleSet GetSampleSet() {
            SampleSet sampleSet = SampleSet.Auto;
            int bestPriority = int.MaxValue;
            foreach (var sample in Samples) {
                if (sample.Hitsound == 0 && sample.Priority <= bestPriority) {
                    sampleSet = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }

            // If only auto was found, try to get a sampleset from the additions
            if (sampleSet == SampleSet.Auto) {
                bestPriority = int.MaxValue;
                foreach (var sample in Samples) {
                    if (sample.Hitsound != 0 && sample.Priority <= bestPriority) {
                        sampleSet = sample.SampleSet;
                        bestPriority = sample.Priority;
                    }
                }
            }

            return sampleSet;
        }

        public SampleSet GetAdditions() {
            SampleSet additions = SampleSet.Auto;
            int bestPriority = int.MaxValue;
            foreach (var sample in Samples) {
                if (sample.Hitsound != 0 && sample.Priority <= bestPriority) {
                    additions = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }

            // If only auto was found, try to get a sampleset from the normals
            if (additions == SampleSet.Auto) {
                bestPriority = int.MaxValue;
                foreach (var sample in Samples) {
                    if (sample.Hitsound == 0 && sample.Priority <= bestPriority) {
                        additions = sample.SampleSet;
                        bestPriority = sample.Priority;
                    }
                }
            }

            return additions;
        }

        public CustomIndex GetCustomIndex(SampleGeneratingArgsComparer comparer = null) {
            if (comparer == null)
                comparer = new SampleGeneratingArgsComparer();

            SampleSet sampleSet = GetSampleSet();
            SampleSet additions = GetAdditions();

            HashSet<SampleGeneratingArgs> normals = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Normal).Select(o => o.SampleArgs), comparer);
            HashSet<SampleGeneratingArgs> whistles = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Whistle).Select(o => o.SampleArgs), comparer);
            HashSet<SampleGeneratingArgs> finishes = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Finish).Select(o => o.SampleArgs), comparer);
            HashSet<SampleGeneratingArgs> claps = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Clap).Select(o => o.SampleArgs), comparer);
            
            CustomIndex ci = new CustomIndex(comparer);

            if (sampleSet == SampleSet.Normal) {
                ci.Samples["normal-hitnormal"] = normals;
            } else if (sampleSet == SampleSet.Drum) {
                ci.Samples["drum-hitnormal"] = normals;
            } else {
                ci.Samples["soft-hitnormal"] = normals;
            }

            if (additions == SampleSet.Normal) {
                ci.Samples["normal-hitwhistle"] = whistles;
                ci.Samples["normal-hitfinish"] = finishes;
                ci.Samples["normal-hitclap"] = claps;
            } else if (additions == SampleSet.Drum) {
                ci.Samples["drum-hitwhistle"] = whistles;
                ci.Samples["drum-hitfinish"] = finishes;
                ci.Samples["drum-hitclap"] = claps;
            } else {
                ci.Samples["soft-hitwhistle"] = whistles;
                ci.Samples["soft-hitfinish"] = finishes;
                ci.Samples["soft-hitclap"] = claps;
            }
            return ci;
        }

        /// <summary>
        /// Grabs the <see cref="HitsoundEvent"/> that is created into the specified sample custom index.
        /// </summary>
        /// <param name="index">The Custom Sample Index</param>
        /// <returns>The current custom index sample list.</returns>
        public HitsoundEvent GetHitsound(int index) {
            SampleSet sampleSet = GetSampleSet();
            SampleSet additions = GetAdditions();

            bool whistle = Samples.Any(o => o.Hitsound == Hitsound.Whistle);
            bool finish = Samples.Any(o => o.Hitsound == Hitsound.Finish);
            bool clap = Samples.Any(o => o.Hitsound == Hitsound.Clap);

            return new HitsoundEvent(Time, MaxOutsideVolume, sampleSet, additions, index, whistle, finish, clap);
        }
    }
}
