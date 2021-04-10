using System;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.BeatmapHelper {
    public class HitSampleInfo : IEquatable<HitSampleInfo> {
        /// <summary>
        /// The sampleset to load the normal sample from.
        /// </summary>
        public SampleSet SampleSet { get; set; }

        /// <summary>
        /// The sampleset to load the addition samples from.
        /// </summary>
        public SampleSet AdditionSet { get; set; }

        /// <summary>
        /// Whether to play the hitnormal sample.
        /// </summary>
        public bool Normal { get; set; }

        /// <summary>
        /// Whether to play the hitwhistle sample.
        /// </summary>
        public bool Whistle { get; set; }

        /// <summary>
        /// Whether to play the hitfinish sample.
        /// </summary>
        public bool Finish { get; set; }

        /// <summary>
        /// Whether to play the hitclap sample.
        /// </summary>
        public bool Clap { get; set; }

        /// <summary>
        /// An optional suffix to provide priority lookup. Falls back to non-suffixed <see cref="Hitsound"/>.
        /// </summary>
        public int CustomIndex { get; set; }

        /// <summary>
        /// An optional filename to provide priority lookup. Falls back to <see cref="Hitsound"/>.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The sample volume.
        /// </summary>
        public double Volume { get; set; }

        public HitSampleInfo() {
            SampleSet = SampleSet.Auto;
            AdditionSet = SampleSet.Auto;
        }

        public HitSampleInfo Clone() => (HitSampleInfo)MemberwiseClone();

        public bool Equals(HitSampleInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SampleSet == other.SampleSet && 
                   AdditionSet == other.AdditionSet && 
                   Normal == other.Normal && 
                   Whistle == other.Whistle && 
                   Finish == other.Finish && 
                   Clap == other.Clap && 
                   CustomIndex == other.CustomIndex && 
                   Filename == other.Filename && 
                   Volume == other.Volume;
        }
    }
}