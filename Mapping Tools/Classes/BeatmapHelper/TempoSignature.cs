using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class TempoSignature : IEquatable<TempoSignature>
    {
        /// <summary>
        /// The bottom value of the time signature.
        /// </summary>
        public int TempoDenominator { get; set; }
        
        /// <summary>
        /// The top value of the time signature.
        /// </summary>
        public int TempoNumerator { get; set; }

        /// <summary>
        /// The constructor for a new Tempo Signature
        /// </summary>
        /// <param name="tempoDenominator">The Bottom vale of the signature.</param>
        /// <param name="tempoNumerator">The top value of the signature.</param>
        public TempoSignature(int tempoDenominator, int tempoNumerator)
        {
            TempoDenominator = tempoDenominator;
            TempoNumerator = tempoNumerator;
        }

        /// <summary>
        /// The constructor for a new Tempo Signature where the Denominator value is 4.
        /// </summary>
        /// <param name="tempoNumerator">The top value of the signature.</param>
        public TempoSignature(int tempoNumerator)
        {
            TempoNumerator = tempoNumerator;
            TempoDenominator = 4;
        }

        /// <summary>
        /// Allows a partial measure before the current marker.
        /// </summary>
        /// <remarks>
        /// The number 5 is specified as a partial measure allowance.
        /// </remarks>
        public bool PartialMeasure { get; set; }

        public bool Equals(TempoSignature other)
        {
            return other != null && 
                   TempoDenominator == other.TempoDenominator && 
                   TempoNumerator == other.TempoNumerator;
        }
        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TempoSignature) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = -175245820;
            hashCode = hashCode * -1521134295 + TempoDenominator.GetHashCode();
            hashCode = hashCode * -1521134295 + TempoNumerator.GetHashCode();
            hashCode = hashCode * -1521134295 + PartialMeasure.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(TempoSignature signature1, TempoSignature signature2)
        {
            return EqualityComparer<TempoSignature>.Default.Equals(signature1, signature2);
        }

        public static bool operator !=(TempoSignature signature1, TempoSignature signature2)
        {
            return !(signature1 == signature2);
        }

        // TODO: Metronome pattern.
        // TODO: 2863311530 2863311417 = Start > Beat > Triplet > Skip

    }
}
