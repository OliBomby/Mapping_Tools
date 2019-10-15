using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class SamplePackage {
        public double Time;
        public double Volume;
        public HashSet<Sample> Samples;

        public SamplePackage(double time, HashSet<Sample> samples) {
            Time = time;
            Volume = 1;
            Samples = samples;
        }

        public SamplePackage(double time) {
            Time = time;
            Volume = 1;
            Samples = new HashSet<Sample>();
        }

        /// <summary>
        /// Grabs the <see cref="SampleSet"/> relying on priority with both itself and other layers of the same sample.
        /// </summary>
        /// <returns></returns>
        public SampleSet GetSampleSet() {
            SampleSet sampleSet = SampleSet.Auto;
            int bestPriority = int.MaxValue;
            foreach (var sample in Samples)
            {
                if (sample.Hitsound == 0 && sample.Priority < bestPriority)
                {
                    sampleSet = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }

            return sampleSet;
        }

        public SampleSet GetAdditions() {
            SampleSet additions = SampleSet.Auto;
            int bestPriority = int.MaxValue;
            foreach (Sample sample in Samples) {
                if (sample.Hitsound != 0 && sample.Priority < bestPriority) {
                    additions = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }
            return additions;
        }

        public CustomIndex GetCustomIndex() {
            SampleSet sampleSet = GetSampleSet();
            SampleSet additions = GetAdditions();

            HashSet<SampleGeneratingArgs> normals = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Normal).Select(o => o.SampleArgs));
            HashSet<SampleGeneratingArgs> whistles = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Whistle).Select(o => o.SampleArgs));
            HashSet<SampleGeneratingArgs> finishes = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Finish).Select(o => o.SampleArgs));
            HashSet<SampleGeneratingArgs> claps = new HashSet<SampleGeneratingArgs>(Samples.Where(o => o.Hitsound == Hitsound.Clap).Select(o => o.SampleArgs));
            
            CustomIndex ci = new CustomIndex();

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

            return new HitsoundEvent(Time, Volume, sampleSet, additions, index, whistle, finish, clap);
        }
    }
}
