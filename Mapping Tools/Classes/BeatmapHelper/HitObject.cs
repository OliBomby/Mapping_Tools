using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class HitObject {
        public string Line { get => GetLine(); set => SetLine(value); }

        public string[] Values { get => GetValues(); set => SetValues(value); }

        public Vector2 Pos { get; set; }

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
        public SampleSet SampleSet { get; set; }
        public SampleSet AdditionSet { get; set; }
        public int CustomIndex { get; set; }
        public double SampleVolume { get; set; }
        public string Filename { get; set; }

        public PathType SliderType { get; set; }
        public List<Vector2> CurvePoints { get; set; }
        public SliderPath SliderPath { get => GetSliderPath(); set => SetSliderPath(value); }
        public int Repeat { get; set; }
        public double PixelLength { get; set; }
        public int[] EdgeHitsounds { get; set; }
        public SampleSet[] EdgeSampleSets { get; set; }
        public SampleSet[] EdgeAdditionSets { get; set; }

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
            BodyHitsounds = new List<TimingPoint>();
            TimelineObjects = new List<TimelineObject>();
        }

        public HitObject(double time, int hitsounds, SampleSet sampleSet, SampleSet additions) {
            // Basic hitsoundind circle
            Pos = new Vector2(256, 192);
            Time = time;
            SetObjectType(5);
            SetHitsounds(hitsounds);
            SampleSet = sampleSet;
            AdditionSet = additions;
            CustomIndex = 0;
            SampleVolume = 0;
            Filename = "";
        }

        public List<string> GetPlayingBodyFilenames(double sliderTickRate, bool includeDefaults = true) {
            List<string> samples = new List<string>();
            if (IsSlider) {
                // Get sliderslide hitsounds for every timingpoint in the slider
                if (includeDefaults || TP.SampleIndex != 0) {
                    SampleSet firstSampleSet = SampleSet == SampleSet.Auto ? TP.SampleSet : SampleSet;
                    samples.Add(GetSliderFilename(firstSampleSet, "slide", TP.SampleIndex));
                    if (Whistle)
                        samples.Add(GetSliderFilename(firstSampleSet, "whistle", TP.SampleIndex));
                }

                foreach (TimingPoint bodyTP in BodyHitsounds) {
                    if (includeDefaults || bodyTP.SampleIndex != 0) {
                        SampleSet sampleSet = SampleSet == SampleSet.Auto ? bodyTP.SampleSet : SampleSet;
                        samples.Add(GetSliderFilename(sampleSet, "slide", bodyTP.SampleIndex));
                        if (Whistle)
                            samples.Add(GetSliderFilename(sampleSet, "whistle", bodyTP.SampleIndex));
                    }
                }

                // Add tick samples
                // 10 ms over tick time is tick
                double t = Time + Redline.MpB / sliderTickRate;
                while (t + 10 < EndTime) {
                    TimingPoint bodyTP = Timing.GetTimingPointAtTime(t, BodyHitsounds, TP);
                    if (includeDefaults || bodyTP.SampleIndex != 0) {
                        SampleSet sampleSet = SampleSet == SampleSet.Auto ? bodyTP.SampleSet : SampleSet;
                        samples.Add(GetSliderFilename(sampleSet, "tick", bodyTP.SampleIndex));
                    }
                    t += Redline.MpB / sliderTickRate;
                }
            }
            return samples;
        }

        private string GetSliderFilename(SampleSet sampleSet, string sampleName, int index) {
            if (index == 0) {
                return string.Format("{0}-slider{1}-default.wav", sampleSet.ToString().ToLower(), sampleName);
            }
            if (index == 1) {
                return string.Format("{0}-slider{1}.wav", sampleSet.ToString().ToLower(), sampleName);
            }
            return string.Format("{0}-slider{1}{2}.wav", sampleSet.ToString().ToLower(), sampleName, index);
        }

        public void MoveTime(double deltaTime) {
            Time += deltaTime;
            EndTime += deltaTime;

            // Move its timelineobjects
            foreach (TimelineObject tlo in TimelineObjects) {
                tlo.Time += deltaTime;
            }

            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime || s.Offset <= Time);
        }

        public void MoveEndTime(Timing timing, double deltaTime) {
            if (Repeat == 0) { return; }

            ChangeTemporalTime(timing, deltaTime / Repeat);
        }

        public void ChangeTemporalTime(Timing timing, double deltaTemporalTime) {
            if (Repeat == 0) { return; }

            if (IsSlider) {
                double deltaLength = (-10000 * timing.SliderMultiplier * deltaTemporalTime) / (Redline.MpB * SV);  // Divide by repeats because the endtime is multiplied by repeats
                PixelLength += deltaLength; // Change the pixellength to match the new time
            }

            // Change
            TemporalLength += deltaTemporalTime;
            EndTime = Math.Floor(Time + TemporalLength * Repeat);
            if (TimelineObjects.Count > 0) { TimelineObjects.Last().Time = EndTime; };
            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime);
        }

        public void Move(Vector2 delta) {
            Pos += delta;
            if (IsSlider) {
                for (int i = 0; i < CurvePoints.Count; i++) {
                    CurvePoints[i] = CurvePoints[i] + delta;
                }
            }
        }

        public bool ResnapSelf(Timing timing, int snap1, int snap2, bool floor=true, TimingPoint tp=null) {
            double newTime = GetResnappedTime(timing, snap1, snap2, floor, tp);
            double deltaTime = newTime - Time;
            MoveTime(deltaTime);
            return deltaTime != 0;
        }

        public bool ResnapEnd(Timing timing, int snap1, int snap2, bool floor = true, TimingPoint tp = null) {
            // If there is a redline in the sliderbody then the sliderend gets snapped to a tick of the latest redline
            if (!IsSlider || timing.TimingPoints.Any(o => o.Inherited && o.Offset <= EndTime + 20 && o.Offset > Time)) {
                return ResnapEndTime(timing, snap1, snap2, floor, tp);
            } else {
                return ResnapEndClassic(timing, snap1, snap2, tp);
            }
        }

        public bool ResnapEndTime(Timing timing, int snap1, int snap2, bool floor = true, TimingPoint tp = null) {
            double newTime = timing.Resnap(EndTime, snap1, snap2, floor, tp);
            double deltaTime = newTime - EndTime;
            MoveEndTime(timing, deltaTime);

            return deltaTime != 0;
        }

        public bool ResnapPosition(int mode, double circleSize) {
            if (mode == 3) {
                // Resnap X to the middle of the columns and Y to 192
                double dist = 512d / Math.Round(circleSize);
                double hdist = dist / 2;

                double dX = Math.Floor(Math.Round((Pos.X - hdist) / dist) * dist + hdist) - Pos.X;
                double dY = 192 - Pos.Y;
                Move(new Vector2(dX, dY));

                return dX != 0 || dY != 0;
            }
            return false;
        }

        public bool ResnapEndClassic(Timing timing, int snap1, int snap2, TimingPoint firstTP = null) {
            // Temporal length is n times a snap divisor length
            TimingPoint tp = timing.GetRedlineAtTime(Time, firstTP);

            double newTemporalLength1 = Timing.GetNearestMultiple(TemporalLength, tp.MpB / snap1);
            double snapDistance1 = Math.Abs(TemporalLength - newTemporalLength1);

            double newTemporalLength2 = Timing.GetNearestMultiple(TemporalLength, tp.MpB / snap2);
            double snapDistance2 = Math.Abs(TemporalLength - newTemporalLength2);

            double newTemporalLength = snapDistance1 < snapDistance2 ? newTemporalLength1 : newTemporalLength2;

            double deltaTime = newTemporalLength - TemporalLength;
            ChangeTemporalTime(timing, deltaTime);

            return deltaTime != 0;
        }

        public double GetResnappedTime(Timing timing, int snap1, int snap2, bool floor=true, TimingPoint tp=null) {
            return timing.Resnap(Time, snap1, snap2, floor, tp);
        }

        public void SetLine(string line) {
            Values = line.Split(',');
        }

        public string GetLine() {
            return string.Join(",", Values);
        }

        public void SetValues(string[] values) {
            Pos = new Vector2(ParseDouble(values[0]), ParseDouble(values[1]));
            Time = ParseDouble(values[2]);
            ObjectType = int.Parse(values[3]);
            Hitsounds = int.Parse(values[4]);

            SliderExtras = IsSlider && values.Count() > 8; // Sliders remove extras and edges stuff if there are no hitsounds
            if (IsSlider) {
                string[] sliderData = values[5].Split('|');
                SliderType = GetPathType(sliderData);
                List<Vector2> points = new List<Vector2>();
                for (int i = 1; i < sliderData.Length; i++) {
                    string[] spl = sliderData[i].Split(':');
                    if (spl.Length == 2) // It has to have 2 coordinates inside
                    {
                        points.Add(new Vector2(ParseDouble(spl[0]), ParseDouble(spl[1])));
                    }
                }
                CurvePoints = points;

                Repeat = int.Parse(values[6]);
                PixelLength = ParseDouble(values[7]);
                if (SliderExtras) {
                    EdgeHitsounds = values[8].Split('|').Select(p => int.Parse(p)).ToArray();
                    if (values.Length > 9) {
                        EdgeSampleSets = values[9].Split('|').Select(p => (SampleSet)int.Parse(p.Split(':')[0])).ToArray();
                        EdgeAdditionSets = values[9].Split('|').Select(p => (SampleSet)int.Parse(p.Split(':')[1])).ToArray();
                    } else {
                        EdgeSampleSets = new SampleSet[Repeat + 1];
                        EdgeAdditionSets = new SampleSet[Repeat + 1];
                        for (int i = 0; i < Repeat + 1; i++) {
                            EdgeSampleSets[i] = SampleSet.Auto;
                            EdgeAdditionSets[i] = SampleSet.Auto;
                        }
                    }

                    if (values.Length > 10)
                        Extras = values[10];
                }
                else {
                    EdgeHitsounds = new int[Repeat + 1];
                    EdgeSampleSets = new SampleSet[Repeat + 1];
                    EdgeAdditionSets = new SampleSet[Repeat + 1];
                }
            }
            else if (IsSpinner) {
                EndTime = ParseDouble(values[5]);
                TemporalLength = EndTime - Time;
                Repeat = 1;
                if (values.Length > 6)
                    Extras = values[6];
            }
            else {
                Repeat = 0;
                EndTime = Time;
                TemporalLength = 0;
                if (values.Length > 5)
                    Extras = values[5];
            }
        }

        public string[] GetValues() {
            if (IsSlider) {
                StringBuilder builder = new StringBuilder();
                builder.Append(GetPathTypeString());
                foreach (Vector2 p in CurvePoints) {
                    builder.Append($"|{p.StringX}:{p.StringY}");
                }
                string sliderShapeString = builder.ToString();

                if (SliderExtras) {
                    string edgeHS = string.Join("|", EdgeHitsounds.Select(p => p.ToString()).ToArray());

                    StringBuilder builder2 = new StringBuilder();
                    for (int i = 0; i < EdgeSampleSets.Count(); i++) {
                        builder2.Append($"|{(int)EdgeSampleSets[i]}:{(int)EdgeAdditionSets[i]}");
                    }
                    builder2.Remove(0, 1);
                    string edgeAd = builder2.ToString();

                    if (Extras == null)
                        return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(),
                                        sliderShapeString, Repeat.ToString(), PixelLength.ToString(CultureInfo.InvariantCulture), edgeHS, edgeAd };
                    return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(),
                                        sliderShapeString, Repeat.ToString(), PixelLength.ToString(CultureInfo.InvariantCulture), edgeHS, edgeAd, Extras };
                }
                else {
                    return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(),
                                        sliderShapeString, Repeat.ToString(), PixelLength.ToString(CultureInfo.InvariantCulture) };
                }
            }
            else if (IsSpinner) {
                if (Extras == null)
                    return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(), Math.Round(EndTime).ToString() };
                return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(), Math.Round(EndTime).ToString(), Extras };
            }
            else {
                if (Extras == null)
                    return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString() };
                return new string[] { Pos.StringX, Pos.StringY, Math.Round(Time).ToString(), ObjectType.ToString(), Hitsounds.ToString(), Extras };
            }
        }

        public int GetObjectType() {
            BitArray cs = new BitArray(new int[] { ComboSkip });
            return MathHelper.GetIntFromBitArray(new BitArray(new bool[] { IsCircle, IsSlider, NewCombo, IsSpinner, cs[0], cs[1], cs[2], IsHoldNote }));
        }

        public void SetObjectType(int type) {
            BitArray b = new BitArray(new int[] { type });
            IsCircle = b[0];
            IsSlider = b[1];
            NewCombo = b[2];
            IsSpinner = b[3];
            ComboSkip = MathHelper.GetIntFromBitArray(new BitArray(new bool[] { b[4], b[5], b[6] }));
            IsHoldNote = b[7];
        }

        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new bool[] { Normal, Whistle, Finish, Clap }));
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
                return string.Join(":", new string[] { Math.Round(EndTime).ToString(), ((int)SampleSet).ToString(), ((int)AdditionSet).ToString(),
                                                        CustomIndex.ToString(), SampleVolume.ToString(), Filename });
            }
            else {
                return string.Join(":", new string[] { ((int)SampleSet).ToString(), ((int)AdditionSet).ToString(),
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
            SampleSet = (SampleSet)int.Parse(split[i]);
            AdditionSet = (SampleSet)int.Parse(split[i + 1]);
            CustomIndex = int.Parse(split[i + 2]);
            SampleVolume = double.Parse(split[i + 3]);
            Filename = split[i + 4];
        }

        public SliderPath GetSliderPath(bool fullLength = false)
        {
            List<Vector2> controlPoints = new List<Vector2> { Pos };
            controlPoints.AddRange(CurvePoints);
            return fullLength ? new SliderPath(SliderType, controlPoints.ToArray()) : new SliderPath(SliderType, controlPoints.ToArray(), PixelLength);
        }

        public void SetSliderPath(SliderPath sliderPath)
        {
            List<Vector2> controlPoints = sliderPath.ControlPoints;
            Pos = controlPoints.First();
            CurvePoints = controlPoints.GetRange(1, controlPoints.Count - 1);
            SliderType = sliderPath.Type;
            PixelLength = sliderPath.Distance;
        }

        private double ParseDouble(string d) {
            return double.Parse(d, CultureInfo.InvariantCulture);
        }

        private PathType GetPathType(string[] sliderData) {
            for (int i = sliderData.Length - 1; i >= 0; i--) {  // Iterating in reverse to get the last valid letter
                char letter = sliderData[i].Count() > 0 ? sliderData[i][0] : '0';  // 0 is not a letter so it will get ignored
                if (Char.IsLetter(letter)) {
                    switch (letter) {
                        case 'L':
                            return PathType.Linear;
                        case 'B':
                            return PathType.Bezier;
                        case 'P':
                            return PathType.PerfectCurve;
                        case 'C':
                            return PathType.Catmull;
                    } 
                }
            }
            return PathType.Bezier;
        }

        private string GetPathTypeString() {
            switch (SliderType) {
                case (PathType.Linear):
                    return "L";
                case (PathType.PerfectCurve):
                    return "P";
                case (PathType.Catmull):
                    return "C";
            }
            return "B";
        }

        public void Debug() {
            Console.WriteLine("temporal length: " + TemporalLength);
            foreach (TimingPoint tp in BodyHitsounds) {
                Console.WriteLine("bodyhitsound:");
                Console.WriteLine("volume: " + tp.Volume);
                Console.WriteLine("sampleset: " + tp.SampleSet);
                Console.WriteLine("index: " + tp.SampleIndex);
            }
            foreach (TimelineObject tlo in TimelineObjects) {
                Console.WriteLine("timelineobject:");
                Console.WriteLine("time: " + tlo.Time);
                Console.WriteLine("repeat: " + tlo.Repeat);
                Console.WriteLine("index: " + tlo.CustomIndex);
                Console.WriteLine("volume: " + tlo.SampleVolume);
                Console.WriteLine("filename: " + tlo.Filename);
                Console.WriteLine("feno index: " + tlo.FenoCustomIndex);
                Console.WriteLine("feno volume: " + tlo.FenoSampleVolume);
            }
        }
    }
}
