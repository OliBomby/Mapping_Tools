using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class Sample {
        public int SampleSet { get; set; }
        public int Hitsound { get; set; }
        public string SamplePath { get; set; }
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
            SamplePath = "";
            Priority = 0;
        }

        public Sample(int sampleSet, int hitsound, string samplePath, int priority) {
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
        }

        public Sample(HitsoundLayer hl) {
            SampleSet = hl.SampleSet;
            Hitsound = hl.Hitsound;
            SamplePath = hl.SamplePath;
            Priority = hl.Priority;
        }
    }
}
