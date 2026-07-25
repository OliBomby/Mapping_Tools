using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Groups every generated sample layer that plays at one timestamp.
    /// </summary>
    public class SamplePackage {
        /// <summary>
        /// The common playback time in milliseconds.
        /// </summary>
        public double Time;
        /// <summary>
        /// The distinct sample layers that play at <see cref="Time"/>.
        /// </summary>
        public HashSet<Sample> Samples;

        /// <summary>
        /// Gets the loudest event-level volume among the package layers.
        /// </summary>
        public double MaxOutsideVolume => Samples.Max(s => s.OutsideVolume);

        /// <summary>
        /// Creates a package around an existing sample set.
        /// </summary>
        /// <param name="time">The playback time in milliseconds.</param>
        /// <param name="samples">The layers playing at that time.</param>
        public SamplePackage(double time, HashSet<Sample> samples) {
            Time = time;
            Samples = samples;
        }

        /// <summary>
        /// Creates an empty package at a playback time.
        /// </summary>
        /// <param name="time">The playback time in milliseconds.</param>
        public SamplePackage(double time) {
            Time = time;
            Samples = new HashSet<Sample>();
        }

        /// <summary>
        /// Applies one event-level volume multiplier to every layer in the package.
        /// </summary>
        /// <param name="outsideVolume">The multiplier assigned to each sample.</param>
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
            SampleSet sampleSet = SampleSet.None;
            int bestPriority = int.MaxValue;
            foreach (var sample in Samples) {
                if (sample.Hitsound == 0 && sample.Priority <= bestPriority) {
                    sampleSet = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }

            // If only auto was found, try to get a sampleset from the additions
            if (sampleSet == SampleSet.None) {
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

        /// <summary>
        /// Resolves the addition sample family by lowest priority, falling back to normal layers.
        /// </summary>
        /// <returns>The family for whistle, finish, and clap, or auto when none is available.</returns>
        public SampleSet GetAdditions() {
            SampleSet additions = SampleSet.None;
            int bestPriority = int.MaxValue;
            foreach (var sample in Samples) {
                if (sample.Hitsound != 0 && sample.Priority <= bestPriority) {
                    additions = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }

            // If only auto was found, try to get a sampleset from the normals
            if (additions == SampleSet.None) {
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

        /// <summary>
        /// Converts package layers into the twelve standard sample slots of an unassigned custom index.
        /// </summary>
        /// <param name="comparer">The identity policy used to deduplicate generation arguments.</param>
        /// <returns>A custom-index requirement whose sample family follows the resolved normal and addition sets.</returns>
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
