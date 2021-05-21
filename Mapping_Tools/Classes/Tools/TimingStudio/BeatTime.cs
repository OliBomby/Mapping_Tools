using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools.TimingStudio
{
    public class BeatTime : IEquatable<BeatTime>
    {
        /// <summary>
        /// The measure in terms of the current bpm track.
        /// </summary>
        public int Measure { get; set; }

        /// <summary>
        /// The beat of the current measure.
        /// </summary>
        public int Beat { get; set; }

        /// <summary>
        /// The fraction of the current beat.
        /// </summary>
        public int Fraction { get; set; }

        /// <summary>
        /// The constuctor of the BeatTime object.
        /// </summary>
        /// <param name="measure"></param>
        /// <param name="beat"></param>
        /// <param name="fraction"></param>
        public BeatTime(int measure, int beat, int fraction)
        {
            Measure = measure;
            Beat = beat;
            Fraction = fraction;
        }

        /// <summary>
        /// The constuctor of the BeatTime object.
        /// </summary>
        public BeatTime()
        {
            Measure = 0;
            Beat = 0;
            Fraction = 0;
        }

        /// <summary>
        /// The string represenation of the time.
        /// </summary>
        /// <returns>The format of 0.0.00 (Measure.Beat.Fraction)</returns>
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