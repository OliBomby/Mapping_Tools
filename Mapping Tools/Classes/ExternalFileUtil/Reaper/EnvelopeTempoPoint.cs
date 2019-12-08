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

        /// <summary>
        /// PT 0.000000000000 72.0000000000 1
        /// 
        /// </summary>
        /// <param name="line"></param>
        public EnvelopeTempoPoint(string line)
        {
            string[] timingInformation = line.Split(char.Parse(""));
            time = decimal.Parse(timingInformation[1]);
            bpm = decimal.Parse(timingInformation[2]);
            envelopeShape = (EnvelopeShape)int.Parse(timingInformation[3]);
            tempoSignature = timingInformation[4] != null ? GetTempoSignature(timingInformation[4]) : new TempoSignature(4);
        }

        private TempoSignature GetTempoSignature(string signatureValue)
        {
            numeratorIncrementalValue = PopulateDictionary();
            int numeratorValue = int.Parse(signatureValue.Substring(signatureValue.Length - 3));
            int denominatorValue = int.Parse(signatureValue.Substring(0, signatureValue.Length - 3));

            // Denominator value always increases by 65
            int nearestDenominatorValue = numeratorIncrementalValue.First(t => t.Key == denominatorValue).Key;
            // We need to get the difference, divide that by 65 and add that to the index number of the found key.
            int denominator = ((denominatorValue - nearestDenominatorValue) / 65) 
                + numeratorIncrementalValue.ToList().FindIndex(t => t.Key == denominatorValue);

            int numerator = (numeratorIncrementalValue.First(t => t.Key == denominatorValue).Value - numeratorValue) + 1;
            return new TempoSignature(denominator, numerator);
        }


        private Dictionary<int, int> numeratorIncrementalValue;

        private Dictionary<int, int> PopulateDictionary()
        {
            Dictionary<int, int> keyValues = new Dictionary<int, int>();
            int evenNumber = 73;
            int oddNumber = 537;

            // There are only 255 avaliable denominators in reaper.
            for (int i = 1; i <= 255; i++)
            {
                int denominatorValue = (65 * i) + (int)Math.Round((decimal) i / 2);
                keyValues.Add(denominatorValue, i % 2 == 0 ? evenNumber : oddNumber);
                Console.WriteLine($"{denominatorValue} : {(i % 2 == 0 ? evenNumber : oddNumber)}");
                switch (i % 2)
                {
                    case 0:
                        evenNumber += 72;
                        break;
                    default:
                        oddNumber += 72;
                        break;
                }
                evenNumber = evenNumber >= 1000 ? evenNumber -= 1000 : evenNumber;
                oddNumber = oddNumber >= 1000 ? oddNumber -= 1000 : oddNumber;
            }
            return keyValues;
        }

    }
}
