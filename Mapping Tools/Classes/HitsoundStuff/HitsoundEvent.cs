using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public struct HitsoundEvent {
        public double Time;
        public SampleSet SampleSet;
        public SampleSet Additions;
        public int CustomIndex;
        public bool Whistle;
        public bool Finish;
        public bool Clap;

        public HitsoundEvent(double time, SampleSet sampleSet, SampleSet additions, int customIndex, bool whistle, bool finish, bool clap) {
            Time = time;
            SampleSet = sampleSet;
            Additions = additions;
            CustomIndex = customIndex;
            Whistle = whistle;
            Finish = finish;
            Clap = clap;
        }

        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new bool[] { false, Whistle, Finish, Clap }));
        }

        public void SetTime(double time) {
            Time = time;
        }
    }
}
