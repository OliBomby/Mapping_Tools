using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public struct Sound {
        public int SampleSet;
        public int Hitsound;
        public string SamplePath;
        public int Priority;

        public Sound(int sampleSet, int hitsound, string samplePath, int priority) {
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
        }
    }
}
