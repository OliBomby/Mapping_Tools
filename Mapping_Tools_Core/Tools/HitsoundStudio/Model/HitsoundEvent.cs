using System.Collections;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    /// <summary>
    /// Represents a hitsound by a single circle in the editor.
    /// </summary>
    public class HitsoundEvent : IHitsoundEvent {
        public double Time { get; }
        public Vector2 Pos { get; }
        public double Volume { get; }
        public string Filename { get; }
        public SampleSet SampleSet { get; }
        public SampleSet Additions { get; }
        public int CustomIndex { get; }
        public bool Whistle { get; }
        public bool Finish { get; }
        public bool Clap { get; }

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
            return MathHelper.GetIntFromBitArray(new BitArray(new[] { false, Whistle, Finish, Clap }));
        }
    }
}
