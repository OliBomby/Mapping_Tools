using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public struct HitsoundLayer {
        public List<double> Times;
        public int SampleSet;
        public int Hitsound;
        public string SamplePath;
        public int Priority;

        public HitsoundLayer(int sampleSet, int hitsound, string samplePath, int priority) {
            Times = new List<double>();
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
        }

        public HitsoundLayer(List<double> times, int sampleSet, int hitsound, string samplePath, int priority) {
            Times = times;
            SampleSet = sampleSet;
            Hitsound = hitsound;
            SamplePath = samplePath;
            Priority = priority;
        }
    }
}
