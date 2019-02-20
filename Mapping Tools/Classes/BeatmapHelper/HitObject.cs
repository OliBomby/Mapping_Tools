using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mapping_Tools.classes.BeatmapHelper {
    public class HitObject {
        public string Line { get => GetLine(); set => SetLine(value); }

        public string[] Values { get => GetValues(); set => SetValues(value); }

        public Poi Pos { get; set; }

        public double Time { get; set; }

        public int ObjectType { get => GetObjectType(); set => SetObjectType(value); }
        public bool IsCircle { get; set; }
        public bool IsSlider { get; set; }
        public bool NewCombo { get; set; }
        public bool IsSpinner { get; set; }
        public int ComboSkip { get; set; }
        public bool IsHoldNote { get; set; }

        public int Hitsounds { get => GetHitsounds(); set => SetHitsounds(value); }
        public bool Normal { get; set; }
        public bool Whistle { get; set; }
        public bool Finish { get; set; }
        public bool Clap { get; set; }

        public string Extras { get => GetExtras(); set => SetExtras(value); }
        public int SampleSet { get; set; }
        public int AdditionSet { get; set; }
        public int CustomIndex { get; set; }
        public double SampleVolume { get; set; }
        public string Filename { get; set; }

        public string SliderType { get; set; }
        public Poi[] CurvePoints { get; set; }
        public int Repeat { get; set; }
        public double PixelLength { get; set; }
        public int[] EdgeHitsounds { get; set; }
        public int[] EdgeSampleSets { get; set; }
        public int[] EdgeAdditionSets { get; set; }

        public bool SliderExtras { get; set; }

        public double TemporalLength { get; set; }
        public double EndTime { get; set; }

        // Special combined with greenline
        public double SV { get; set; }
        public TimingPoint TP { get; set; }
        public TimingPoint HitsoundTP { get; set; }
        public TimingPoint Redline { get; set; }
        public List<TimingPoint> BodyHitsounds { get; set; }

        // Special combined with timeline
        public List<TimelineObject> TimelineObjects { get; set; }

        public HitObject(string line) {
            // Example lines:
            // 74,183,57308,2,0,B|70:236,1,53.9999983520508,4|0,0:3|0:0,0:0:0:0:
            // 295,347,57458,5,2,0:0:0:0:
            // Mania:
            // 128,192,78,1,0,0:0:0:0:
            // 213,192,78,128,0,378:0:0:0:0:

            Line = line;
        }

        public void MoveTime(double deltaTime) {
            Time += deltaTime;
            EndTime += deltaTime;
            // Move its sliderbodyhitsounds
            foreach (TimingPoint tp in BodyHitsounds) {
                tp.Offset += deltaTime;
            }
            // Move its timelineobjects
            foreach (TimelineObject tlo in TimelineObjects) {
                tlo.Time += deltaTime;
            }
        }

        public bool ResnapSelf(Timing timing, int snap1, int snap2) {
            double newTime = Math.Floor(timing.Resnap(Time, snap1, snap2));
            double deltaTime = newTime - Time;
            MoveTime(deltaTime);
            return deltaTime != 0;
        }

        public bool ResnapEnd(Timing timing, int snap1, int snap2) {
            double newTime = timing.Resnap(EndTime, snap1, snap2);
            double deltaTime = newTime - EndTime;
            if (IsSlider) {
                double deltaLength = (-10000 * timing.SliderMultiplier * deltaTime / Repeat) / (Redline.MpB * SV);  // Divide by repeats because the endtime is multiplied by repeats
                PixelLength += deltaLength; // Change the pixellength to match the new time
            }

            // Change
            TemporalLength += deltaTime;
            TimelineObjects.Last().Time = Math.Floor(Time + TemporalLength);
            EndTime = Math.Floor(Time + TemporalLength);
            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime);
            return deltaTime != 0;
        }

        public void SetLine(string line) {
            Values = line.Split(',');
        }

        public string GetLine() {
            return string.Join(",", Values);
        }

        public void SetValues(string[] values) {
            Pos = new Poi(ParseDouble(values[0]), ParseDouble(values[1]));
            Time = ParseDouble(values[2]);
            ObjectType = int.Parse(values[3]);
            Hitsounds = int.Parse(values[4]);

            SliderExtras = IsSlider && values.Count() > 8; // Sliders remove extras and edges stuff if there are no hitsounds
            if (IsSlider) {
                string[] sliderData = values[5].Split('|');
                SliderType = GetLastLetter(sliderData);
                List<Poi> points = new List<Poi>();
                for (int i = 1; i < sliderData.Length; i++) {
                    string[] spl = sliderData[i].Split(':');
                    if (spl.Length == 2) // It has to have 2 coordinates inside
                    {
                        points.Add(new Poi(ParseDouble(spl[0]), ParseDouble(spl[1])));
                    }
                }
                CurvePoints = points.ToArray();

                Repeat = int.Parse(values[6]);
                PixelLength = ParseDouble(values[7]);
                if (SliderExtras) {
                    EdgeHitsounds = values[8].Split('|').Select(p => int.Parse(p)).ToArray();
                    EdgeSampleSets = values[9].Split('|').Select(p => int.Parse(p.Split(':')[0])).ToArray();
                    EdgeAdditionSets = values[9].Split('|').Select(p => int.Parse(p.Split(':')[1])).ToArray();

                    Extras = values[10];
                }
                else {
                    EdgeHitsounds = new int[Repeat + 1];
                    EdgeSampleSets = new int[Repeat + 1];
                    EdgeAdditionSets = new int[Repeat + 1];
                }
            }
            else if (IsSpinner) {
                EndTime = ParseDouble(values[5]);
                TemporalLength = EndTime - Time;
                Repeat = 1;
                Extras = values[6];
            }
            else {
                Repeat = 0;
                EndTime = Time;
                TemporalLength = 0;
                Extras = values[5];
            }
        }

        public string[] GetValues() {
            if (IsSlider) {
                string ret = "";
                foreach (Poi p in CurvePoints) {
                    ret += "|" + p.StringX();
                    ret += ":" + p.StringY();
                }
                string sliderShapeString = SliderType + ret;

                if (SliderExtras) {
                    string edgeHS = string.Join("|", EdgeHitsounds.Select(p => p.ToString()).ToArray());

                    string rett = "";
                    for (int i = 0; i < EdgeSampleSets.Count(); i++) {
                        rett += "|" + EdgeSampleSets[i];
                        rett += ":" + EdgeAdditionSets[i];
                    }
                    string edgeAd = rett.Substring(1);

                    return new string[] { Pos.StringX(), Pos.StringY(), Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(),
                                        sliderShapeString, Repeat.ToString(), PixelLength.ToString(CultureInfo.InvariantCulture), edgeHS, edgeAd, Extras };
                }
                else {
                    return new string[] { Pos.StringX(), Pos.StringY(), Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(),
                                        sliderShapeString, Repeat.ToString(), PixelLength.ToString(CultureInfo.InvariantCulture) };
                }
            }
            else if (IsSpinner) {
                return new string[] { Pos.StringX(), Pos.StringY(), Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(), Math.Round(EndTime).ToString(), Extras };
            }
            else {
                return new string[] { Pos.StringX(), Pos.StringY(), Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(), Extras };
            }
        }

        public int GetObjectType() {
            BitArray cs = new BitArray(new int[] { ComboSkip });
            return GetIntFromBitArray(new BitArray(new bool[] { IsCircle, IsSlider, NewCombo, IsSpinner, cs[0], cs[1], cs[2], IsHoldNote }));
        }

        public void SetObjectType(int type) {
            BitArray b = new BitArray(new int[] { type });
            IsCircle = b[0];
            IsSlider = b[1];
            NewCombo = b[2];
            IsSpinner = b[3];
            ComboSkip = GetIntFromBitArray(new BitArray(new bool[] { b[4], b[5], b[6] }));
            IsHoldNote = b[7];
        }

        public int GetHitsounds() {
            return GetIntFromBitArray(new BitArray(new bool[] { Normal, Whistle, Finish, Clap }));
        }

        public void SetHitsounds(int hitsounds) {
            BitArray b = new BitArray(new int[] { hitsounds });
            Normal = b[0];
            Whistle = b[1];
            Finish = b[2];
            Clap = b[3];
        }

        public string GetExtras() {
            if (IsHoldNote) {
                return string.Join(":", new string[] { Math.Round(EndTime).ToString(), SampleSet.ToString(), AdditionSet.ToString(),
                                                        CustomIndex.ToString(), SampleVolume.ToString(), Filename });
            }
            else {
                return string.Join(":", new string[] { SampleSet.ToString(), AdditionSet.ToString(),
                                                        CustomIndex.ToString(), SampleVolume.ToString(), Filename });
            }
        }

        public void SetExtras(string extras) {
            string[] split = extras.Split(':');
            int i = 0;
            if (IsHoldNote) {
                EndTime = ParseDouble(split[i]);
                TemporalLength = EndTime - Time;
                Repeat = 1;
                i += 1;
            }
            SampleSet = int.Parse(split[i]);
            AdditionSet = int.Parse(split[i + 1]);
            CustomIndex = int.Parse(split[i + 2]);
            SampleVolume = double.Parse(split[i + 3]);
            Filename = split[i + 4];
        }

        private int GetIntFromBitArray(BitArray bitArray) {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }

        private double ParseDouble(string d) {
            return double.Parse(d, CultureInfo.InvariantCulture);
        }

        private string GetLastLetter(string[] sliderData) {
            for (int i = sliderData.Length - 1; i >= 0; i--) {
                if (Char.IsLetter(sliderData[i], 0)) {
                    return sliderData[i][0].ToString(); // Return first letter
                }
            }
            return "B";
        }
    }
}
