using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.ExternalFileUtil.Reaper
{
    public class EnvelopeTempoPoint
    {
        /// <summary>
        /// The time spesified in seconds to the precision of 12 decimal places.
        /// </summary>
        private decimal time;

        /// <summary>
        /// The time position of the envelope tempo point spesified in seconds to the precision of 12 decimal places.
        /// </summary>
        public decimal Time
        {
            get { return time; }
            set { time = value; }
        }

        private decimal bpm;

        /// <summary>
        /// The beats per minute value of the envelope tempo point with a precision of 10 decimal places.
        /// </summary>
        public decimal BPM
        {
            get { return bpm; }
            set { bpm = value; }
        }

        private EnvelopeShape envelopeShape;

        public EnvelopeShape EnvelopeShape
        {
            get { return envelopeShape; }
            set { envelopeShape = value; }
        }

       

        //1114124 should be 12/17
        //524296 should be 8/8
        //1638425 should be 25/25
        //1638417 should be 17/25

        private TempoSignature tempoSignature;

        public TempoSignature TempoSignature
        {
            get { return tempoSignature; }
            set { tempoSignature = value; }
        }





    }
}
