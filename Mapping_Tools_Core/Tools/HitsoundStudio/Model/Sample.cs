using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class Sample : ISample {
        public Sample(IHitsoundLayer hitsoundLayer) : this(hitsoundLayer.SampleGeneratingArgs, hitsoundLayer.Priority,
            hitsoundLayer.SampleSet, hitsoundLayer.Hitsound) { }

        public Sample(ISampleGeneratingArgs sampleGeneratingArgs, int priority, SampleSet sampleSet, Hitsound hitsound) {
            SampleGeneratingArgs = sampleGeneratingArgs;
            Priority = priority;
            SampleSet = sampleSet;
            Hitsound = hitsound;
        }

        public object Clone() {
            return new Sample((ISampleGeneratingArgs) SampleGeneratingArgs.Clone(), Priority, SampleSet, Hitsound);
        }

        public ISampleGeneratingArgs SampleGeneratingArgs { get; set; }
        public int Priority { get; set; }
        public double OutsideVolume { get; set; }
        public SampleSet SampleSet { get; set; }
        public Hitsound Hitsound { get; set; }
    }
}