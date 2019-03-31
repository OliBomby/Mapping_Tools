using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SamplePackage {
        public double Time;
        public HashSet<Sample> Samples;

        public SamplePackage(double time, HashSet<Sample> samples) {
            Time = time;
            Samples = samples;
        }

        public SamplePackage(double time) {
            Time = time;
            Samples = new HashSet<Sample>();
        }

        public int GetSampleSet() {
            int sampleSet = 0;
            int bestPriority = int.MaxValue;
            foreach (Sample sample in Samples) {
                if (sample.Hitsound == 0 && sample.Priority < bestPriority) {
                    sampleSet = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }
            return sampleSet;
        }

        public int GetAdditions() {
            int additions = 0;
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
            int sampleSet = GetSampleSet();
            int additions = GetAdditions();

            HashSet<string> normals = new HashSet<string>(Samples.Where(o => o.Hitsound == 0).Select(o => o.SamplePath));
            HashSet<string> whistles = new HashSet<string>(Samples.Where(o => o.Hitsound == 1).Select(o => o.SamplePath));
            HashSet<string> finishes = new HashSet<string>(Samples.Where(o => o.Hitsound == 2).Select(o => o.SamplePath));
            HashSet<string> claps = new HashSet<string>(Samples.Where(o => o.Hitsound == 3).Select(o => o.SamplePath));

            CustomIndex ci = new CustomIndex();

            if (sampleSet == 1) {
                ci.Samples["normal-hitnormal"] = normals;
            } else if (sampleSet == 3) {
                ci.Samples["drum-hitnormal"] = normals;
            } else {
                ci.Samples["soft-hitnormal"] = normals;
            }

            if (additions == 1) {
                ci.Samples["normal-hitwhistle"] = whistles;
                ci.Samples["normal-hitfinish"] = finishes;
                ci.Samples["normal-hitclap"] = claps;
            } else if (additions == 3) {
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
    }
}
