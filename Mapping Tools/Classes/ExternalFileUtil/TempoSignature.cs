using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.ExternalFileUtil
{
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    public class TempoSignature : IEquatable<TempoSignature>
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    {
        private int _tempoDenominator;

        /// <summary>
        /// The bottom value of the time signature.
        /// </summary>
        public int TempoDenominator
        {
            get { return _tempoDenominator; }
            set { _tempoDenominator = value; }
        }

        private int _tempoNumerator;

        /// <summary>
        /// The top value of the time signature.
        /// </summary>
        public int TempoNumerator
        {
            get { return _tempoNumerator; }
            set { _tempoNumerator = value; }
        }

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

        private bool _partialMeasure;

        /// <summary>
        /// Allows a partial measure before the current marker.
        /// </summary>
        /// <remarks>
        /// The number 5 is spesified as a partial measurea allowance.
        /// </remarks>
        public bool PartialMeasure
        {
            get { return _partialMeasure; }
            set { _partialMeasure = value; }
        }

        public bool Equals(TempoSignature other)
        {
            return _tempoDenominator == other.TempoDenominator
                && _tempoNumerator == other._tempoNumerator;
        }

        public override int GetHashCode()
        {
            var hashCode = -175245820;
            hashCode = hashCode * -1521134295 + _tempoDenominator.GetHashCode();
            hashCode = hashCode * -1521134295 + _tempoNumerator.GetHashCode();
            hashCode = hashCode * -1521134295 + _partialMeasure.GetHashCode();
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
