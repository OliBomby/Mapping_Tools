namespace Mapping_Tools.Classes.HitsoundStuff {
    public class Sample {
        public SampleSet SampleSet { get; set; }
        public Hitsound Hitsound { get; set; }
        public SampleGeneratingArgs SampleArgs { get; set; }
        public int Priority { get; set; }

        public int SampleSetComboBoxIndex { get => GetSampleSetComboBoxIndex(); set => SetSampleSetComboBoxIndex(value); }

        private void SetSampleSetComboBoxIndex(int value) {
            SampleSet = (SampleSet)value + 1;
        }

        private int GetSampleSetComboBoxIndex() {
            return (int)SampleSet - 1;
        }

        public Sample() {
            SampleSet = 0;
            Hitsound = 0;
            SampleArgs = new SampleGeneratingArgs();
            Priority = 0;
        }

        public Sample(SampleSet sampleSet, Hitsound hitsound, SampleGeneratingArgs samplePath, int priority) {
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SampleArgs = samplePath;
            Priority = priority;
        }

        public Sample(HitsoundLayer hl) {
            SampleSet = hl.SampleSet;
            Hitsound = hl.Hitsound;
            SampleArgs = hl.SampleArgs;
            Priority = hl.Priority;
        }
    }
}
