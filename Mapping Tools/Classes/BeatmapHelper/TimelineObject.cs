using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// 
    /// </summary>
    public class TimelineObject {
        /// <summary>
        /// 
        /// </summary>
        public HitObject Origin { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double Time { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int Repeat { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ObjectType { get; set; }
        private BitArray TypeArray => new BitArray(new[] { ObjectType });

        /// <summary>
        /// 
        /// </summary>
        public bool IsCircle => TypeArray[0];

        /// <summary>
        /// 
        /// </summary>
        public bool IsSlider => TypeArray[1];
        /// <summary>
        /// 
        /// </summary>
        public bool IsSpinner => TypeArray[3];
        public bool IsHoldNote => TypeArray[7];
        public bool IsSliderHead => IsSlider && Repeat == 0;
        public bool IsSliderRepeat => IsSlider && Repeat != 0 && Repeat != Origin.Repeat;
        public bool IsSliderEnd => IsSlider && Repeat == Origin.Repeat;
        public bool IsSpinnerHead => IsSpinner && Repeat == 0;
        public bool IsSpinnerEnd => IsSpinner && Repeat == 1;
        public bool IsHoldnoteHead => IsHoldNote && Repeat == 0;
        public bool IsHoldnoteEnd => IsHoldNote && Repeat == 1;

        /// <summary>
        /// 
        /// </summary>
        public SampleSet SampleSet { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SampleSet AdditionSet { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Normal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Whistle { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Finish { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool Clap { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool HasHitsound =>
            IsCircle || IsSliderHead || IsHoldnoteHead || IsSliderEnd || IsSpinnerEnd || IsSliderRepeat;

        /// <summary>
        /// 
        /// </summary>
        public bool UsesFilename => !string.IsNullOrEmpty(Filename) && (IsCircle || IsHoldnoteHead);

        /// <summary>
        /// 
        /// </summary>
        public bool CanCustoms => IsCircle || IsHoldnoteHead;

        /// <summary>
        /// 
        /// </summary>
        public int CustomIndex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double SampleVolume { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Filename { get; set; }

        // Special combined with greenline
        /// <summary>
        /// 
        /// </summary>
        public TimingPoint TimingPoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public TimingPoint HitsoundTimingPoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public TimingPoint UninheritedTimingPoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SampleSet FenoSampleSet { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SampleSet FenoAdditionSet { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int FenoCustomIndex { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double FenoSampleVolume { get; set; }

        // 
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Special for hitsound copier</remarks>
        public bool CanCopy = true;

        /// <summary>
        /// Generates a new <see cref="TimelineObject"/>.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="time"></param>
        /// <param name="objectType"></param>
        /// <param name="repeat"></param>
        /// <param name="hitsounds"></param>
        /// <param name="sampleset"></param>
        /// <param name="additionset"></param>
        public TimelineObject(HitObject origin, double time, int objectType, int repeat, int hitsounds, SampleSet sampleset, SampleSet additionset) {
            Origin = origin;
            Time = time;

            BitArray b = new BitArray(new[] { hitsounds });
            Normal = b[0];
            Whistle = b[1];
            Finish = b[2];
            Clap = b[3];

            SampleSet = sampleset;
            AdditionSet = additionset;

            ObjectType = objectType;

            Repeat = repeat;

            if ( IsCircle || IsHoldnoteHead ) // Can have custom index/volume/filename
            {
                CustomIndex = origin.CustomIndex;
                SampleVolume = origin.SampleVolume;
                Filename = origin.Filename;
            }
        }

        /// <summary>
        /// Grabs the hitsound from the <see cref="TimelineObject"/>
        /// </summary>
        /// <returns></returns>
        public Hitsound GetHitsound() {
            if (Normal) {
                return Hitsound.Normal;
            }
            if (Whistle) {
                return Hitsound.Whistle;
            }
            if (Finish) {
                return Hitsound.Finish;
            }
            if (Clap) {
                return Hitsound.Clap;
            }
            return Hitsound.Normal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new[] { Normal, Whistle, Finish, Clap }));
        }

        /// <summary>
        /// Sets the hitsound to the <see cref="TimelineObject"/>
        /// </summary>
        /// <param name="hitsound"></param>
        public void SetHitsound(Hitsound hitsound) {
            Normal = false;
            Whistle = false;
            Finish = false;
            Clap = false;
            switch (hitsound) {
                case Hitsound.Normal:
                    Normal = true;
                    return;
                case Hitsound.Whistle:
                    Whistle = true;
                    return;
                case Hitsound.Finish:
                    Finish = true;
                    return;
                case Hitsound.Clap:
                    Clap = true;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hitsound), hitsound, null);
            }
        }

        public void ResetHitsounds() {
            Normal = false;
            Whistle = false;
            Finish = false;
            Clap = false;
            SampleSet = SampleSet.Auto;
            AdditionSet = SampleSet.Auto;
        }

        /// <summary>
        /// Checks if the selected timeline object does play a normal
        /// (Only in modes other than Mania)
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool PlaysNormal(GameMode mode) {
            return mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public List<Tuple<SampleSet, Hitsound, int>> GetPlayingHitsounds(GameMode mode = GameMode.Standard) {
            List<Tuple<SampleSet, Hitsound, int>> samples = new List<Tuple<SampleSet, Hitsound, int>>();
            bool normal = mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);

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

        /// <summary>
        /// Grabs the playing filenames of the <see cref="TimelineObject"/>
        /// </summary>
        /// <param name="mode">The osu! <see cref="GameMode"/></param>
        /// <param name="includeDefaults"></param>
        /// <returns></returns>
        public List<string> GetPlayingFilenames(GameMode mode = GameMode.Standard, bool includeDefaults = true) {
            List<string> samples = new List<string>();
            bool normal = mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);
            bool useFilename = !string.IsNullOrEmpty(Filename) && (IsCircle || IsHoldnoteHead);

            if (useFilename) {
                samples.Add(Filename);
            } else if (includeDefaults || FenoCustomIndex != 0) {
                if (normal)
                    samples.Add(GetFileName(FenoSampleSet, Hitsound.Normal, FenoCustomIndex, mode));
                if (Whistle)
                    samples.Add(GetFileName(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex, mode));
                if (Finish)
                    samples.Add(GetFileName(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex, mode));
                if (Clap)
                    samples.Add(GetFileName(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex, mode));
            }

            return samples;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="mapDir"></param>
        /// <param name="firstSamples"></param>
        /// <param name="includeDefaults"></param>
        /// <returns></returns>
        public List<string> GetFirstPlayingFilenames(GameMode mode, string mapDir, Dictionary<string, string> firstSamples, bool includeDefaults=true) {
            List<string> samples = new List<string>();
            bool normal = mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);
            bool useFilename = !string.IsNullOrEmpty(Filename) && (IsCircle || IsHoldnoteHead);

            if (useFilename) {
                string samplePath = Path.Combine(mapDir, Filename);
                string fullPathExtLess = Path.Combine(
                    Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                    Path.GetFileNameWithoutExtension(samplePath));

                // Get the first occurence of this sound to not get duplicated
                if (firstSamples.Keys.Contains(fullPathExtLess)) {
                    samples.Add(Path.GetFileName(firstSamples[fullPathExtLess]));
                }
            } else if (includeDefaults || FenoCustomIndex != 0) {
                if (normal)
                    AddFirstIdenticalFilename(FenoSampleSet, Hitsound.Normal, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
                if (Whistle)
                    AddFirstIdenticalFilename(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
                if (Finish)
                    AddFirstIdenticalFilename(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
                if (Clap)
                    AddFirstIdenticalFilename(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
            }

            return samples;
        }

        private void AddFirstIdenticalFilename(SampleSet sampleSet, Hitsound hitsound, int index, List<string> samples, GameMode mode, bool useFilename, string mapDir, Dictionary<string, string> firstSamples, bool includeDefaults) {
            string filename = GetFileName(sampleSet, hitsound, index, mode);
            string samplePath = Path.Combine(mapDir, filename);
            string fullPathExtLess = Path.Combine(
                Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                Path.GetFileNameWithoutExtension(samplePath));

            // Get the first occurence of this sound to not get duplicated
            if (firstSamples.Keys.Contains(fullPathExtLess)) {
                if (!useFilename) {
                    samples.Add(Path.GetFileName(firstSamples[fullPathExtLess]));
                }
            } else {
                // Sample doesn't exist
                if (!useFilename && includeDefaults) {
                    samples.Add(GetFileName(sampleSet, hitsound, 0, mode));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
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
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hstp"></param>
        public void GiveHitsoundTimingPoint(TimingPoint hstp) {
            HitsoundTimingPoint = hstp;
            FenoSampleSet = SampleSet == 0 ? hstp.SampleSet : SampleSet;
            FenoAdditionSet = AdditionSet == 0 ? FenoSampleSet : AdditionSet;
            FenoCustomIndex = CustomIndex == 0 ? hstp.SampleIndex : CustomIndex;
            FenoSampleVolume = Math.Abs(SampleVolume) < Precision.DOUBLE_EPSILON ? hstp.Volume : SampleVolume;
        }

        /// <summary>
        /// Grabs the playing file name of the object.
        /// </summary>
        /// <param name="sampleSet"></param>
        /// <param name="hitsound"></param>
        /// <param name="index"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static string GetFileName(SampleSet sampleSet, Hitsound hitsound, int index, GameMode mode) {
            string taiko = mode == GameMode.Taiko ? "taiko-" : "";
            switch (index)
            {
                case 0:
                    return $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}-default";
                case 1:
                    return $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}";
                default:
                    return $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}{index}";
            }
        }

        public TimelineObject Copy() {
            return (TimelineObject) MemberwiseClone();
        }

        public override string ToString() {
            return $"{Time}, {ObjectType}, {Repeat}, {FenoSampleVolume}";
        }
    }
}
