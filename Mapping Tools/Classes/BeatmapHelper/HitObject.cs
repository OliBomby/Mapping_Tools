using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class HitObject : ITextLine, IComparable<HitObject> {

        public HitObject() { }

        public HitObject(string line) {
            // Example lines:
            // 74,183,57308,2,0,B|70:236,1,53.9999983520508,4|0,0:3|0:0,0:0:0:0:
            // 295,347,57458,5,2,0:0:0:0:
            // Mania:
            // 128,192,78,1,0,0:0:0:0:
            // 213,192,78,128,0,378:0:0:0:0:

            SetLine(line);
        }

        public HitObject(Vector2 pos, double time, HitObjectType type, bool newCombo, int comboSkip,
            bool normal, bool whistle, bool finish, bool clap, SampleSet sampleSet, SampleSet additionSet,
            int index, double volume, string filename) {
            Pos = pos;
            // Let the end position be the same as the start position before changed later for sliders
            EndPos = Pos;
            Time = time;
            SetObjectType(type);
            NewCombo = newCombo;
            ComboSkip = comboSkip;
            Normal = normal;
            Whistle = whistle;
            Finish = finish;
            Clap = clap;
            SampleSet = sampleSet;
            AdditionSet = additionSet;
            CustomIndex = index;
            SampleVolume = volume;
            Filename = filename;
        }

        public HitObject(Vector2 pos, double time, int type, int hitsounds, SampleSet sampleSet, SampleSet additionSet,
            int index, double volume, string filename) {
            Pos = pos;
            // Let the end position be the same as the start position before changed later for sliders
            EndPos = Pos;
            Time = time;
            SetObjectType(type);
            SetHitsounds(hitsounds);
            SampleSet = sampleSet;
            AdditionSet = additionSet;
            CustomIndex = index;
            SampleVolume = volume;
            Filename = filename;
        }

        public HitObject(double time, int hitsounds, SampleSet sampleSet, SampleSet additions) {
            // Basic hitsoundind circle
            Pos = new Vector2(256, 192);
            // Let the end position be the same as the start position before changed later for sliders
            EndPos = Pos;
            Time = time;
            SetObjectType(5);
            SetHitsounds(hitsounds);
            SampleSet = sampleSet;
            AdditionSet = additions;
            CustomIndex = 0;
            SampleVolume = 0;
            Filename = string.Empty;
        }

        public HitObject(Editor_Reader.HitObject ob) {
            PixelLength = ob.SpatialLength;
            Time = ob.StartTime;
            ObjectType = ob.Type;
            EndTime = ob.EndTime;
            Hitsounds = ob.SoundType;
            if (IsSlider) {
                Repeat = ob.SegmentCount;

                SliderType = (PathType) ob.CurveType;
                if (ob.sliderCurvePoints != null) {
                    CurvePoints = new List<Vector2>(ob.sliderCurvePoints.Length / 2);
                    for (var i = 1; i < ob.sliderCurvePoints.Length / 2; i++)
                        CurvePoints.Add(new Vector2(ob.sliderCurvePoints[i * 2], ob.sliderCurvePoints[i * 2 + 1]));
                }

                EdgeHitsounds = new List<int>(Repeat + 1);
                if (ob.SoundTypeList != null)
                    EdgeHitsounds = ob.SoundTypeList.ToList();
                for (var i = EdgeHitsounds.Count; i < Repeat + 1; i++) EdgeHitsounds.Add(0);

                EdgeSampleSets = new List<SampleSet>(Repeat + 1);
                EdgeAdditionSets = new List<SampleSet>(Repeat + 1);
                if (ob.SampleSetList != null)
                    EdgeSampleSets = Array.ConvertAll(ob.SampleSetList, ss => (SampleSet) ss).ToList();
                if (ob.SampleSetAdditionsList != null)
                    EdgeAdditionSets = Array.ConvertAll(ob.SampleSetAdditionsList, ss => (SampleSet) ss).ToList();
                for (var i = EdgeSampleSets.Count; i < Repeat + 1; i++) EdgeSampleSets.Add(SampleSet.Auto);
                for (var i = EdgeAdditionSets.Count; i < Repeat + 1; i++) EdgeAdditionSets.Add(SampleSet.Auto);
            } else if (IsSpinner || IsHoldNote) {
                Repeat = 1;
            } else {
                Repeat = 0;
            }

            Pos = new Vector2(ob.X, ob.Y);
            // Let the end position be the same as the start position before changed later for sliders
            EndPos = Pos;

            Filename = ob.SampleFile;
            SampleVolume = ob.SampleVolume;
            SampleSet = (SampleSet) ob.SampleSet;
            AdditionSet = (SampleSet) ob.SampleSetAdditions;
            CustomIndex = ob.CustomSampleSet;
            IsSelected = ob.IsSelected;
        }

        [JsonProperty]
        public string Line {
            get => GetLine();
            set => SetLine(value);
        }

        /// <summary>
        /// Base position of hit object.
        /// </summary>
        public Vector2 Pos { get; set; }

        /// <summary>
        /// Position of slider end. By default is equal to the start position.
        /// </summary>
        public Vector2 EndPos { get; set; }

        /// <summary>
        /// Stacked position of hit object. Must be computed by beatmap.
        /// </summary>
        public Vector2 StackedPos { get; set; }

        /// <summary>
        /// Stacked slider end position of hit object. Must be computed by beatmap.
        /// </summary>
        public Vector2 StackedEndPos { get; set; }

        public double Time { get; set; }

        public int ObjectType {
            get => GetObjectType();
            set => SetObjectType(value);
        }

        public bool IsCircle { get; set; }
        public bool IsSlider { get; set; }
        public bool NewCombo { get; set; }
        public bool IsSpinner { get; set; }
        public int ComboSkip { get; set; }
        public bool IsHoldNote { get; set; }

        public int Hitsounds {
            get => GetHitsounds();
            set => SetHitsounds(value);
        }

        public bool Normal { get; set; }
        public bool Whistle { get; set; }
        public bool Finish { get; set; }
        public bool Clap { get; set; }

        public string Extras {
            get => GetExtras();
            set => SetExtras(value);
        }

        public SampleSet SampleSet { get; set; }
        public SampleSet AdditionSet { get; set; }
        public int CustomIndex { get; set; }
        public double SampleVolume { get; set; }
        public string Filename { get; set; }

        public PathType SliderType { get; set; }
        public List<Vector2> CurvePoints { get; set; }

        public SliderPath SliderPath {
            get => GetSliderPath();
            set => SetSliderPath(value);
        }

        public int Repeat {
            get => IsSlider ? _repeat : IsCircle ? 0 : 1;
            set => _repeat = value;
        }

        public double PixelLength { get; set; }
        public List<int> EdgeHitsounds { get; set; }
        public List<SampleSet> EdgeSampleSets { get; set; }
        public List<SampleSet> EdgeAdditionSets { get; set; }

        public bool SliderExtras => GetSliderExtras();
        
        [JsonProperty]
        public bool ActualNewCombo { get; set; }
        [JsonProperty]
        public int ComboIndex { get; set; }
        [JsonProperty]
        public int ColourIndex { get; set; }
        [JsonProperty]
        public ComboColour Colour { get; set; }
        
        [JsonProperty]
        public double TemporalLength { get; set; } // Duration of one repeat

        public double EndTime {
            get => GetEndTime();
            set => SetEndTime(value);
        } // Includes all repeats

        public double GetEndTime(bool floor = true) {
            var endTime = Time + TemporalLength * Repeat;
            return floor ? Math.Floor(endTime + Precision.DOUBLE_EPSILON) : endTime;
        }

        private void SetEndTime(double value) {
            TemporalLength = Repeat == 0 ? 0 : (value - Time) / Repeat;
        }

        /// <summary>
        /// The stack count indicates the number of hit objects that this object is stacked upon.
        /// Used for calculating stack offset.
        /// </summary>
        public int StackCount { get; set; }

        // Special combined with greenline
        [JsonProperty]
        public double SliderVelocity { get; set; }
        [JsonProperty]
        public TimingPoint TimingPoint { get; set; }
        [JsonProperty]
        public TimingPoint HitsoundTimingPoint { get; set; }
        [JsonProperty]
        public TimingPoint UnInheritedTimingPoint { get; set; }
        
        [JsonProperty]
        public bool IsSelected { get; set; }

        public List<TimingPoint> BodyHitsounds = new List<TimingPoint>();
        private int _repeat;

        // Special combined with timeline
        public List<TimelineObject> TimelineObjects = new List<TimelineObject>();

        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public bool SaveWithFloatPrecision { get; set; }


        /// <inheritdoc />
        public void SetLine(string line) {
            var values = line.Split(',');

            if (values.Length <= 4)
                throw new BeatmapParsingException("Hit object is missing values.", line);

            if (TryParseDouble(values[0], out var x) && TryParseDouble(values[1], out var y))
                Pos = new Vector2(x, y);
            else throw new BeatmapParsingException("Failed to parse coordinate of hit object.", line);

            // Let the end position be the same as the start position before changed later for sliders
            EndPos = Pos;

            if (TryParseDouble(values[2], out var t))
                Time = t;
            else throw new BeatmapParsingException("Failed to parse time of hit object.", line);

            if (TryParseInt(values[3], out var type))
                ObjectType = type;
            else throw new BeatmapParsingException("Failed to parse type of hit object.", line);

            if (TryParseInt(values[4], out var hitsounds))
                Hitsounds = hitsounds;
            else throw new BeatmapParsingException("Failed to parse hitsound of hit object.", line);

            // Sliders remove extras and edges stuff if there are no hitsounds
            if (IsSlider) {
                if (values.Length <= 7)
                    throw new BeatmapParsingException("Slider object is missing values.", line);

                var sliderData = values[5].Split('|');

                SliderType = GetPathType(sliderData);

                var points = new List<Vector2>();
                for (var i = 1; i < sliderData.Length; i++) {
                    var spl = sliderData[i].Split(':');
                    if (spl.Length == 2) // It has to have 2 coordinates inside
                    {
                        if (TryParseDouble(spl[0], out var ax) && TryParseDouble(spl[1], out var ay))
                            points.Add(new Vector2(ax, ay));
                        else throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", line);
                    }
                }

                CurvePoints = points;

                if (TryParseInt(values[6], out var repeat))
                    Repeat = repeat;
                else throw new BeatmapParsingException("Failed to parse repeat number of slider.", line);

                if (TryParseDouble(values[7], out var pixelLength))
                    PixelLength = pixelLength;
                else throw new BeatmapParsingException("Failed to parse pixel length of slider.", line);

                // Edge hitsounds on 8
                EdgeHitsounds = new List<int>(Repeat + 1);
                if (values.Length > 8) {
                    var split = values[8].Split('|');
                    for (var i = 0; i < Math.Min(split.Length, Repeat + 1); i++)
                        EdgeHitsounds.Add(TryParseInt(split[i], out var ehs) ? ehs : hitsounds);
                }

                for (var i = EdgeHitsounds.Count; i < Repeat + 1; i++) EdgeHitsounds.Add(hitsounds);

                // Edge samplesets on 9
                EdgeSampleSets = new List<SampleSet>(Repeat + 1);
                EdgeAdditionSets = new List<SampleSet>(Repeat + 1);
                if (values.Length > 9) {
                    var split = values[9].Split('|');
                    for (var i = 0; i < Math.Min(split.Length, Repeat + 1); i++) {
                        EdgeSampleSets.Add(TryParseInt(split[i].Split(':')[0], out var ess)
                            ? (SampleSet) ess
                            : SampleSet.Auto);
                        EdgeAdditionSets.Add(TryParseInt(split[i].Split(':')[1], out var eas)
                            ? (SampleSet) eas
                            : SampleSet.Auto);
                    }
                }

                for (var i = EdgeSampleSets.Count; i < Repeat + 1; i++) EdgeSampleSets.Add(SampleSet.Auto);
                for (var i = EdgeAdditionSets.Count; i < Repeat + 1; i++) EdgeAdditionSets.Add(SampleSet.Auto);

                // Extras on 10
                if (values.Length > 10)
                    Extras = values[10];
                else
                    SetExtras();
            } else if (IsSpinner) {
                if (values.Length <= 5)
                    throw new BeatmapParsingException("Spinner object is missing values.", line);

                if (TryParseDouble(values[5], out var et))
                    EndTime = et;
                else throw new BeatmapParsingException("Failed to parse end time of spinner.", line);

                TemporalLength = EndTime - Time;
                Repeat = 1;

                // Extras on 6
                if (values.Length > 6)
                    Extras = values[6];
                else
                    SetExtras();
            } else {
                // Circle or hold note
                Repeat = 0;
                EndTime = Time;
                TemporalLength = 0;

                // Extras on 5
                if (values.Length > 5)
                    Extras = values[5];
                else
                    SetExtras();
            }
        }

        /// <inheritdoc />
        public string GetLine() {
            var values = new List<string> {
                SaveWithFloatPrecision ? Pos.X.ToInvariant() : Pos.X.ToRoundInvariant(),
                SaveWithFloatPrecision ? Pos.Y.ToInvariant() : Pos.Y.ToRoundInvariant(),
                SaveWithFloatPrecision ? Time.ToInvariant() : Time.ToRoundInvariant(),
                ObjectType.ToInvariant(),
                Hitsounds.ToInvariant()
            };

            if (IsSlider) {
                var builder = new StringBuilder();
                builder.Append(GetPathTypeString());
                foreach (var p in CurvePoints) builder.Append($"|{(SaveWithFloatPrecision ? p.X.ToInvariant() : p.X.ToRoundInvariant())}:{(SaveWithFloatPrecision ? p.Y.ToInvariant() : p.Y.ToRoundInvariant())}");
                values.Add(builder.ToString());
                values.Add(Repeat.ToInvariant());
                values.Add(PixelLength.ToInvariant());

                if (SliderExtras) {
                    // Edge hitsounds, samplesets and extras
                    values.Add(string.Join("|", EdgeHitsounds.Select(p => p.ToInvariant())));

                    var builder2 = new StringBuilder();
                    for (var i = 0; i < EdgeSampleSets.Count(); i++)
                        builder2.Append(
                            $"|{EdgeSampleSets[i].ToIntInvariant()}:{EdgeAdditionSets[i].ToIntInvariant()}");
                    builder2.Remove(0, 1);
                    values.Add(builder2.ToString());

                    values.Add(Extras);
                }
            } else if (IsSpinner) {
                values.Add(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant());
                values.Add(Extras);
            } else {
                // It's a circle or a hold note
                // Hold note has a difference in GetExtras
                values.Add(Extras);
            }

            return string.Join(",", values);
        }

        /// <summary>
        /// </summary>
        /// <param name="ob"></param>
        /// <returns></returns>
        public static explicit operator HitObject(Editor_Reader.HitObject ob) {
            return new HitObject(ob);
        }

        public List<string> GetPlayingBodyFilenames(double sliderTickRate, bool includeDefaults = true) {
            var samples = new List<string>();
            if (IsSlider) {
                // Get sliderslide hitsounds for every timingpoint in the slider
                if (includeDefaults || TimingPoint.SampleIndex != 0) {
                    var firstSampleSet = SampleSet == SampleSet.Auto ? TimingPoint.SampleSet : SampleSet;
                    samples.Add(GetSliderFilename(firstSampleSet, "slide", TimingPoint.SampleIndex));
                    if (Whistle)
                        samples.Add(GetSliderFilename(firstSampleSet, "whistle", TimingPoint.SampleIndex));
                }

                foreach (var bodyTp in BodyHitsounds)
                    if (includeDefaults || bodyTp.SampleIndex != 0) {
                        var sampleSet = SampleSet == SampleSet.Auto ? bodyTp.SampleSet : SampleSet;
                        samples.Add(GetSliderFilename(sampleSet, "slide", bodyTp.SampleIndex));
                        if (Whistle)
                            samples.Add(GetSliderFilename(sampleSet, "whistle", bodyTp.SampleIndex));
                    }

                // Add tick samples
                // 10 ms over tick time is tick
                var t = Time + UnInheritedTimingPoint.MpB / sliderTickRate;
                while (t + 10 < EndTime) {
                    var bodyTp = Timing.GetTimingPointAtTime(t, BodyHitsounds, TimingPoint);
                    if (includeDefaults || bodyTp.SampleIndex != 0) {
                        var sampleSet = SampleSet == SampleSet.Auto ? bodyTp.SampleSet : SampleSet;
                        samples.Add(GetSliderFilename(sampleSet, "tick", bodyTp.SampleIndex));
                    }

                    t += UnInheritedTimingPoint.MpB / sliderTickRate;
                }
            }

            return samples;
        }

        /// <summary>
        ///     Gets the type of this hit object.
        /// </summary>
        /// <exception cref="InvalidOperationException">If this hit object has no type.</exception>
        public HitObjectType GetHitObjectType() {
            if (IsCircle) return HitObjectType.Circle;

            if (IsSlider) return HitObjectType.Slider;

            if (IsSpinner) return HitObjectType.Spinner;

            if (IsHoldNote) return HitObjectType.HoldNote;

            throw new InvalidOperationException("This hit object has no type.");
        }

        private string GetSliderFilename(SampleSet sampleSet, string sampleName, int index) {
            if (index == 0) return $"{sampleSet.ToString().ToLower()}-slider{sampleName}-default.wav";
            if (index == 1) return $"{sampleSet.ToString().ToLower()}-slider{sampleName}.wav";
            return $"{sampleSet.ToString().ToLower()}-slider{sampleName}{index}.wav";
        }

        public List<double> GetAllTloTimes(Timing timing) {
            var times = new List<double>();

            if (IsCircle) {
                times.Add(Time);
            } else if (IsSlider) {
                // Adding time for every repeat of the slider
                var sliderTemporalLength = timing.CalculateSliderTemporalLength(Time, PixelLength);

                for (var i = 0; i <= Repeat; i++) {
                    var time = Math.Floor(Time + sliderTemporalLength * i);
                    times.Add(time);
                }
            } else if (IsSpinner || IsHoldNote) {
                times.Add(Time);
                times.Add(EndTime);
            }

            return times;
        }

        /// <summary>
        /// Removes all hitounds and sets samplesets to auto.
        /// Also clears hitsounds from timeline objects and clears body hitsounds.
        /// </summary>
        public void ResetHitsounds() {
            SetHitsounds(1);
            SampleSet = SampleSet.Auto;
            AdditionSet = SampleSet.Auto;
            SampleVolume = 0;
            CustomIndex = 0;
            Filename = string.Empty;
            if (IsSlider) {
                for (int i = 0; i < EdgeHitsounds.Count; i++) {
                    EdgeHitsounds[i] = 0;
                }
                for (int i = 0; i < EdgeSampleSets.Count; i++) {
                    EdgeSampleSets[i] = SampleSet.Auto;
                }
                for (int i = 0; i < EdgeAdditionSets.Count; i++) {
                    EdgeAdditionSets[i] = SampleSet.Auto;
                }
            }

            foreach (var tlo in TimelineObjects) {
                tlo.ResetHitsounds();
            }

            BodyHitsounds.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="deltaTime"></param>
        public void MoveTime(double deltaTime) {
            Time += deltaTime;

            // Move its timelineobjects
            foreach (var tlo in TimelineObjects) tlo.Time += deltaTime;

            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime || s.Offset <= Time);
        }

        public void MoveEndTime(Timing timing, double deltaTime) {
            if (Repeat == 0) return;

            ChangeTemporalTime(timing, deltaTime / Repeat);
        }

        public void CalculateSliderTemporalLength(Timing timing, bool useOwnSv) {
            if (!IsSlider) return;
            if (double.IsNaN(PixelLength) || PixelLength < 0 || CurvePoints.All(o => o == Pos)) {
                TemporalLength = 0;
            } else {
                TemporalLength = useOwnSv
                    ? timing.CalculateSliderTemporalLength(Time, PixelLength, SliderVelocity)
                    : timing.CalculateSliderTemporalLength(Time, PixelLength);
            }
        }

        public void ChangeTemporalTime(Timing timing, double deltaTemporalTime) {
            if (Repeat == 0) return;

            if (IsSlider) {
                var deltaLength = -10000 * timing.SliderMultiplier * deltaTemporalTime /
                                  (UnInheritedTimingPoint.MpB *
                                   (double.IsNaN(SliderVelocity) ? -100 : SliderVelocity)); // Divide by repeats because the endtime is multiplied by repeats
                PixelLength += deltaLength; // Change the pixel length to match the new time
            }

            // Change
            TemporalLength += deltaTemporalTime;

            // Move body objects
            UpdateTimelineObjectTimes();

            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime);
        }

        public void UpdateTimelineObjectTimes() {
            for (int i = 0; i < Math.Min(Repeat + 1, TimelineObjects.Count); i++) {
                double time = Math.Floor(Time + TemporalLength * i);
                TimelineObjects[i].Time = time;
            }
        }

        /// <summary>
        /// Calculates the <see cref="EndPos"/> for sliders.
        /// </summary>
        public void CalculateEndPosition() {
            EndPos = IsSlider ? GetSliderPath().PositionAt(1) : Pos;
        }

        /// <summary>
        /// </summary>
        /// <param name="delta"></param>
        public void Move(Vector2 delta) {
            Pos += delta;
            if (!IsSlider) return;
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = CurvePoints[i] + delta;
        }

        /// <summary>
        /// Apply a 2x2 transformation matrix to the positions and curve points.
        /// </summary>
        /// <param name="mat"></param>
        public void Transform(Matrix2 mat) {
            Pos = Matrix2.Mult(mat, Pos);
            if (!IsSlider) return;
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = Matrix2.Mult(mat, CurvePoints[i]);;
        }

        public bool ResnapSelf(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = GetResnappedTime(timing, beatDivisors, floor, tp, firstTp);
            var deltaTime = newTime - Time;
            MoveTime(deltaTime);
            return Math.Abs(deltaTime) > Precision.DOUBLE_EPSILON;
        }

        public bool ResnapEnd(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            // If there is a redline in the sliderbody then the sliderend gets snapped to a tick of the latest redline
            if (!IsSlider || timing.TimingPoints.Any(o => o.Uninherited && o.Offset <= EndTime + 20 && o.Offset > Time))
                return ResnapEndTime(timing, beatDivisors, floor, tp, firstTp);

            return ResnapEndClassic(timing, beatDivisors, firstTp);
        }

        public bool ResnapEndTime(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = timing.Resnap(EndTime, beatDivisors, floor, tp: tp, firstTp: firstTp);

            var deltaTime = newTime - EndTime;
            MoveEndTime(timing, deltaTime);

            return Math.Abs(deltaTime) > Precision.DOUBLE_EPSILON;
        }

        public bool ResnapEndClassic(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, TimingPoint firstTp = null) {
            var newTemporalLength = timing.ResnapDuration(Time, TemporalLength, beatDivisors, false, firstTp: firstTp);

            var deltaTime = newTemporalLength - TemporalLength;
            ChangeTemporalTime(timing, deltaTime);

            return Math.Abs(deltaTime) > Precision.DOUBLE_EPSILON;
        }

        public bool ResnapPosition(GameMode mode, double circleSize) {
            if (mode != GameMode.Mania) return false;
            // Resnap X to the middle of the columns and Y to 192
            var dist = 512d / Math.Round(circleSize);
            var hdist = dist / 2;

            var dX = Math.Floor(Math.Round((Pos.X - hdist) / dist) * dist + hdist) - Pos.X;
            var dY = 192 - Pos.Y;
            Move(new Vector2(dX, dY));

            return Math.Abs(dX) > Precision.DOUBLE_EPSILON || Math.Abs(dY) > Precision.DOUBLE_EPSILON;
        }

        public double GetResnappedTime(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            return timing.Resnap(Time, beatDivisors, floor, tp: tp, firstTp: firstTp);
        }

        private bool GetSliderExtras() {
            var hitsounds = GetHitsounds();
            return (EdgeHitsounds != null && EdgeHitsounds.Any(o => o != hitsounds)) ||
                   (EdgeSampleSets != null && EdgeSampleSets.Any(o => o != SampleSet.Auto)) ||
                   (EdgeAdditionSets != null && EdgeAdditionSets.Any(o => o != SampleSet.Auto)) ||
                   SampleSet != SampleSet.Auto || AdditionSet != SampleSet.Auto || CustomIndex != 0 || 
                   Math.Abs(SampleVolume) > Precision.DOUBLE_EPSILON || !string.IsNullOrEmpty(Filename);
        }

        public override string ToString() {
            return GetLine();
        }

        public int GetObjectType() {
            var cs = new BitArray(new[] {ComboSkip});
            return MathHelper.GetIntFromBitArray(new BitArray(new[]
                {IsCircle, IsSlider, NewCombo, IsSpinner, cs[0], cs[1], cs[2], IsHoldNote}));
        }

        public void SetObjectType(int type) {
            var b = new BitArray(new[] {type});
            IsCircle = b[0];
            IsSlider = b[1];
            NewCombo = b[2];
            IsSpinner = b[3];
            // Spinners ignore combo skip on .osu parsing
            ComboSkip = IsSpinner ? 0 : MathHelper.GetIntFromBitArray(new BitArray(new[] {b[4], b[5], b[6]}));
            IsHoldNote = b[7];
        }

        public void SetObjectType(HitObjectType type) {
            IsCircle = false;
            IsSlider = false;
            IsSpinner = false;
            IsHoldNote = false;

            switch (type) {
                case HitObjectType.Circle:
                    IsCircle = true;
                    break;
                case HitObjectType.Slider:
                    IsSlider = true;
                    break;
                case HitObjectType.Spinner:
                    IsSpinner = true;
                    break;
                case HitObjectType.HoldNote:
                    IsHoldNote = true;
                    break;
            }
        }

        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new[] {Normal, Whistle, Finish, Clap}));
        }

        public void SetHitsounds(int hitsounds) {
            var b = new BitArray(new[] {hitsounds});
            Normal = b[0];
            Whistle = b[1];
            Finish = b[2];
            Clap = b[3];
        }

        public string GetExtras() {
            if (IsHoldNote)
                return string.Join(":", SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant(), SampleSet.ToIntInvariant(),
                    AdditionSet.ToIntInvariant(), CustomIndex.ToInvariant(), SampleVolume.ToRoundInvariant(), Filename);
            return string.Join(":", SampleSet.ToIntInvariant(), AdditionSet.ToIntInvariant(), CustomIndex.ToInvariant(),
                SampleVolume.ToRoundInvariant(), Filename);
        }

        public void SetExtras(string extras) {
            // Extras has an extra value at the start if it's a hold note
            var split = extras.Split(':');
            var i = 0;
            if (IsHoldNote) {
                if (TryParseDouble(split[i], out var et))
                    EndTime = et;
                else throw new BeatmapParsingException("Failed to parse end time of hold note.", extras);
                TemporalLength = EndTime - Time;
                Repeat = 1;
                i += 1;
            }

            if (TryParseInt(split[i], out var ss))
                SampleSet = (SampleSet) ss;
            else throw new BeatmapParsingException("Failed to parse sample set of hit object.", extras);

            if (TryParseInt(split[i + 1], out var ass))
                AdditionSet = (SampleSet) ass;
            else throw new BeatmapParsingException("Failed to parse additional sample set of hit object.", extras);

            if (TryParseInt(split[i + 2], out var ci))
                CustomIndex = ci;
            else throw new BeatmapParsingException("Failed to parse custom index of hit object.", extras);

            if (TryParseDouble(split[i + 3], out var vol))
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
            return fullLength
                ? new SliderPath(SliderType, GetAllCurvePoints().ToArray())
                : new SliderPath(SliderType, GetAllCurvePoints().ToArray(), PixelLength);
        }

        public void SetSliderPath(SliderPath sliderPath) {
            var controlPoints = sliderPath.ControlPoints;
            SetAllCurvePoints(controlPoints);
            SliderType = sliderPath.Type;
            PixelLength = sliderPath.Distance;
        }

        public List<Vector2> GetAllCurvePoints() {
            var controlPoints = new List<Vector2> {Pos};
            controlPoints.AddRange(CurvePoints);
            return controlPoints;
        }

        public void SetAllCurvePoints(List<Vector2> controlPoints) {
            Pos = controlPoints.First();
            CurvePoints = controlPoints.GetRange(1, controlPoints.Count - 1);
        }

        private PathType GetPathType(string[] sliderData) {
            for (var i = sliderData.Length - 1; i >= 0; i--) {
                // Iterating in reverse to get the last valid letter
                var letter =
                    sliderData[i].Any() ? sliderData[i][0] : '0'; // 0 is not a letter so it will get ignored
                if (char.IsLetter(letter))
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

            // If there is no valid letter it will literally default to catmull
            return PathType.Catmull;
        }

        private string GetPathTypeString() {
            switch (SliderType) {
                case PathType.Linear:
                    return "L";
                case PathType.PerfectCurve:
                    return "P";
                case PathType.Catmull:
                    return "C";
                case PathType.Bezier:
                    return "B";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Detects a failure in the slider path algorithm causing a slider to become invisible.
        /// </summary>
        /// <returns></returns>
        public bool IsInvisible() {
            return PixelLength != 0 && PixelLength <= 0.0001 ||
                   double.IsNaN(PixelLength) ||
                   CurvePoints.All(o => o == Pos);
        }

        public HitObject DeepCopy() {
            var newHitObject = (HitObject) MemberwiseClone();
            newHitObject.BodyHitsounds = BodyHitsounds?.Select(o => o.Copy()).ToList();
            newHitObject.TimelineObjects = TimelineObjects?.Select(o => o.Copy()).ToList();
            newHitObject.CurvePoints = CurvePoints?.Copy();
            if (EdgeHitsounds != null)
                newHitObject.EdgeHitsounds = new List<int>(EdgeHitsounds);
            if (EdgeSampleSets != null)
                newHitObject.EdgeSampleSets = new List<SampleSet>(EdgeSampleSets);
            if (EdgeAdditionSets != null)
                newHitObject.EdgeAdditionSets = new List<SampleSet>(EdgeAdditionSets);
            newHitObject.TimingPoint = TimingPoint?.Copy();
            newHitObject.HitsoundTimingPoint = HitsoundTimingPoint?.Copy();
            newHitObject.UnInheritedTimingPoint = UnInheritedTimingPoint?.Copy();
            newHitObject.Colour = Colour?.Copy();
            return newHitObject;
        }

        public void Debug() {
            Console.WriteLine(GetLine());
            foreach (var tp in BodyHitsounds) {
                Console.WriteLine(@"bodyhitsound:");
                Console.WriteLine(@"volume: " + tp.Volume);
                Console.WriteLine(@"sampleset: " + tp.SampleSet);
                Console.WriteLine(@"index: " + tp.SampleIndex);
            }

            foreach (var tlo in TimelineObjects) {
                Console.WriteLine(@"timelineobject:");
                Console.WriteLine(@"time: " + tlo.Time);
                Console.WriteLine(@"repeat: " + tlo.Repeat);
                Console.WriteLine(@"index: " + tlo.CustomIndex);
                Console.WriteLine(@"volume: " + tlo.SampleVolume);
                Console.WriteLine(@"filename: " + tlo.Filename);
                Console.WriteLine(@"feno index: " + tlo.FenoCustomIndex);
                Console.WriteLine(@"feno volume: " + tlo.FenoSampleVolume);
            }
        }

        public int CompareTo(HitObject other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            if (Time == other.Time) return other.NewCombo.CompareTo(NewCombo);
            return Time.CompareTo(other.Time);
        }
    }
}