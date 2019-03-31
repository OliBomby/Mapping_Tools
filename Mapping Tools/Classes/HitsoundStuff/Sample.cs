using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class Sample {
        public int SampleSet;
        public int Hitsound;
        public string SamplePath;
        public int Priority;

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
