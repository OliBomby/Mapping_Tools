using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.TimingStudio
{
    public abstract class BeatTime : IEquatable<BeatTime>
    {
        /// <summary>
        /// The measure in terms of the current bpm track.
        /// </summary>
        public int Measure { get; set; }

        /// <summary>
        /// The beat of the current measure.
        /// </summary>
        /// <remarks>
        /// Seperated using the <see cref="BeatmapHelper.TimingPoint.Meter"></see> property.
        /// </remarks>
        public int Beat { get; set; }

        /// <summary>
        /// The fraction of the current beat.
        /// </summary>
        public int Fraction { get; set; }

        /// <summary>
        /// The string represenation of the time.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.Measure}.{this.Beat}.{this.Fraction}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BeatTime);
        }

        public bool Equals(BeatTime other)
        {
            return other != null &&
                   Measure == other.Measure &&
                   Beat == other.Beat &&
                   Fraction == other.Fraction;
        }

        public override int GetHashCode()
        {
            var hashCode = 390680319;
            hashCode = hashCode * -1521134295 + Measure.GetHashCode();
            hashCode = hashCode * -1521134295 + Beat.GetHashCode();
            hashCode = hashCode * -1521134295 + Fraction.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(BeatTime time1, BeatTime time2)
        {
            return EqualityComparer<BeatTime>.Default.Equals(time1, time2);
        }

        public static bool operator !=(BeatTime time1, BeatTime time2)
        {
            return !(time1 == time2);
        }


    }
}