using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.ExternalFileUtil
{
    public class TempoSignature
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

        // TODO: Metronome pattern.
        // TODO: 2863311530 2863311417 = Start > Beat > Triplet > Skip

    }
}
