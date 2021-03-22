using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    /// <summary>
    /// 
    /// </summary>
    public class SamplePackage : ISamplePackage {
        public double Time { get; }
        public ISet<ISample> Samples { get; }

        public SamplePackage(double time, ISet<ISample> samples) {
            Time = time;
            Samples = samples;
        }

        public SamplePackage(double time) {
            Time = time;
            Samples = new HashSet<ISample>();
        }

        public void SetAllOutsideVolume(double outsideVolume) {
            foreach (var sample in Samples) {
                sample.OutsideVolume = outsideVolume;
            }
        }

        public double GetMaxOutsideVolume() => Samples.Max(s => s.OutsideVolume);

        public SampleSet GetSampleSet() {
            SampleSet sampleSet = SampleSet.Auto;
            int bestPriority = int.MaxValue;
            foreach (var sample in Samples)
            {
                if (sample.Hitsound == 0 && sample.Priority <= bestPriority)
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
            foreach (ISample sample in Samples) {
                if (sample.Hitsound != 0 && sample.Priority <= bestPriority) {
                    additions = sample.SampleSet;
                    bestPriority = sample.Priority;
                }
            }
            return additions;
        }

        public ICustomIndex GetCustomIndex() {
            SampleSet sampleSet = GetSampleSet();
            SampleSet additions = GetAdditions();

            HashSet<ISampleGenerator> normals = new HashSet<ISampleGenerator>(
                Samples.Where(o => o.Hitsound == Hitsound.Normal).Select(o => o.SampleGenerator));
            HashSet<ISampleGenerator> whistles = new HashSet<ISampleGenerator>(
                Samples.Where(o => o.Hitsound == Hitsound.Whistle).Select(o => o.SampleGenerator));
            HashSet<ISampleGenerator> finishes = new HashSet<ISampleGenerator>(
                Samples.Where(o => o.Hitsound == Hitsound.Finish).Select(o => o.SampleGenerator));
            HashSet<ISampleGenerator> claps = new HashSet<ISampleGenerator>(
                Samples.Where(o => o.Hitsound == Hitsound.Clap).Select(o => o.SampleGenerator));
            
            ICustomIndex ci = new CustomIndex();

            switch (sampleSet) {
                case SampleSet.Normal:
                    ci.Samples["normal-hitnormal"] = normals;
                    break;
                case SampleSet.Drum:
                    ci.Samples["drum-hitnormal"] = normals;
                    break;
                default:
                    // Soft
                    ci.Samples["soft-hitnormal"] = normals;
                    break;
            }

            switch (additions) {
                case SampleSet.Normal:
                    ci.Samples["normal-hitwhistle"] = whistles;
                    ci.Samples["normal-hitfinish"] = finishes;
                    ci.Samples["normal-hitclap"] = claps;
                    break;
                case SampleSet.Drum:
                    ci.Samples["drum-hitwhistle"] = whistles;
                    ci.Samples["drum-hitfinish"] = finishes;
                    ci.Samples["drum-hitclap"] = claps;
                    break;
                default:
                    // Soft
                    ci.Samples["soft-hitwhistle"] = whistles;
                    ci.Samples["soft-hitfinish"] = finishes;
                    ci.Samples["soft-hitclap"] = claps;
                    break;
            }
            return ci;
        }

        public IHitsoundEvent GetHitsound(int index) {
            SampleSet sampleSet = GetSampleSet();
            SampleSet additions = GetAdditions();

            bool whistle = Samples.Any(o => o.Hitsound == Hitsound.Whistle);
            bool finish = Samples.Any(o => o.Hitsound == Hitsound.Finish);
            bool clap = Samples.Any(o => o.Hitsound == Hitsound.Clap);

            return new HitsoundEvent(Time, GetMaxOutsideVolume(), sampleSet, additions, index, whistle, finish, clap);
        }
    }
}
