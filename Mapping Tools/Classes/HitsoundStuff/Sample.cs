using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class Sample {
        public int SampleSet { get; set; }
        public int Hitsound { get; set; }
        public SampleGeneratingArgs SampleArgs { get; set; }
        public int Priority { get; set; }

        public int SampleSetComboBoxIndex { get => GetSampleSetComboBoxIndex(); set => SetSampleSetComboBoxIndex(value); }

        private void SetSampleSetComboBoxIndex(int value) {
            SampleSet = value + 1;
        }

        private int GetSampleSetComboBoxIndex() {
            return SampleSet - 1;
        }

        public Sample() {
            SampleSet = 0;
            Hitsound = 0;
            SampleArgs = new SampleGeneratingArgs();
            Priority = 0;
        }

        public Sample(int sampleSet, int hitsound, SampleGeneratingArgs samplePath, int priority) {
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
