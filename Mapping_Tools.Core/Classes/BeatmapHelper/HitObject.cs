#nullable disable
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
    /// Represents an osu! hit object and its parsed gameplay, timing, geometry,
    /// hitsound, and slider data.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class HitObject : ITextLine, IComparable<HitObject> {

        /// <summary>
        /// Creates an uninitialized object for serializers and incremental construction.
        /// </summary>
        public HitObject() { }

        /// <summary>
        /// Parses one comma-separated line from an osu! <c>[HitObjects]</c> section.
        /// </summary>
        /// <param name="line">The complete hit-object line, including type-specific fields and sample extras.</param>
        public HitObject(string line) {
            // Example lines:
            // 74,183,57308,2,0,B|70:236,1,53.9999983520508,4|0,0:3|0:0,0:0:0:0:
            // 295,347,57458,5,2,0:0:0:0:
            // Mania:
            // 128,192,78,1,0,0:0:0:0:
            // 213,192,78,128,0,378:0:0:0:0:

            SetLine(line);
        }

        /// <summary>
        /// Creates a hit object from decoded type and hitsound flags.
        /// </summary>
        /// <param name="pos">The object position in the 512 by 384 osu! playfield.</param>
        /// <param name="time">The start time in milliseconds.</param>
        /// <param name="type">The gameplay object kind.</param>
        /// <param name="newCombo">Whether this object starts a combo.</param>
        /// <param name="comboSkip">The number of extra combo colours to skip, from zero through seven.</param>
        /// <param name="normal">Whether the normal sample layer plays.</param>
        /// <param name="whistle">Whether the whistle addition plays.</param>
        /// <param name="finish">Whether the finish addition plays.</param>
        /// <param name="clap">Whether the clap addition plays.</param>
        /// <param name="sampleSet">The normal-layer sample set, or auto.</param>
        /// <param name="additionSet">The addition-layer sample set, or auto.</param>
        /// <param name="index">The custom sample index; zero delegates to the active timing point.</param>
        /// <param name="volume">The sample volume percentage; zero delegates to the active timing point.</param>
        /// <param name="filename">An optional beatmap-relative custom sample filename.</param>
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

        /// <summary>
        /// Creates a hit object from the packed osu! type and hitsound bit fields.
        /// </summary>
        /// <param name="pos">The object position in osu! playfield coordinates.</param>
        /// <param name="time">The start time in milliseconds.</param>
        /// <param name="type">The packed type, new-combo, and combo-skip flags from the file format.</param>
        /// <param name="hitsounds">The packed normal, whistle, finish, and clap flags.</param>
        /// <param name="sampleSet">The normal-layer sample set, or auto.</param>
        /// <param name="additionSet">The addition-layer sample set, or auto.</param>
        /// <param name="index">The custom sample index; zero delegates to timing.</param>
        /// <param name="volume">The sample volume percentage; zero delegates to timing.</param>
        /// <param name="filename">An optional custom sample filename.</param>
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

        /// <summary>
        /// Creates a centered hitsounding circle for timeline and hitsound-generation workflows.
        /// </summary>
        /// <param name="time">The circle time in milliseconds.</param>
        /// <param name="hitsounds">The packed normal, whistle, finish, and clap flags.</param>
        /// <param name="sampleSet">The normal-layer sample set.</param>
        /// <param name="additions">The addition-layer sample set.</param>
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

        /// <summary>
        /// Gets or sets the complete osu! hit-object line through <see cref="GetLine"/> and <see cref="SetLine"/>.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the start time in milliseconds.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// Gets or sets the packed osu! type, combo, and combo-skip bit field.
        /// </summary>
        public int ObjectType {
            get => GetObjectType();
            set => SetObjectType(value);
        }

        /// <summary>
        /// Indicates that the object uses circle gameplay and serialization fields.
        /// </summary>
        public bool IsCircle { get; set; }
        /// <summary>
        /// Indicates that the object carries a slider path, repeats, and edge samples.
        /// </summary>
        public bool IsSlider { get; set; }
        /// <summary>
        /// Indicates that the object begins a new combo.
        /// </summary>
        public bool NewCombo { get; set; }
        /// <summary>
        /// Indicates that the object has a spinner end time.
        /// </summary>
        public bool IsSpinner { get; set; }
        /// <summary>
        /// Gets or sets how many additional combo colours are skipped when starting a new combo.
        /// </summary>
        public int ComboSkip { get; set; }
        /// <summary>
        /// Indicates that the object is an osu!mania hold note with its end time stored in extras.
        /// </summary>
        public bool IsHoldNote { get; set; }

        /// <summary>
        /// Gets or sets the packed normal, whistle, finish, and clap sample flags.
        /// </summary>
        public int Hitsounds {
            get => GetHitsounds();
            set => SetHitsounds(value);
        }

        /// <summary>
        /// Indicates that the normal sample layer plays; this is bit zero of <see cref="Hitsounds"/>.
        /// </summary>
        public bool Normal { get; set; }
        /// <summary>
        /// Indicates that the whistle addition layer plays.
        /// </summary>
        public bool Whistle { get; set; }
        /// <summary>
        /// Indicates that the finish addition layer plays.
        /// </summary>
        public bool Finish { get; set; }
        /// <summary>
        /// Indicates that the clap addition layer plays.
        /// </summary>
        public bool Clap { get; set; }

        /// <summary>
        /// Gets or sets the colon-separated sample extras, including hold-note end time when applicable.
        /// </summary>
        public string Extras {
            get => GetExtras();
            set => SetExtras(value);
        }

        /// <summary>
        /// Gets or sets the normal-layer sample set; <see cref="SampleSet.None"/> means inherit from timing.
        /// </summary>
        public SampleSet SampleSet { get; set; }
        /// <summary>
        /// Gets or sets the whistle/finish/clap sample set; <see cref="SampleSet.None"/> means inherit.
        /// </summary>
        public SampleSet AdditionSet { get; set; }
        /// <summary>
        /// Gets or sets the custom sample index; zero means inherit from the active timing point.
        /// </summary>
        public int CustomIndex { get; set; }
        /// <summary>
        /// Gets or sets the sample volume percentage; zero means inherit from the active timing point.
        /// </summary>
        public double SampleVolume { get; set; }
        /// <summary>
        /// Gets or sets the optional beatmap-relative custom sample filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// All path types and their index in the curve points array.
        /// Used for preserving multiple path types in osu! lazer file format.
        /// </summary>
        public List<(PathType, int)> AdditionalSliderTypes { get; set; }
        /// <summary>
        /// Gets or sets the primary curve algorithm used to interpret slider control points.
        /// </summary>
        public PathType SliderType { get; set; }
        /// <summary>
        /// Gets or sets slider control points after the object's starting <see cref="Pos"/>.
        /// </summary>
        public List<Vector2> CurvePoints { get; set; }

        /// <summary>
        /// Gets or replaces the geometric path assembled from the start position, curve points, type, and pixel length.
        /// </summary>
        public SliderPath SliderPath {
            get => GetSliderPath();
            set => SetSliderPath(value);
        }

        /// <summary>
        /// Gets or sets the number of slider spans; circles report zero and other non-slider objects report one.
        /// </summary>
        public int Repeat {
            get => IsSlider ? repeat : IsCircle ? 0 : 1;
            set => repeat = value;
        }

        /// <summary>
        /// Gets or sets the requested slider path distance in osu! pixels.
        /// </summary>
        public double PixelLength { get; set; }
        /// <summary>
        /// Gets or sets packed hitsound flags for the slider head, repeat points, and tail.
        /// </summary>
        public List<int> EdgeHitsounds { get; set; }
        /// <summary>
        /// Gets or sets normal-layer sample-set overrides for each slider edge.
        /// </summary>
        public List<SampleSet> EdgeSampleSets { get; set; }
        /// <summary>
        /// Gets or sets addition-layer sample-set overrides for each slider edge.
        /// </summary>
        public List<SampleSet> EdgeAdditionSets { get; set; }

        /// <summary>
        /// Indicates whether slider-edge or object-level sample data must be serialized.
        /// </summary>
        public bool SliderExtras => GetSliderExtras();
        
        /// <summary>
        /// Gets or sets the new-combo state after mode-specific and sequence processing.
        /// </summary>
        [JsonProperty]
        public bool ActualNewCombo { get; set; }
        /// <summary>
        /// Gets or sets the object's zero-based position within its combo.
        /// </summary>
        [JsonProperty]
        public int ComboIndex { get; set; }
        /// <summary>
        /// Gets or sets the resolved index into the beatmap combo-colour palette.
        /// </summary>
        [JsonProperty]
        public int ColourIndex { get; set; }
        /// <summary>
        /// Gets or sets the combo colour resolved by beatmap processing.
        /// </summary>
        [JsonProperty]
        public ComboColour Colour { get; set; }

        /// <summary>
        /// Gets or sets the playable slider length after degenerate-path corrections.
        /// </summary>
        public double TrueLength { get; set; } // Requires more calculation
        /// <summary>
        /// Gets or sets the duration in milliseconds of one slider span or one duration-bearing object.
        /// </summary>
        [JsonProperty]
        public double TemporalLength { get; set; } // Duration of one repeat

        /// <summary>
        /// Gets or adjusts the object's final time in milliseconds.
        /// </summary>
        public double EndTime {
            get => GetEndTime();
            set => SetEndTime(value);
        } // Includes all repeats

        /// <summary>
        /// Calculates the final time from the start, per-span duration, and span count.
        /// </summary>
        /// <param name="floor">Whether to floor the calculated file-format time with an epsilon correction.</param>
        /// <returns><see cref="Time"/> plus one span duration for every <see cref="Repeat"/>.</returns>
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
        /// <summary>
        /// Gets or sets the inherited slider-velocity multiplier encoded by the active greenline.
        /// </summary>
        [JsonProperty]
        public double SliderVelocity { get; set; }
        /// <summary>
        /// Gets or sets the effective timing point used for slider velocity and samples at the object start.
        /// </summary>
        [JsonProperty]
        public TimingPoint TimingPoint { get; set; }
        /// <summary>
        /// Gets or sets the timing point that supplies inherited hitsound settings at the object start.
        /// </summary>
        [JsonProperty]
        public TimingPoint HitsoundTimingPoint { get; set; }
        /// <summary>
        /// Gets or sets the active uninherited timing point that supplies beat length.
        /// </summary>
        [JsonProperty]
        public TimingPoint UnInheritedTimingPoint { get; set; }
        
        /// <summary>
        /// Gets or sets editor selection state; it is not part of the osu! file format.
        /// </summary>
        [JsonProperty]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Timing changes inside the slider body that affect slide, whistle, or tick samples.
        /// </summary>
        public List<TimingPoint> BodyHitsounds = new List<TimingPoint>();
        private int repeat;

        // Special combined with timeline
        /// <summary>
        /// Expanded head, repeat, and tail events derived from this object's gameplay timeline.
        /// </summary>
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
                AdditionalSliderTypes = GetAdditionalPathTypes(sliderData);

                var points = new List<Vector2>();
                foreach (var value in sliderData) {
                    var spl = value.Split(':');

                    // It has to have 2 coordinates inside
                    if (spl.Length != 2) continue;

                    if (TryParseDouble(spl[0], out var ax) && TryParseDouble(spl[1], out var ay))
                        points.Add(new Vector2(ax, ay));
                    else throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", line);
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
                if (AdditionalSliderTypes is not null && AdditionalSliderTypes.Count > 1) {
                    int i = 0;
                    int i2 = 0;
                    bool first = true;
                    foreach (var p in CurvePoints) {
                        while (i2 < AdditionalSliderTypes.Count && AdditionalSliderTypes[i2].Item2 <= i) {
                            if (!first)
                                builder.Append('|');

                            builder.Append(GetPathTypeString(AdditionalSliderTypes[i2].Item1));
                            i++;
                            i2++;
                            first = false;
                        }

                        if (!first)
                            builder.Append('|');

                        builder.Append($"{(SaveWithFloatPrecision ? p.X.ToInvariant() : p.X.ToRoundInvariant())}:{(SaveWithFloatPrecision ? p.Y.ToInvariant() : p.Y.ToRoundInvariant())}");
                        i++;
                        first = false;
                    }
                    while (i2 < AdditionalSliderTypes.Count && AdditionalSliderTypes[i2].Item2 <= i) {
                        if (!first)
                            builder.Append('|');

                        builder.Append(GetPathTypeString(AdditionalSliderTypes[i2].Item1));
                        i++;
                        i2++;
                        first = false;
                    }
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

        /// <summary>
        /// Enumerates slider-body slide, whistle, and tick filenames that can play.
        /// </summary>
        /// <param name="sliderTickRate">The beat subdivisions per span used to place ticks.</param>
        /// <param name="includeDefaults">Whether inherited index-zero filenames are included.</param>
        /// <returns>Filenames in playback occurrence order; duplicates are retained.</returns>
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

        /// <summary>
        /// Calculates tick times across all slider spans, reversing their within-span order on reverse passes.
        /// </summary>
        /// <param name="sliderTickRate">The number of tick intervals per uninherited beat.</param>
        /// <returns>Absolute tick times in milliseconds, excluding ticks within 10 ms of a span end.</returns>
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

        /// <summary>
        /// Expands the object into the times at which timeline objects should exist.
        /// </summary>
        /// <param name="timing">Timing data used to derive slider span duration.</param>
        /// <returns>Circle start; every slider edge; or start and end for spinners and hold notes.</returns>
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
        /// Moves the hit object and its timeline objects by a time offset.
        /// </summary>
        /// <param name="deltaTime">The time offset in milliseconds.</param>
        public void MoveTime(double deltaTime) {
            Time += deltaTime;

            // Move its timelineobjects
            foreach (var tlo in TimelineObjects) tlo.Time += deltaTime;

            BodyHitsounds.RemoveAll(s => s.Offset >= EndTime || s.Offset <= Time);
        }

        /// <summary>
        /// Moves the final edge while keeping the start fixed and updating slider length when necessary.
        /// </summary>
        /// <param name="timing">Timing data used to translate duration changes into slider pixels.</param>
        /// <param name="deltaTime">The desired final-time change in milliseconds.</param>
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

        /// <summary>
        /// Resolves the playable slider length, correcting degenerate and duplicate-ended linear paths.
        /// </summary>
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

        /// <summary>
        /// Recalculates one slider span's duration from its corrected geometry and timing.
        /// </summary>
        /// <param name="timing">The beatmap timing model.</param>
        /// <param name="useOwnSv">Whether to use this object's cached <see cref="SliderVelocity"/> instead of resolving timing again.</param>
        public void CalculateSliderTemporalLength(Timing timing, bool useOwnSv) {
            if (!IsSlider) return;

            CalculateSliderTrueLength();

            TemporalLength = useOwnSv
                ? timing.CalculateSliderTemporalLength(Time, TrueLength, SliderVelocity)
                : timing.CalculateSliderTemporalLength(Time, TrueLength);
        }

        /// <summary>
        /// Changes one span's duration and keeps slider geometry and expanded timeline data consistent.
        /// </summary>
        /// <param name="timing">Timing data used to convert milliseconds to slider pixels.</param>
        /// <param name="deltaTemporalTime">The per-span duration change in milliseconds.</param>
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

        /// <summary>
        /// Repositions existing head, repeat, and tail timeline objects from the current span duration.
        /// </summary>
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
        /// Moves the hit object and all slider control points by a position offset.
        /// </summary>
        /// <param name="delta">The position offset.</param>
        public void Move(Vector2 delta) {
            Pos += delta;
            if (!IsSlider) return;
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = CurvePoints[i] + delta;
        }

        /// <summary>
        /// Apply a 2x2 transformation matrix to the positions and curve points.
        /// </summary>
        /// <param name="mat">The linear transform applied about the coordinate origin.</param>
        public void Transform(Matrix2 mat) {
            Pos = Matrix2.Mult(mat, Pos);
            if (!IsSlider) return;
            for (var i = 0; i < CurvePoints.Count; i++) CurvePoints[i] = Matrix2.Mult(mat, CurvePoints[i]);
        }

        /// <summary>
        /// Snaps the start to the nearest permitted beat subdivision and shifts all derived events by the same amount.
        /// </summary>
        /// <param name="timing">The beatmap timing model.</param>
        /// <param name="beatDivisors">Permitted fractions of a beat.</param>
        /// <param name="floor">Whether the snapped file-format time is floored.</param>
        /// <param name="tp">An optional timing point from which to begin the search.</param>
        /// <param name="firstTp">An optional lower timing boundary.</param>
        /// <returns><see langword="true"/> when the object moved by more than numeric tolerance.</returns>
        public bool ResnapSelf(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = GetResnappedTime(timing, beatDivisors, floor, tp, firstTp);
            var deltaTime = newTime - Time;
            MoveTime(deltaTime);
            return Math.Abs(deltaTime) > Precision.DoubleEpsilon;
        }

        /// <summary>
        /// Snaps the object's end using direct end-time snapping when timing changes occur inside a slider, otherwise using classic duration snapping.
        /// </summary>
        /// <param name="timing">The beatmap timing model.</param>
        /// <param name="beatDivisors">Permitted fractions of a beat.</param>
        /// <param name="floor">Whether direct snapped end times are floored.</param>
        /// <param name="tp">An optional timing point from which to begin the search.</param>
        /// <param name="firstTp">An optional lower timing boundary.</param>
        /// <returns><see langword="true"/> when the end changed by more than numeric tolerance.</returns>
        public bool ResnapEnd(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            // If there is a redline in the sliderbody then the sliderend gets snapped to a tick of the latest redline
            if (!IsSlider || timing.TimingPoints.Any(o => o.Uninherited && o.Offset <= EndTime + 20 && o.Offset > Time))
                return ResnapEndTime(timing, beatDivisors, floor, tp, firstTp);

            return ResnapEndClassic(timing, beatDivisors, firstTp);
        }

        /// <summary>
        /// Snaps the absolute end time, then applies the resulting duration change.
        /// </summary>
        /// <param name="timing">The beatmap timing model.</param>
        /// <param name="beatDivisors">Permitted fractions of a beat.</param>
        /// <param name="floor">Whether the snapped file-format time is floored.</param>
        /// <param name="tp">An optional timing point from which to begin the search.</param>
        /// <param name="firstTp">An optional lower timing boundary.</param>
        /// <returns><see langword="true"/> when the end changed by more than numeric tolerance.</returns>
        public bool ResnapEndTime(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = timing.Resnap(EndTime, beatDivisors, floor, tp: tp, firstTp: firstTp);

            var deltaTime = newTime - EndTime;
            MoveEndTime(timing, deltaTime);

            return Math.Abs(deltaTime) > Precision.DoubleEpsilon;
        }

        /// <summary>
        /// Snaps one span's duration relative to the object's start, preserving the slider's timing model.
        /// </summary>
        /// <param name="timing">The beatmap timing model.</param>
        /// <param name="beatDivisors">Permitted fractions of a beat.</param>
        /// <param name="firstTp">An optional lower timing boundary.</param>
        /// <returns><see langword="true"/> when the span duration changed by more than numeric tolerance.</returns>
        public bool ResnapEndClassic(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, TimingPoint firstTp = null) {
            var newTemporalLength = timing.ResnapDuration(Time, TemporalLength, beatDivisors, false, firstTp: firstTp);

            var deltaTime = newTemporalLength - TemporalLength;
            ChangeTemporalTime(timing, deltaTime);

            return Math.Abs(deltaTime) > Precision.DoubleEpsilon;
        }

        /// <summary>
        /// Centers an osu!mania object in its nearest column and on the standard vertical coordinate.
        /// </summary>
        /// <param name="mode">The beatmap mode; only mania objects are changed.</param>
        /// <param name="circleSize">The mania key count encoded by circle size.</param>
        /// <returns><see langword="true"/> when the position changed.</returns>
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

        /// <summary>
        /// Calculates the snapped start time without mutating the object.
        /// </summary>
        /// <param name="timing">The beatmap timing model.</param>
        /// <param name="beatDivisors">Permitted fractions of a beat.</param>
        /// <param name="floor">Whether the result is floored to file-format milliseconds.</param>
        /// <param name="tp">An optional timing point from which to begin the search.</param>
        /// <param name="firstTp">An optional lower timing boundary.</param>
        /// <returns>The nearest permitted start time in milliseconds.</returns>
        public double GetResnappedTime(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            return timing.Resnap(Time, beatDivisors, floor, tp: tp, firstTp: firstTp);
        }

        private bool GetSliderExtras() {
            var hitsounds = GetHitsounds();
            return EdgeHitsounds != null && EdgeHitsounds.Any(o => o != hitsounds) ||
                   EdgeSampleSets != null && EdgeSampleSets.Any(o => o != SampleSet.None) ||
                   EdgeAdditionSets != null && EdgeAdditionSets.Any(o => o != SampleSet.None) ||
                   SampleSet != SampleSet.None || AdditionSet != SampleSet.None || CustomIndex != 0 || 
                   Math.Abs(SampleVolume) > Precision.DoubleEpsilon || !string.IsNullOrEmpty(Filename);
        }

        /// <summary>
        /// Serializes the object as an osu! hit-object line.
        /// </summary>
        /// <returns>The same representation as <see cref="GetLine"/>.</returns>
        public override string ToString() {
            return GetLine();
        }

        /// <summary>
        /// Packs gameplay kind, new-combo state, and combo-skip count into the osu! type bit field.
        /// </summary>
        /// <returns>The integer written in the hit-object type column.</returns>
        public int GetObjectType() {
            var cs = new BitArray(new[] {ComboSkip});
            return MathHelper.GetIntFromBitArray(new BitArray(new[]
                {IsCircle, IsSlider, NewCombo, IsSpinner, cs[0], cs[1], cs[2], IsHoldNote}));
        }

        /// <summary>
        /// Decodes an osu! type bit field into gameplay, combo, and combo-skip properties.
        /// </summary>
        /// <param name="type">The packed integer from the hit-object type column.</param>
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

        /// <summary>
        /// Selects one gameplay kind while clearing all other kind flags.
        /// </summary>
        /// <param name="type">The gameplay object kind.</param>
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

        /// <summary>
        /// Packs the four sample-layer flags into the osu! hitsound bit field.
        /// </summary>
        /// <returns>The integer written in the hit-object hitsound column.</returns>
        public int GetHitsounds() {
            return MathHelper.GetIntFromBitArray(new BitArray(new[] {Normal, Whistle, Finish, Clap}));
        }

        /// <summary>
        /// Decodes an osu! hitsound bit field into its normal, whistle, finish, and clap flags.
        /// </summary>
        /// <param name="hitsounds">The packed integer from the hit-object hitsound column.</param>
        public void SetHitsounds(int hitsounds) {
            var b = new BitArray(new[] {hitsounds});
            Normal = b[0];
            Whistle = b[1];
            Finish = b[2];
            Clap = b[3];
        }

        /// <summary>
        /// Serializes object-level sample overrides, prefixing the end time for mania hold notes.
        /// </summary>
        /// <returns>The colon-separated extras field used by the osu! file format.</returns>
        public string GetExtras() {
            if (IsHoldNote)
                return string.Join(":", SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant(), SampleSet.ToIntInvariant(),
                    AdditionSet.ToIntInvariant(), CustomIndex.ToInvariant(), SampleVolume.ToRoundInvariant(), Filename);
            return string.Join(":", SampleSet.ToIntInvariant(), AdditionSet.ToIntInvariant(), CustomIndex.ToInvariant(),
                SampleVolume.ToRoundInvariant(), Filename);
        }

        /// <summary>
        /// Parses object-level sample overrides and the optional mania hold-note end time.
        /// </summary>
        /// <param name="extras">The colon-separated extras field from a hit-object line.</param>
        /// <exception cref="BeatmapParsingException">A required numeric field is malformed.</exception>
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

        /// <summary>
        /// Resets sample overrides to inherited defaults and initializes hold-note duration when applicable.
        /// </summary>
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

        /// <summary>
        /// Builds a geometric path from the object's complete control-point list.
        /// </summary>
        /// <param name="fullLength">When true, do not truncate or extend the path to <see cref="PixelLength"/>.</param>
        /// <returns>A newly constructed slider path.</returns>
        public SliderPath GetSliderPath(bool fullLength = false) {
            return fullLength
                ? new SliderPath(SliderType, GetAllCurvePoints().ToArray())
                : new SliderPath(SliderType, GetAllCurvePoints().ToArray(), PixelLength);
        }

        /// <summary>
        /// Replaces slider type, control points, and pixel length from a geometric path.
        /// </summary>
        /// <param name="sliderPath">The path whose values become this object's serialized slider data.</param>
        public void SetSliderPath(SliderPath sliderPath) {
            var controlPoints = sliderPath.ControlPoints;
            SetAllCurvePoints(controlPoints);
            SliderType = sliderPath.Type;
            PixelLength = sliderPath.Distance;
        }

        /// <summary>
        /// Combines the object start position with its remaining slider control points.
        /// </summary>
        /// <returns>A new list whose first item is <see cref="Pos"/>.</returns>
        public List<Vector2> GetAllCurvePoints() {
            var controlPoints = new List<Vector2> {Pos};
            controlPoints.AddRange(CurvePoints);
            return controlPoints;
        }

        /// <summary>
        /// Splits a complete slider control-point list into start position and trailing curve points.
        /// </summary>
        /// <param name="controlPoints">A non-empty list whose first point becomes <see cref="Pos"/>.</param>
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
            switch (pathType) {
                case PathType.Linear:
                    return "L";
                case PathType.PerfectCurve:
                    return "P";
                case PathType.Catmull:
                    return "C";
                case PathType.Bezier:
                    return "B";
                case PathType.BSpline:
                    return "B4";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Detects a failure in the slider path algorithm causing a slider to become invisible.
        /// </summary>
        /// <returns><see langword="true"/> for zero-area, NaN-length, or effectively zero-length slider geometry.</returns>
        public bool IsInvisible() {
            return PixelLength != 0 && PixelLength <= 0.0001 ||
                   double.IsNaN(PixelLength) ||
                   CurvePoints.All(o => o == Pos);
        }

        /// <summary>
        /// Copies the hit object and duplicates all mutable nested timing, geometry, edge, timeline, and colour data.
        /// </summary>
        /// <returns>An independently mutable hit object.</returns>
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

        /// <summary>
        /// Writes the serialized object and its derived body/timeline hitsound state to standard output.
        /// </summary>
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

        /// <summary>
        /// Orders objects chronologically, placing new-combo objects before other objects at the same time.
        /// </summary>
        /// <param name="other">The object to compare, or <see langword="null"/>.</param>
        /// <returns>A standard sort value; any instance sorts after <see langword="null"/>.</returns>
        public int CompareTo(HitObject other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            if (Time == other.Time) return other.NewCombo.CompareTo(NewCombo);
            return Time.CompareTo(other.Time);
        }
    }
}
