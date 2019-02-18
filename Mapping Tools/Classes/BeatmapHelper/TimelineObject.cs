using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.classes.BeatmapHelper {
    class TimelineObject {
        public HitObject Origin { get; set; }
        public double Time { get; set; }
        public int Repeat { get; set; }

        public bool IsCircle { get; set; } = false;
        public bool IsSliderHead { get; set; } = false;
        public bool IsSliderRepeat { get; set; } = false;
        public bool IsSliderEnd { get; set; } = false;
        public bool IsSpinnerHead { get; set; } = false;
        public bool IsSpinnerEnd { get; set; } = false;
        public bool IsHoldnoteHead { get; set; } = false;
        public bool IsHoldnoteEnd { get; set; } = false;

        public int SampleSet { get; set; }
        public int AdditionSet { get; set; }
        public bool Normal { get; set; }
        public bool Whistle { get; set; }
        public bool Finish { get; set; }
        public bool Clap { get; set; }

        public bool HasHitsound { get; set; }

        public int CustomIndex { get; set; }
        public double SampleVolume { get; set; }
        public string Filename { get; set; }

        // Special combined with greenline
        public int FenoSampleSet { get; set; }
        public int FenoAdditionSet { get; set; }
        public int FenoCustomIndex { get; set; }
        public double FenoSampleVolume { get; set; }

        public TimelineObject(HitObject origin, double time, int objectType, int repeat, int hitsounds, int sampleset, int additionset) {
            Origin = origin;
            Time = time;

            BitArray b = new BitArray(new int[] { hitsounds });
            Normal = b[0];
            Whistle = b[1];
            Finish = b[2];
            Clap = b[3];

            SampleSet = sampleset;
            AdditionSet = additionset;

            BitArray c = new BitArray(new int[] { objectType });
            IsCircle = c[0];
            bool isSlider = c[1];
            bool isSpinner = c[3];
            bool isHoldNote = c[7];

            if( repeat == 0 ) {
                IsSliderHead = isSlider;
                IsSpinnerHead = isSpinner;
                IsHoldnoteHead = isHoldNote;

                if( IsCircle || isHoldNote ) // Can have custom index/volume/filename
                {
                    CustomIndex = origin.CustomIndex;
                    SampleVolume = origin.SampleVolume;
                    Filename = origin.Filename;
                }
            }
            else if( repeat == origin.Repeat ) {
                IsSliderEnd = isSlider;
                IsSpinnerEnd = isSpinner;
                IsHoldnoteEnd = isHoldNote;
            }
            else {
                IsSliderRepeat = isSlider;
            }
            HasHitsound = IsCircle || IsSliderHead || IsHoldnoteHead || IsSliderEnd || IsSpinnerEnd || IsSliderRepeat;

            Repeat = repeat;
        }

        public int GetHitsounds() {
            return GetIntFromBitArray(new BitArray(new bool[] { Normal, Whistle, Finish, Clap }));
        }

        private int GetIntFromBitArray(BitArray bitArray) {
            if( bitArray.Length > 32 )
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }
    }
}
