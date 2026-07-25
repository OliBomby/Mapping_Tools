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
        /// <summary>
        /// The playback time in milliseconds.
        /// </summary>
        public double Time;
        /// <summary>
        /// The source object's position in osu! playfield coordinates.
        /// </summary>
        public Vector2 Pos;
        /// <summary>
        /// The resolved sample volume percentage.
        /// </summary>
        public double Volume;
        /// <summary>
        /// An optional explicit custom sample filename.
        /// </summary>
        public string Filename;
        /// <summary>
        /// The resolved normal-layer sample family.
        /// </summary>
        public SampleSet SampleSet;
        /// <summary>
        /// The resolved whistle/finish/clap sample family.
        /// </summary>
        public SampleSet Additions;
        /// <summary>
        /// The resolved custom sample index.
        /// </summary>
        public int CustomIndex;
        /// <summary>
        /// Whether the whistle addition plays.
        /// </summary>
        public bool Whistle;
        /// <summary>
        /// Whether the finish addition plays.
        /// </summary>
        public bool Finish;
        /// <summary>
        /// Whether the clap addition plays.
        /// </summary>
        public bool Clap;

        /// <summary>
        /// Creates a centered event without an explicit filename.
        /// </summary>
        /// <param name="time">The playback time in milliseconds.</param>
        /// <param name="volume">The volume.</param>
        /// <param name="sampleSet">The sample set.</param>
        /// <param name="additions">The additions.</param>
        /// <param name="customIndex">The custom index.</param>
        /// <param name="whistle">The whistle.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="clap">The clap.</param>
        public HitsoundEvent(double time, double volume, SampleSet sampleSet, SampleSet additions, int customIndex, bool whistle, bool finish, bool clap) : this(
            time, new Vector2(256, 192), volume, string.Empty, sampleSet, additions, customIndex, whistle, finish, clap) { }

        /// <summary>
        /// Creates a fully resolved hitsound event for editor or export processing.
        /// </summary>
        /// <param name="time">The playback time in milliseconds.</param>
        /// <param name="pos">The pos.</param>
        /// <param name="volume">The volume.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="sampleSet">The sample set.</param>
        /// <param name="additions">The additions.</param>
        /// <param name="customIndex">The custom index.</param>
        /// <param name="whistle">The whistle.</param>
        /// <param name="finish">The finish.</param>
        /// <param name="clap">The clap.</param>
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
        /// Packs the whistle, finish, and clap flags into the osu! hitsound integer.
        /// </summary>
        /// <returns></returns>
        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new bool[] { false, Whistle, Finish, Clap }));
        }
    }
}
