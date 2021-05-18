using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Represents a hitsound by a single circle in the editor
    /// </summary>
    public class HitsoundEvent {
        public double Time;
        public Vector2 Pos;
        public double Volume;
        public string Filename;
        public SampleSet SampleSet;
        public SampleSet Additions;
        public int CustomIndex;
        public bool Whistle;
        public bool Finish;
        public bool Clap;

        public HitsoundEvent(double time, double volume, SampleSet sampleSet, SampleSet additions, int customIndex, bool whistle, bool finish, bool clap) : this(
            time, new Vector2(256, 192), volume, string.Empty, sampleSet, additions, customIndex, whistle, finish, clap) { }

        public HitsoundEvent(double time, Vector2 pos, double volume, string filename, SampleSet sampleSet, SampleSet additions, int customIndex, bool whistle, bool finish, bool clap) {
            Time = time;
            Pos = pos;
            Volume = volume;
            Filename = filename;
            SampleSet = sampleSet;
            Additions = additions;
            CustomIndex = customIndex;
            Whistle = whistle;
            Finish = finish;
            Clap = clap;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new bool[] { false, Whistle, Finish, Clap }));
        }
    }
}
