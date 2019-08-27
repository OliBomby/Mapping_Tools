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
    public class HitObject : ITextLine {
        private int repeat;

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
        public int Repeat { get => IsSlider ? repeat : IsCircle ? 0 : 1; set => repeat = value; }
        public double PixelLength { get; set; }
        public List<int> EdgeHitsounds { get; set; }
        public List<SampleSet> EdgeSampleSets { get; set; }
        public List<SampleSet> EdgeAdditionSets { get; set; }

        public bool SliderExtras { get => GetSliderExtras(); }

        public double TemporalLength { get; set; } // Duration of one repeat
        public double EndTime { get => GetEndTime(); set => SetEndTime(value); } // Includes all repeats

        private double GetEndTime() {
            return Math.Floor(Time + TemporalLength * Repeat + Precision.DOUBLE_EPSILON);
        }

        private void SetEndTime(double value) {
            TemporalLength = Repeat == 0 ? 0 : value / Repeat;
        }

        // Special combined with greenline
        public double SV { get; set; }
        public TimingPoint TP { get; set; }
        public TimingPoint HitsoundTP { get; set; }
        public TimingPoint Redline { get; set; }
        public List<TimingPoint> BodyHitsounds = new List<TimingPoint>();

        // Special combined with timeline
        public List<TimelineObject> TimelineObjects = new List<TimelineObject>();

        public HitObject(string line) {
            // Example lines:
            // 74,183,57308,2,0,B|70:236,1,53.9999983520508,4|0,0:3|0:0,0:0:0:0:
            // 295,347,57458,5,2,0:0:0:0:
            // Mania:
            // 128,192,78,1,0,0:0:0:0:
            // 213,192,78,128,0,378:0:0:0:0:

            SetLine(line);
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

        public HitObject(Editor_Reader.HitObject ob) {
            PixelLength = ob.SpatialLength;
            Time = ob.StartTime;
            ObjectType = ob.Type;
            EndTime = ob.EndTime;
            Hitsounds = ob.SoundType;
            if (IsSlider) {
                Repeat = ob.SegmentCount;

                SliderType = (PathType)ob.CurveType;
                if (ob.sliderCurvePoints != null) {
                    CurvePoints = new List<Vector2>(ob.sliderCurvePoints.Length / 2);
                    for (int i = 1; i < ob.sliderCurvePoints.Length / 2; i++)
                        CurvePoints.Add(new Vector2(ob.sliderCurvePoints[i * 2], ob.sliderCurvePoints[i * 2 + 1]));
                }

                EdgeHitsounds = new List<int>(Repeat + 1);
                if (ob.SoundTypeList != null)
                    EdgeHitsounds = ob.SoundTypeList.ToList();
                for (int i = EdgeHitsounds.Count; i < Repeat + 1; i++) {
                    EdgeHitsounds.Add(0);
                }

                EdgeSampleSets = new List<SampleSet>(Repeat + 1);
                EdgeAdditionSets = new List<SampleSet>(Repeat + 1);
                if (ob.SampleSetList != null)
                    EdgeSampleSets = Array.ConvertAll(ob.SampleSetList, ss => (SampleSet)ss).ToList();
                if (ob.SampleSetAdditionsList != null)
                    EdgeAdditionSets = Array.ConvertAll(ob.SampleSetAdditionsList, ss => (SampleSet)ss).ToList();
                for (int i = EdgeSampleSets.Count; i < Repeat + 1; i++) {
                    EdgeSampleSets.Add(SampleSet.Auto);
                }
                for (int i = EdgeAdditionSets.Count; i < Repeat + 1; i++) {
                    EdgeAdditionSets.Add(SampleSet.Auto);
                }
            } else if (IsSpinner || IsHoldNote) {
                Repeat = 1;
            } else {
                Repeat = 0;
            }
            Pos = new Vector2(ob.X, ob.Y);
            Filename = ob.SampleFile;
            SampleVolume = ob.SampleVolume;
            SampleSet = (SampleSet)ob.SampleSet;
            AdditionSet = (SampleSet)ob.SampleSetAdditions;
            CustomIndex = ob.CustomSampleSet;

            Debug();
        }

        public static explicit operator HitObject(Editor_Reader.HitObject ob) {
            return new HitObject(ob);
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

            // Clean up body objects
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

        public bool ResnapSelf(Timing timing, int snap1, int snap2, bool floor = true, TimingPoint tp = null, TimingPoint firstTP = null) {
            double newTime = GetResnappedTime(timing, snap1, snap2, floor, tp, firstTP);
            double deltaTime = newTime - Time;
            MoveTime(deltaTime);
            return deltaTime != 0;
        }

        public bool ResnapEnd(Timing timing, int snap1, int snap2, bool floor = true, TimingPoint tp = null, TimingPoint firstTP = null) {
            // If there is a redline in the sliderbody then the sliderend gets snapped to a tick of the latest redline
            if (!IsSlider || timing.TimingPoints.Any(o => o.Inherited && o.Offset <= EndTime + 20 && o.Offset > Time)) {
                return ResnapEndTime(timing, snap1, snap2, floor, tp, firstTP);
            } else {
                return ResnapEndClassic(timing, snap1, snap2, firstTP);
            }
        }

        public bool ResnapEndTime(Timing timing, int snap1, int snap2, bool floor = true, TimingPoint tp = null, TimingPoint firstTP = null) {
            double newTime = timing.Resnap(EndTime, snap1, snap2, floor, tp, firstTP);
            double deltaTime = newTime - EndTime;
            MoveEndTime(timing, deltaTime);

            return deltaTime != 0;
        }

        public bool ResnapPosition(GameMode mode, double circleSize) {
            if (mode == GameMode.Mania) {
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

        public double GetResnappedTime(Timing timing, int snap1, int snap2, bool floor = true, TimingPoint tp = null, TimingPoint firstTP = null) {
            return timing.Resnap(Time, snap1, snap2, floor, tp, firstTP);
        }

        private bool GetSliderExtras() {
            return EdgeHitsounds.Any(o => o != 0) || EdgeSampleSets.Any(o => o != SampleSet.Auto) || EdgeAdditionSets.Any(o => o != SampleSet.Auto) || SampleSet != SampleSet.Auto || AdditionSet != SampleSet.Auto || CustomIndex != 0 || SampleVolume != 0 || Filename != "";
        }

        public void SetLine(string line) {
            var values = line.Split(',');

            if (values.Length <= 4)
                throw new BeatmapParsingException("Hit object is missing values.", line);

            if (TryParseDouble(values[0], out double x) && TryParseDouble(values[1], out double y))
                Pos = new Vector2(x, y);
            else throw new BeatmapParsingException("Failed to parse coordinate of hit object.", line);

            if (TryParseDouble(values[2], out double t))
                Time = t;
            else throw new BeatmapParsingException("Failed to parse time of hit object.", line);

            if (int.TryParse(values[3], out int type))
                ObjectType = type;
            else throw new BeatmapParsingException("Failed to parse type of hit object.", line);

            if (int.TryParse(values[4], out int hitsounds))
                Hitsounds = hitsounds;
            else throw new BeatmapParsingException("Failed to parse hitsound of hit object.", line);

            // Sliders remove extras and edges stuff if there are no hitsounds
            if (IsSlider) {
                if (values.Length <= 7)
                    throw new BeatmapParsingException("Slider object is missing values.", line);

                string[] sliderData = values[5].Split('|');

                SliderType = GetPathType(sliderData);

                List<Vector2> points = new List<Vector2>();
                for (int i = 1; i < sliderData.Length; i++) {
                    string[] spl = sliderData[i].Split(':');
                    if (spl.Length == 2) // It has to have 2 coordinates inside
                    {
                        if (TryParseDouble(spl[0], out double ax) && TryParseDouble(spl[1], out double ay))
                            points.Add(new Vector2(ax, ay));
                        else throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", line);
                    }
                }
                CurvePoints = points;

                if (int.TryParse(values[6], out int repeat))
                    Repeat = int.Parse(values[6]);
                else throw new BeatmapParsingException("Failed to parse repeat number of slider.", line);

                if (TryParseDouble(values[7], out double pixelLength))
                    PixelLength = pixelLength;
                else throw new BeatmapParsingException("Failed to parse pixel length of slider.", line);

                // Edge hitsounds on 8
                EdgeHitsounds = new List<int>(Repeat + 1);
                if (values.Length > 8) {
                    var split = values[8].Split('|');
                    for (int i = 0; i < Math.Min(split.Length, Repeat + 1); i++) {
                        EdgeHitsounds.Add(int.TryParse(split[i], out int ehs) ? ehs : 0);
                    }
                }
                for (int i = EdgeHitsounds.Count; i < Repeat + 1; i++) {
                    EdgeHitsounds.Add(0);
                }

                // Edge samplesets on 9
                EdgeSampleSets = new List<SampleSet>(Repeat + 1);
                EdgeAdditionSets = new List<SampleSet>(Repeat + 1);
                if (values.Length > 9) {
                    var split = values[9].Split('|');
                    for (int i = 0; i < Math.Min(split.Length, Repeat + 1); i++) {
                        EdgeSampleSets.Add(int.TryParse(split[i].Split(':')[0], out int ess) ? (SampleSet)ess : SampleSet.Auto);
                        EdgeAdditionSets.Add(int.TryParse(split[i].Split(':')[1], out int eas) ? (SampleSet)eas : SampleSet.Auto);
                    }
                }
                for (int i = EdgeSampleSets.Count; i < Repeat + 1; i++) {
                    EdgeSampleSets.Add(SampleSet.Auto);
                }
                for (int i = EdgeAdditionSets.Count; i < Repeat + 1; i++) {
                    EdgeAdditionSets.Add(SampleSet.Auto);
                }

                // Extras on 10
                if (values.Length > 10) {
                    Extras = values[10];
                } else {
                    SetExtras();
                }
            } else if (IsSpinner) {
                if (values.Length <= 5)
                    throw new BeatmapParsingException("Spinner object is missing values.", line);

                if (TryParseDouble(values[5], out double et))
                    EndTime = et;
                else throw new BeatmapParsingException("Failed to parse end time of spinner.", line);

                TemporalLength = EndTime - Time;
                Repeat = 1;

                // Extras on 6
                if (values.Length > 6) {
                    Extras = values[6];
                } else {
                    SetExtras();
                }
            } else {
                // Circle or hold note
                Repeat = 0;
                EndTime = Time;
                TemporalLength = 0;

                // Extras on 5
                if (values.Length > 5) {
                    Extras = values[5];
                } else {
                    SetExtras();
                }
            }
        }

        public string GetLine() {
            var values = new List<string> {
                Pos.StringX,
                Pos.StringY,
                Math.Round(Time).ToString(),
                ObjectType.ToString(),
                Hitsounds.ToString()
            };

            if (IsSlider) {
                StringBuilder builder = new StringBuilder();
                builder.Append(GetPathTypeString());
                foreach (Vector2 p in CurvePoints) {
                    builder.Append($"|{p.StringX}:{p.StringY}");
                }
                values.Add(builder.ToString());
                values.Add(Repeat.ToString());
                values.Add(PixelLength.ToString(CultureInfo.InvariantCulture));

                if (SliderExtras) {
                    // Edge hitsounds, samplesets and extras
                    values.Add(string.Join("|", EdgeHitsounds.Select(p => p.ToString())));

                    StringBuilder builder2 = new StringBuilder();
                    for (int i = 0; i < EdgeSampleSets.Count(); i++) {
                        builder2.Append($"|{(int)EdgeSampleSets[i]}:{(int)EdgeAdditionSets[i]}");
                    }
                    builder2.Remove(0, 1);
                    values.Add(builder2.ToString());

                    values.Add(Extras);
                }
            } else if (IsSpinner) {
                values.Add(Math.Round(EndTime).ToString());
                values.Add(Extras);
            } else {
                // It's a circle or a hold note
                // Hold note has a difference in GetExtras
                values.Add(Extras);
            }

            return string.Join(",", values);
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
            } else {
                return string.Join(":", new string[] { ((int)SampleSet).ToString(), ((int)AdditionSet).ToString(),
                                                        CustomIndex.ToString(), SampleVolume.ToString(), Filename });
            }
        }

        public void SetExtras(string extras) {
            // Extras has an extra value at the start if it's a hold note
            string[] split = extras.Split(':');
            int i = 0;
            if (IsHoldNote) {
                if (TryParseDouble(split[i], out double et))
                    EndTime = et;
                else throw new BeatmapParsingException("Failed to parse end time of hold note.", extras);
                TemporalLength = EndTime - Time;
                Repeat = 1;
                i += 1;
            }

            if (int.TryParse(split[i], out int ss))
                SampleSet = (SampleSet)ss;
            else throw new BeatmapParsingException("Failed to parse sample set of hit object.", extras);

            if (int.TryParse(split[i + 1], out int ass))
                AdditionSet = (SampleSet)ass;
            else throw new BeatmapParsingException("Failed to parse additional sample set of hit object.", extras);

            if (int.TryParse(split[i + 2], out int ci))
                CustomIndex = ci;
            else throw new BeatmapParsingException("Failed to parse custom index of hit object.", extras);

            if (TryParseDouble(split[i + 3], out double vol))
                SampleVolume = vol;
            else throw new BeatmapParsingException("Failed to parse volume of hit object.", extras);

            Filename = split[i + 4];
        }

        public void SetExtras() {
            // Set it to the default values
            if (IsHoldNote) {
                // Hold note should always have extras
                EndTime = Time;
                TemporalLength = 0;
                Repeat = 1;
            }
            SampleSet = SampleSet.Auto;
            AdditionSet = SampleSet.Auto;
            CustomIndex = 0;
            SampleVolume = 0;
            Filename = "";
        }

        public SliderPath GetSliderPath(bool fullLength = false) {
            List<Vector2> controlPoints = new List<Vector2> { Pos };
            controlPoints.AddRange(CurvePoints);
            return fullLength ? new SliderPath(SliderType, controlPoints.ToArray()) : new SliderPath(SliderType, controlPoints.ToArray(), PixelLength);
        }

        public void SetSliderPath(SliderPath sliderPath) {
            List<Vector2> controlPoints = sliderPath.ControlPoints;
            Pos = controlPoints.First();
            CurvePoints = controlPoints.GetRange(1, controlPoints.Count - 1);
            SliderType = sliderPath.Type;
            PixelLength = sliderPath.Distance;
        }

        private bool TryParseDouble(string d, out double result) {
            return double.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private PathType GetPathType(string[] sliderData) {
            for (int i = sliderData.Length - 1; i >= 0; i--) {  // Iterating in reverse to get the last valid letter
                char letter = sliderData[i].Count() > 0 ? sliderData[i][0] : '0';  // 0 is not a letter so it will get ignored
                if (char.IsLetter(letter)) {
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
            // If there is no valid letter it will literally default to catmull
            return PathType.Catmull;
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
            Console.WriteLine("is circle: " + IsCircle);
            Console.WriteLine("is slider: " + IsSlider);
            Console.WriteLine("is spinner: " + IsSpinner);
            Console.WriteLine("is hold note: " + IsHoldNote);
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
