namespace Mapping_Tools.Core.Classes.BeatmapHelper {
#nullable disable

    /// <summary>
    /// Describes the metrical numerator and denominator active at a timing point.
    /// </summary>
    public class TempoSignature : IEquatable<TempoSignature>
    {
        /// <summary>
        /// Creates the standard four-four signature used by an uninitialized timing point.
        /// </summary>
        public TempoSignature() : this(4, 4)
        {
        }

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

        /// <summary>
        /// Compares the numerator and denominator of two signatures.
        /// </summary>
        /// <param name="other">The signature to compare.</param>
        /// <returns><see langword="true"/> when both metrical components match.</returns>
        public bool Equals(TempoSignature other)
        {
            return other != null && 
                   TempoDenominator == other.TempoDenominator && 
                   TempoNumerator == other.TempoNumerator;
        }
        /// <summary>
        /// Determines whether an object is a signature with matching metrical components.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><see langword="true"/> for an equal <see cref="TempoSignature"/>.</returns>
        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TempoSignature) obj);
        }

        /// <summary>
        /// Combines the metrical components and partial-measure flag into a hash code.
        /// </summary>
        /// <returns>A hash code for this signature state.</returns>
        public override int GetHashCode()
        {
            var hashCode = -175245820;
            hashCode = hashCode * -1521134295 + TempoDenominator.GetHashCode();
            hashCode = hashCode * -1521134295 + TempoNumerator.GetHashCode();
            hashCode = hashCode * -1521134295 + PartialMeasure.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Applies the == operator.
        /// </summary>
        /// <param name="signature1">The left signature.</param>
        /// <param name="signature2">The right signature.</param>
        /// <returns><see langword="true"/> when both references are null or their metrical components match.</returns>
        public static bool operator ==(TempoSignature signature1, TempoSignature signature2)
        {
            return EqualityComparer<TempoSignature>.Default.Equals(signature1, signature2);
        }

        /// <summary>
        /// Applies the != operator.
        /// </summary>
        /// <param name="signature1">The left signature.</param>
        /// <param name="signature2">The right signature.</param>
        /// <returns><see langword="true"/> when the signatures are not equal.</returns>
        public static bool operator !=(TempoSignature signature1, TempoSignature signature2)
        {
            return !(signature1 == signature2);
        }

        // TODO: Metronome pattern.
        // TODO: 2863311530 2863311417 = Start > Beat > Triplet > Skip

    }
}
