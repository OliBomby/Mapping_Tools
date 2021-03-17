using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class Sample : ISample {
        public Sample(IHitsoundLayer hitsoundLayer) : this(hitsoundLayer.SampleGenerator, hitsoundLayer.Priority,
            hitsoundLayer.SampleSet, hitsoundLayer.Hitsound) { }

        public Sample(ISampleGenerator sampleGenerator, int priority, SampleSet sampleSet, Hitsound hitsound) {
            SampleGenerator = sampleGenerator;
            Priority = priority;
            SampleSet = sampleSet;
            Hitsound = hitsound;
        }

        public object Clone() {
            return new Sample(SampleGenerator?.Clone() as ISampleGenerator, Priority, SampleSet, Hitsound);
        }

        public ISampleGenerator SampleGenerator { get; set; }
        public int Priority { get; set; }
        public double OutsideVolume { get; set; }
        public SampleSet SampleSet { get; set; }
        public Hitsound Hitsound { get; set; }

        public bool Equals(ISample other) {
            if (!(other is Sample sample)) return false;
            if (SampleGenerator == null && sample.SampleGenerator != null ||
                SampleGenerator != null && sample.SampleGenerator == null) return false;
            return (SampleGenerator == null || SampleGenerator.Equals(sample.SampleGenerator)) && 
                   Priority == sample.Priority && 
                   OutsideVolume.Equals(sample.OutsideVolume) && 
                   SampleSet == sample.SampleSet && 
                   Hitsound == sample.Hitsound;
        }
    }
}