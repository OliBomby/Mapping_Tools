using System;
using System.Buffers;
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

        public List<PathControlPoint> CurvePoints { get; set; }

        public SliderPath SliderPath {
            get => GetSliderPath();
            set => SetSliderPath(value);
        }

        public int Repeat {
            get => IsSlider ? repeat : IsCircle ? 0 : 1;
            set => repeat = value;
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

        public double TrueLength { get; set; } // Requires more calculation
        [JsonProperty]
        public double TemporalLength { get; set; } // Duration of one repeat

        public double EndTime {
            get => GetEndTime();
            set => SetEndTime(value);
        } // Includes all repeats

        public double GetEndTime(bool floor = true) {
            var endTime = Time + TemporalLength * Repeat;
            return floor ? Math.Floor(endTime + Precision.DoubleEpsilon) : endTime;
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

        public List<TimingPoint> BodyHitsounds = new();
        private int repeat;

        // Special combined with timeline
        public List<TimelineObject> TimelineObjects = new();

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

                var points = new List<PathControlPoint>();
                PathType? pathType = PathType.Bezier;
                foreach (var value in sliderData) {
                    if (value.Length == 0 || !char.IsLetter(value[0])) continue;

                    if (char.IsLetter(value[0])) {
                        var letter = value[0];
                        switch (letter) {
                            case 'L':
                                pathType = PathType.Linear;
                                break;
                            case 'B':
                                if (value.Length > 1 && int.TryParse(value[1..], out int degree) && degree > 0) {
                                    pathType = new PathType(SplineType.BSpline) { Degree = degree };
                                    break;
                                }

                                pathType = PathType.Bezier;
                                break;
                            case 'P':
                                pathType = PathType.PerfectCurve;
                                break;
                            case 'C':
                                pathType = PathType.Catmull;
                                break;
                        }
                    } else {
                        var spl = value.Split(':');

                        // It has to have 2 coordinates inside
                        if (spl.Length != 2) continue;

                        if (TryParseDouble(spl[0], out var ax) && TryParseDouble(spl[1], out var ay))
                            points.Add(new PathControlPoint(new Vector2(ax, ay), pathType));
                        else throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", line);

                        pathType = null;
                    }
                }

                CurvePoints = points;

                if (TryParseInt(values[6], out var r))
                    Repeat = r;
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
                            : SampleSet.None);
                        EdgeAdditionSets.Add(TryParseInt(split[i].Split(':')[1], out var eas)
                            ? (SampleSet) eas
                            : SampleSet.None);
                    }
                }

                for (var i = EdgeSampleSets.Count; i < Repeat + 1; i++) EdgeSampleSets.Add(SampleSet.None);
                for (var i = EdgeAdditionSets.Count; i < Repeat + 1; i++) EdgeAdditionSets.Add(SampleSet.None);

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


        private PathType ConvertPathType(string input)
        {
            switch (input[0])
            {
                default:
                case 'C':
                    return PathType.CATMULL;

                case 'B':
                    if (input.Length > 1 && int.TryParse(input.AsSpan(1), out int degree) && degree > 0)
                        return PathType.BSpline(degree);

                    return PathType.BEZIER;

                case 'L':
                    return PathType.LINEAR;

                case 'P':
                    return PathType.PERFECT_CURVE;
            }
        }

        /// <summary>
        /// Converts a given point string into a set of path control points.
        /// </summary>
        /// <remarks>
        /// A point string takes the form: X|1:1|2:2|2:2|3:3|Y|1:1|2:2.
        /// This has three segments:
        /// <list type="number">
        ///     <item>
        ///         <description>X: { (1,1), (2,2) } (implicit segment)</description>
        ///     </item>
        ///     <item>
        ///         <description>X: { (2,2), (3,3) } (implicit segment)</description>
        ///     </item>
        ///     <item>
        ///         <description>Y: { (3,3), (1,1), (2, 2) } (explicit segment)</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="pointString">The point string.</param>
        /// <param name="offset">The positional offset to apply to the control points.</param>
        /// <returns>All control points in the resultant path.</returns>
        private PathControlPoint[] ConvertPathString(string pointString, Vector2 offset)
        {
            // This code takes on the responsibility of handling explicit segments of the path ("X" & "Y" from above). Implicit segments are handled by calls to convertPoints().
            string[] pointStringSplit = pointString.Split('|');

            var pointsBuffer = ArrayPool<Vector2>.Shared.Rent(pointStringSplit.Length);
            var segmentsBuffer = ArrayPool<(PathType Type, int StartIndex)>.Shared.Rent(pointStringSplit.Length);
            int currentPointsIndex = 0;
            int currentSegmentsIndex = 0;

            try
            {
                foreach (string s in pointStringSplit)
                {
                    if (char.IsLetter(s[0]))
                    {
                        // The start of a new segment(indicated by having an alpha character at position 0).
                        var pathType = ConvertPathType(s);
                        segmentsBuffer[currentSegmentsIndex++] = (pathType, currentPointsIndex);

                        // First segment is prepended by an extra zero point
                        if (currentPointsIndex == 0)
                            pointsBuffer[currentPointsIndex++] = Vector2.Zero;
                    }
                    else
                    {
                        pointsBuffer[currentPointsIndex++] = readPoint(s, offset);
                    }
                }

                int pointsCount = currentPointsIndex;
                int segmentsCount = currentSegmentsIndex;
                var controlPoints = new List<ArraySegment<PathControlPoint>>(pointsCount);
                var allPoints = new ArraySegment<Vector2>(pointsBuffer, 0, pointsCount);

                for (int i = 0; i < segmentsCount; i++)
                {
                    if (i < segmentsCount - 1)
                    {
                        int startIndex = segmentsBuffer[i].StartIndex;
                        int endIndex = segmentsBuffer[i + 1].StartIndex;
                        controlPoints.AddRange(ConvertPoints(segmentsBuffer[i].Type, allPoints.Slice(startIndex, endIndex - startIndex), pointsBuffer[endIndex]));
                    }
                    else
                    {
                        int startIndex = segmentsBuffer[i].StartIndex;
                        controlPoints.AddRange(ConvertPoints(segmentsBuffer[i].Type, allPoints.Slice(startIndex), null));
                    }
                }

                return MergeControlPointsLists(controlPoints);
            }
            finally
            {
                ArrayPool<Vector2>.Shared.Return(pointsBuffer);
                ArrayPool<(PathType, int)>.Shared.Return(segmentsBuffer);
            }

            static Vector2 readPoint(string value, Vector2 startPos)
            {
                string[] vertexSplit = value.Split(':');

                Vector2 pos = new Vector2((int)Parsing.ParseDouble(vertexSplit[0], Parsing.MAX_COORDINATE_VALUE), (int)Parsing.ParseDouble(vertexSplit[1], Parsing.MAX_COORDINATE_VALUE)) - startPos;
                return pos;
            }
        }

        /// <summary>
        /// Converts a given point list into a set of path segments.
        /// </summary>
        /// <param name="type">The path type of the point list.</param>
        /// <param name="points">The point list.</param>
        /// <param name="endPoint">Any extra endpoint to consider as part of the points. This will NOT be returned.</param>
        /// <returns>The set of points contained by <paramref name="points"/> as one or more segments of the path.</returns>
        private IEnumerable<ArraySegment<PathControlPoint>> ConvertPoints(PathType type, ArraySegment<Vector2> points, Vector2? endPoint)
        {
            var vertices = new PathControlPoint[points.Count];

            // Parse into control points.
            for (int i = 0; i < points.Count; i++)
                vertices[i] = new PathControlPoint { Position = points[i] };

            // Edge-case rules (to match stable).
            if (type == PathType.PERFECT_CURVE)
            {
                int endPointLength = endPoint == null ? 0 : 1;

                if (formatVersion < LegacyBeatmapEncoder.FIRST_LAZER_VERSION)
                {
                    if (vertices.Length + endPointLength != 3)
                        type = PathType.BEZIER;
                    else if (isLinear(points[0], points[1], endPoint ?? points[2]))
                    {
                        // osu-stable special-cased colinear perfect curves to a linear path
                        type = PathType.LINEAR;
                    }
                }
                else if (vertices.Length + endPointLength > 3)
                    // Lazer supports perfect curves with less than 3 points and colinear points
                    type = PathType.BEZIER;
            }

            // The first control point must have a definite type.
            vertices[0].Type = type;

            // A path can have multiple implicit segments of the same type if there are two sequential control points with the same position.
            // To handle such cases, this code may return multiple path segments with the final control point in each segment having a non-null type.
            // For the point string X|1:1|2:2|2:2|3:3, this code returns the segments:
            // X: { (1,1), (2, 2) }
            // X: { (3, 3) }
            // Note: (2, 2) is not returned in the second segments, as it is implicit in the path.
            int startIndex = 0;
            int endIndex = 0;

            while (++endIndex < vertices.Length)
            {
                // Keep incrementing while an implicit segment doesn't need to be started.
                if (vertices[endIndex].Position != vertices[endIndex - 1].Position)
                    continue;

                // Legacy CATMULL sliders don't support multiple segments, so adjacent CATMULL segments should be treated as a single one.
                // Importantly, this is not applied to the first control point, which may duplicate the slider path's position
                // resulting in a duplicate (0,0) control point in the resultant list.
                if (type == PathType.CATMULL && endIndex > 1 && formatVersion < LegacyBeatmapEncoder.FIRST_LAZER_VERSION)
                    continue;

                // The last control point of each segment is not allowed to start a new implicit segment.
                if (endIndex == vertices.Length - 1)
                    continue;

                // Force a type on the last point, and return the current control point set as a segment.
                vertices[endIndex - 1].Type = type;
                yield return new ArraySegment<PathControlPoint>(vertices, startIndex, endIndex - startIndex);

                // Skip the current control point - as it's the same as the one that's just been returned.
                startIndex = endIndex + 1;
            }

            if (startIndex < endIndex)
                yield return new ArraySegment<PathControlPoint>(vertices, startIndex, endIndex - startIndex);

            static bool isLinear(Vector2 p0, Vector2 p1, Vector2 p2)
                => Precision.AlmostEquals(0, (p1.Y - p0.Y) * (p2.X - p0.X)
                                             - (p1.X - p0.X) * (p2.Y - p0.Y));
        }

        private PathControlPoint[] MergeControlPointsLists(List<ArraySegment<PathControlPoint>> controlPointList)
        {
            int totalCount = 0;

            foreach (var arr in controlPointList)
                totalCount += arr.Count;

            var mergedArray = new PathControlPoint[totalCount];
            int copyIndex = 0;

            foreach (var arr in controlPointList)
            {
                arr.AsSpan().CopyTo(mergedArray.AsSpan(copyIndex));
                copyIndex += arr.Count;
            }

            return mergedArray;
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
                if (SaveWithFloatPrecision) {
                    builder.Append(GetPathTypeString(SliderType));
                    foreach (var p in CurvePoints)
                        builder.Append($"|{(SaveWithFloatPrecision ? p.X.ToInvariant() : p.X.ToRoundInvariant())}:{(SaveWithFloatPrecision ? p.Y.ToInvariant() : p.Y.ToRoundInvariant())}");
                } else {
                    builder.Append(GetPathTypeString(SliderType));
                    foreach (var p in CurvePoints)
                        builder.Append($"|{(SaveWithFloatPrecision ? p.X.ToInvariant() : p.X.ToRoundInvariant())}:{(SaveWithFloatPrecision ? p.Y.ToInvariant() : p.Y.ToRoundInvariant())}");
                }
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

        public List<string> GetPlayingBodyFilenames(double sliderTickRate, bool includeDefaults = true) {
            var samples = new List<string>();
            if (IsSlider) {
                // Get sliderslide hitsounds for every timingpoint in the slider
                if (includeDefaults || TimingPoint.SampleIndex != 0) {
                    var firstSampleSet = SampleSet == SampleSet.None ? TimingPoint.SampleSet : SampleSet;
                    samples.Add(GetSliderFilename(firstSampleSet, "slide", TimingPoint.SampleIndex));
                    if (Whistle)
                        samples.Add(GetSliderFilename(firstSampleSet, "whistle", TimingPoint.SampleIndex));
                }

                foreach (var bodyTp in BodyHitsounds)
                    if (includeDefaults || bodyTp.SampleIndex != 0) {
                        var sampleSet = SampleSet == SampleSet.None ? bodyTp.SampleSet : SampleSet;
                        samples.Add(GetSliderFilename(sampleSet, "slide", bodyTp.SampleIndex));
                        if (Whistle)
                            samples.Add(GetSliderFilename(sampleSet, "whistle", bodyTp.SampleIndex));
                    }

                // Add tick samples
                // 10 ms over tick time is tick
                foreach (var t in GetSliderTickTimes(sliderTickRate))
                {
                    var bodyTp = Timing.GetTimingPointAtTime(t, BodyHitsounds, TimingPoint);
                    if (includeDefaults || bodyTp.SampleIndex != 0) {
                        var sampleSet = SampleSet == SampleSet.None ? bodyTp.SampleSet : SampleSet;
                        samples.Add(GetSliderFilename(sampleSet, "tick", bodyTp.SampleIndex));
                    }
                }
            }

            return samples;
        }

        public List<double> GetSliderTickTimes(double sliderTickRate) {
            // Sliders with NaN velocity don't have ticks
            if (!IsSlider || double.IsNaN(SliderVelocity)) return new List<double>();

            var ticks = new List<double>();
            var t = UnInheritedTimingPoint.MpB / sliderTickRate;
            while (t + 10 < TemporalLength) {
                ticks.Add(t);
                t += UnInheritedTimingPoint.MpB / sliderTickRate;
            }

            // Each repeat does the same tick times but in reverse for reverse passes
            var allTicks = new List<double>();
            for (var i = 0; i < Repeat; i++) {
                int i2 = i;
                allTicks.AddRange(i % 2 == 0
                    ? ticks.Select(tick => Time + i2 * TemporalLength + tick)
                    : ticks.Select(tick => Time + (i2 + 1) * TemporalLength - tick)
                    );
                ticks.Reverse();
            }

            return allTicks;
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
            SampleSet = SampleSet.None;
            AdditionSet = SampleSet.None;
            SampleVolume = 0;
            CustomIndex = 0;
            Filename = string.Empty;
            if (IsSlider) {
                for (int i = 0; i < EdgeHitsounds.Count; i++) {
                    EdgeHitsounds[i] = 0;
                }
                for (int i = 0; i < EdgeSampleSets.Count; i++) {
                    EdgeSampleSets[i] = SampleSet.None;
                }
                for (int i = 0; i < EdgeAdditionSets.Count; i++) {
                    EdgeAdditionSets[i] = SampleSet.None;
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

        /// <summary>
        /// Calculates the linear distance between each control point.
        /// </summary>
        public static float QuickCalculateLength(IEnumerable<Vector2> controlPoints) {
            float length = 0;
            Vector2? lastPoint = null;
            foreach (var cp in controlPoints) {
                if (lastPoint.HasValue) {
                    length += (float)Vector2.Distance(lastPoint.Value, cp);
                }
                lastPoint = cp;
            }
            return length;
        }

        public void CalculateSliderTrueLength() {            
            if (!IsSlider || double.IsNaN(PixelLength) || PixelLength < 0 || CurvePoints.All(o => o == Pos)) {
                TrueLength = 0;
                return;
            }
            if (SliderType == PathType.Linear && CurvePoints.Count > 1 && 
                CurvePoints[^1] == CurvePoints[^2]) {
                TrueLength = Math.Min(PixelLength, QuickCalculateLength(GetAllCurvePoints()));
                return;
            }

            TrueLength = PixelLength;
        }

        public void CalculateSliderTemporalLength(Timing timing, bool useOwnSv) {
            if (!IsSlider) return;

            CalculateSliderTrueLength();

            TemporalLength = useOwnSv
                ? timing.CalculateSliderTemporalLength(Time, TrueLength, SliderVelocity)
                : timing.CalculateSliderTemporalLength(Time, TrueLength);
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
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = Matrix2.Mult(mat, CurvePoints[i]);
        }

        public bool ResnapSelf(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = GetResnappedTime(timing, beatDivisors, floor, tp, firstTp);
            var deltaTime = newTime - Time;
            MoveTime(deltaTime);
            return Math.Abs(deltaTime) > Precision.DoubleEpsilon;
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

            return Math.Abs(deltaTime) > Precision.DoubleEpsilon;
        }

        public bool ResnapEndClassic(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, TimingPoint firstTp = null) {
            var newTemporalLength = timing.ResnapDuration(Time, TemporalLength, beatDivisors, false, firstTp: firstTp);

            var deltaTime = newTemporalLength - TemporalLength;
            ChangeTemporalTime(timing, deltaTime);

            return Math.Abs(deltaTime) > Precision.DoubleEpsilon;
        }

        public bool ResnapPosition(GameMode mode, double circleSize) {
            if (mode != GameMode.Mania) return false;
            // Resnap X to the middle of the columns and Y to 192
            var dist = 512d / Math.Round(circleSize);
            var hdist = dist / 2;

            var dX = Math.Floor(Math.Round((Pos.X - hdist) / dist) * dist + hdist) - Pos.X;
            var dY = 192 - Pos.Y;
            Move(new Vector2(dX, dY));

            return Math.Abs(dX) > Precision.DoubleEpsilon || Math.Abs(dY) > Precision.DoubleEpsilon;
        }

        public double GetResnappedTime(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            return timing.Resnap(Time, beatDivisors, floor, tp: tp, firstTp: firstTp);
        }

        private bool GetSliderExtras() {
            var hitsounds = GetHitsounds();
            return (EdgeHitsounds != null && EdgeHitsounds.Any(o => o != hitsounds)) ||
                   (EdgeSampleSets != null && EdgeSampleSets.Any(o => o != SampleSet.None)) ||
                   (EdgeAdditionSets != null && EdgeAdditionSets.Any(o => o != SampleSet.None)) ||
                   SampleSet != SampleSet.None || AdditionSet != SampleSet.None || CustomIndex != 0 || 
                   Math.Abs(SampleVolume) > Precision.DoubleEpsilon || !string.IsNullOrEmpty(Filename);
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

            SampleSet = SampleSet.None;
            AdditionSet = SampleSet.None;
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
                if (sliderData[i].Length == 0 || !char.IsLetter(sliderData[i][0])) continue;

                var letter = sliderData[i][0];
                switch (letter) {
                    case 'L':
                        return PathType.Linear;
                    case 'B':
                        if (sliderData[i].Length > 1 && int.TryParse(sliderData[i][1..], out int degree) && degree > 0)
                            return PathType.BSpline;

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

        private List<(PathType, int)> GetAdditionalPathTypes(string[] sliderData) {
            var allPathTypes = new List<(PathType, int)>();

            for (var i = 0; i < sliderData.Length; i++) {
                if (sliderData[i].Length == 0 || !char.IsLetter(sliderData[i][0])) continue;

                var letter = sliderData[i][0];
                switch (letter) {
                    case 'L':
                        allPathTypes.Add((PathType.Linear, i));
                        break;
                    case 'B':
                        if (sliderData[i].Length > 1 && int.TryParse(sliderData[i][1..], out int degree) && degree > 0) {
                            allPathTypes.Add((PathType.BSpline, i));
                            break;
                        }

                        allPathTypes.Add((PathType.Bezier, i));
                        break;
                    case 'P':
                        allPathTypes.Add((PathType.PerfectCurve, i));
                        break;
                    case 'C':
                        allPathTypes.Add((PathType.Catmull, i));
                        break;
                }
            }

            return allPathTypes;
        }

        private string GetPathTypeString(PathType pathType) {
            switch (pathType.Type) {
                case SplineType.Linear:
                    return "L";
                case SplineType.PerfectCurve:
                    return "P";
                case SplineType.Catmull:
                    return "C";
                case SplineType.BSpline:
                    if (pathType.Degree.HasValue)
                        return "B" + pathType.Degree;
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