using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class TimelineObject {
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

        public SampleSet SampleSet { get; set; }
        public SampleSet AdditionSet { get; set; }
        public bool Normal { get; set; }
        public bool Whistle { get; set; }
        public bool Finish { get; set; }
        public bool Clap { get; set; }

        public bool HasHitsound { get; set; }

        public int CustomIndex { get; set; }
        public double SampleVolume { get; set; }
        public string Filename { get; set; }

        // Special combined with greenline
        public TimingPoint TP { get; set; }
        public TimingPoint HitsoundTP { get; set; }
        public TimingPoint Redline { get; set; }
        public SampleSet FenoSampleSet { get; set; }
        public SampleSet FenoAdditionSet { get; set; }
        public int FenoCustomIndex { get; set; }
        public double FenoSampleVolume { get; set; }

        // Special for hitsound copier
        public bool canCopy = true;

        public TimelineObject(HitObject origin, double time, int objectType, int repeat, int hitsounds, SampleSet sampleset, SampleSet additionset) {
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
            return MathHelper.GetIntFromBitArray(new BitArray(new bool[] { Normal, Whistle, Finish, Clap }));
        }

        public List<Tuple<SampleSet, Hitsound, int>> GetPlayingHitsounds(int mode = 0) {
            List<Tuple<SampleSet, Hitsound, int>> samples = new List<Tuple<SampleSet, Hitsound, int>>();
            bool normal = mode != 3 || Normal || !(Whistle || Finish || Clap);

            if (normal)
                samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoSampleSet, Hitsound.Normal, FenoCustomIndex));
            if (Whistle)
                samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex));
            if (Finish)
                samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex));
            if (Clap)
                samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex));

            return samples;
        }

        public List<string> GetPlayingFilenames(int mode = 0) {
            List<string> samples = new List<string>();
            bool normal = mode != 3 || Normal || !(Whistle || Finish || Clap);
            bool useFilename = Filename != null && Filename != "" && (IsCircle || IsHoldnoteHead);

            if (normal)
                samples.Add(GetFileName(FenoSampleSet, Hitsound.Normal, FenoCustomIndex));
            if (Whistle)
                samples.Add(GetFileName(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex));
            if (Finish)
                samples.Add(GetFileName(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex));
            if (Clap)
                samples.Add(GetFileName(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex));

            return useFilename ? new List<string>() { Filename } : samples;
        }

        public void HitsoundsToOrigin() {
            if (Origin.IsCircle || (Origin.IsSpinner && Repeat == 1) || (Origin.IsHoldNote && Repeat == 0)) {
                Origin.Hitsounds = GetHitsounds();
                Origin.SampleSet = SampleSet;
                Origin.AdditionSet = AdditionSet;
                Origin.CustomIndex = CustomIndex;
                Origin.SampleVolume = SampleVolume;
                Origin.Filename = Filename;
            } else if (Origin.IsSlider) {
                Origin.EdgeHitsounds[Repeat] = GetHitsounds();
                Origin.EdgeSampleSets[Repeat] = SampleSet;
                Origin.EdgeAdditionSets[Repeat] = AdditionSet;
                Origin.SliderExtras = true;
            }
        }

        public static string GetFileName(SampleSet sampleSet, Hitsound hitsound, int index) {
            if (index == 1) {
                return String.Format("{0}-hit{1}.wav", sampleSet.ToString().ToLower(), hitsound.ToString().ToLower());
            }
            return String.Format("{0}-hit{1}{2}.wav", sampleSet.ToString().ToLower(), hitsound.ToString().ToLower(), index);
        }
    }
}
