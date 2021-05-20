using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using static Mapping_Tools.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class TimingPoint : ITextLine, IComparable<TimingPoint> {
        // Offset, Milliseconds per Beat, Meter, Sample Set, Sample Index, Volume, Inherited, Kiai Mode
        /// <summary>
        /// The millisecond value of the timing point.
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// Milliseconds per Beat
        /// </summary>
        public double MpB { get; set; }

        /// <summary>
        /// Time signature to x/4
        /// </summary>
        public TempoSignature Meter { get; set; }

        /// <summary>
        /// The sample set from the <see cref="TimingPoint"/>
        /// </summary>
        public SampleSet SampleSet { get; set; }

        /// <summary>
        /// The custom index number from the <see cref="TimingPoint"/>
        /// </summary>
        public int SampleIndex { get; set; }

        /// <summary>
        /// The volume based from 0 - 100 %
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// An instance of the <see cref="TimingPoint"/> 
        /// that does not rely on the previous timing point 
        /// and instead creates a new Bpm, offset, and/or time signature change to the timing section.
        /// <para/>
        /// True for Uninherited control points. False, for Inherited control points.
        /// </summary>
        public bool Uninherited { get; set; }

        /// <summary>
        /// A special section which represents a chorus or big moment within the song.
        /// </summary>
        public bool Kiai { get; set; }

        /// <summary>
        /// A taiko implementation that removes the first instance of the bar,
        /// it is used when multiple and/or conflicting timing points are used throughout the map.
        /// <para/>
        /// It can also be utilised for the Nightcore mod of standard by removing a finish sample at the timing point.
        /// </summary>
        public bool OmitFirstBarLine { get; set; }

        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        [JsonIgnore]
        public bool SaveWithFloatPrecision { get; set; }

        /// <summary>
        /// Creates a new <see cref="TimingPoint"/>
        /// </summary>
        /// <param name="offset">The offset from the start of the audio in milliseconds</param>
        /// <param name="mpb">The milliseconds per beat. (Quarter Note in Music Theory terms.) </param>
        /// <param name="meter">The time signature in x / 4</param>
        /// <param name="sampleSet">The <see cref="SampleSet"/> that is used from the timing point</param>
        /// <param name="sampleIndex"></param>
        /// <param name="volume"></param>
        /// <param name="uninherited"></param>
        /// <param name="kiai"></param>
        /// <param name="omitFirstBarLine"></param>
        public TimingPoint(double offset, double mpb, int meter, SampleSet sampleSet, int sampleIndex, double volume, bool uninherited, bool kiai, bool omitFirstBarLine) {
            Offset = offset;
            MpB = mpb;
            Meter = new TempoSignature(meter);
            SampleSet = sampleSet;
            SampleIndex = sampleIndex;
            Volume = volume;
            Uninherited = uninherited;
            Kiai = kiai;
            OmitFirstBarLine = omitFirstBarLine;
        }

        /// <summary>
        /// Creates a new <see cref="TimingPoint"/>
        /// </summary>
        /// <param name="offset">The offset from the start of the audio in milliseconds</param>
        /// <param name="mpb">The milliseconds per beat. (Quarter Note in Music Theory terms.) </param>
        /// <param name="meter">The tempo signature object.</param>
        /// <param name="sampleSet">The <see cref="SampleSet"/> that is used from the timing point</param>
        /// <param name="sampleIndex"></param>
        /// <param name="volume"></param>
        /// <param name="uninherited"></param>
        /// <param name="kiai"></param>
        /// <param name="omitFirstBarLine"></param>
        public TimingPoint(double offset, double mpb, TempoSignature meter, SampleSet sampleSet, int sampleIndex, double volume, bool uninherited, bool kiai, bool omitFirstBarLine)
        {
            Offset = offset;
            MpB = mpb;
            Meter = meter;
            SampleSet = sampleSet;
            SampleIndex = sampleIndex;
            Volume = volume;
            Uninherited = uninherited;
            Kiai = kiai;
            OmitFirstBarLine = omitFirstBarLine;
        }

        /// <summary>
        /// Creates a new Timing Point from the <see cref="Editor_Reader.ControlPoint"/>.
        /// </summary>
        /// <param name="cp">The control point value from <see cref="Editor_Reader"/></param>
        public TimingPoint(Editor_Reader.ControlPoint cp) {
            MpB = cp.BeatLength;
            Offset = cp.Offset;
            SampleIndex = cp.CustomSamples;
            SampleSet = (SampleSet)cp.SampleSet;
            Meter = new TempoSignature(cp.TimeSignature);
            Volume = cp.Volume;
            Kiai = (cp.EffectFlags & 1) > 0;
            OmitFirstBarLine = (cp.EffectFlags & 8) > 0;
            Uninherited = cp.TimingChange;
        }

        /// <summary>
        /// Creates a new Timing Point from the <see cref="Editor_Reader.ControlPoint"/>
        /// </summary>
        /// <param name="cp">A <see cref="Editor_Reader.ControlPoint"/> from the <see cref="Beatmap"/>.</param>
        /// <returns></returns>
        public static explicit operator TimingPoint(Editor_Reader.ControlPoint cp) {
            return new TimingPoint(cp);
        }

        /// <summary>
        /// Creates a new Timing Point from the string line of the .osu file.
        /// </summary>
        /// <param name="line"></param>
        public TimingPoint(string line) {
            SetLine(line);
        }

        public TimingPoint()
        {
            MpB = 60000;
            Offset = 0;
            Meter = new TempoSignature(4,4);
            SampleSet = new SampleSet();
            SampleIndex = 0;
            Volume = 100;
            Uninherited = false;
            Kiai = false;
            OmitFirstBarLine = false;

        }


        /// <summary>
        /// Generates the line from the selected <see cref="TimingPoint"/>
        /// </summary>
        /// <returns></returns>
        public string GetLine() {
            int style = MathHelper.GetIntFromBitArray(new BitArray(new[] { Kiai, false, false, OmitFirstBarLine }));
            return $"{(SaveWithFloatPrecision ? Offset.ToInvariant() : Offset.ToRoundInvariant())},{MpB.ToInvariant()},{Meter.TempoNumerator.ToInvariant()},{SampleSet.ToIntInvariant()},{SampleIndex.ToInvariant()},{(SaveWithFloatPrecision ? Volume.ToInvariant() : Volume.ToRoundInvariant())},{Convert.ToInt32(Uninherited).ToInvariant()},{style.ToInvariant()}";
        }

        /// <summary>
        /// Sets a <see cref="TimingPoint"/> from the line of the beatmap file.
        /// </summary>
        /// <param name="line"></param>
        /// <exception cref="BeatmapParsingException">If the beatmap can not be read correctly.</exception>
        public void SetLine(string line) {
            string[] values = line.Split(',');

            if (TryParseDouble(values[0], out double offset))
                Offset = offset;
            else throw new BeatmapParsingException("Failed to parse offset of timing point", line);

            if (TryParseDouble(values[1], out double mpb))
                MpB = mpb;
            else throw new BeatmapParsingException("Failed to parse milliseconds per beat of timing point", line);

            if (TryParseInt(values[2], out int meter))
                Meter = new TempoSignature(meter);
            else throw new BeatmapParsingException("Failed to parse meter of timing point", line);

            if (Enum.TryParse(values[3], out SampleSet ss))
                SampleSet = ss;
            else throw new BeatmapParsingException("Failed to parse sampleset of timing point", line);

            if (TryParseInt(values[4], out int ind))
                SampleIndex = ind;
            else throw new BeatmapParsingException("Failed to parse sample index of timing point", line);

            if (TryParseDouble(values[5], out double vol))
                Volume = vol;
            else throw new BeatmapParsingException("Failed to parse volume of timing point", line);

            Uninherited = values[6] == "1";

            if (values.Length <= 7) return;
            if (TryParseInt(values[7], out int style)) {
                BitArray b = new BitArray(new int[] { style });
                Kiai = b[0];
                OmitFirstBarLine = b[3];
            } else throw new BeatmapParsingException("Failed to style of timing point", line);
        }

        /// <summary>
        /// Creates a new <see cref="TimingPoint"/> from the selected <see cref="TimingPoint"/>.
        /// </summary>
        /// <returns>An exact replica of the <see cref="TimingPoint"/></returns>
        public TimingPoint Copy() {
            return new TimingPoint(Offset, MpB, Meter, SampleSet, SampleIndex, Volume, Uninherited, Kiai, OmitFirstBarLine);
        }

        /// <summary>
        /// Can clarify if the current timing point should snap to the nearest beat of the previous timing point.
        /// </summary>
        /// <param name="timing"></param>
        /// <param name="snap1"></param>
        /// <param name="snap2"></param>
        /// <param name="floor"></param>
        /// <param name="tp"></param>
        /// <param name="firstTP"></param>
        /// <returns></returns>
        public bool ResnapSelf(Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor=true, TimingPoint tp=null, TimingPoint firstTP = null) {
            double newTime = timing.Resnap(Offset, beatDivisors, floor, tp: tp, firstTp: firstTP);
            double deltaTime = newTime - Offset;
            Offset += deltaTime;
            return deltaTime != 0;
        }

        public bool Equals(TimingPoint tp) {
            return Offset == tp.Offset &&
                MpB == tp.MpB &&
                Meter == tp.Meter &&
                SampleSet == tp.SampleSet &&
                SampleIndex == tp.SampleIndex &&
                Volume == tp.Volume &&
                Uninherited == tp.Uninherited &&
                Kiai == tp.Kiai &&
                OmitFirstBarLine == tp.OmitFirstBarLine;
        }

        public bool SameEffect(TimingPoint tp) {
            if (tp.Uninherited && !Uninherited) {
                return MpB == -100 &&
                       Meter == tp.Meter &&
                       SampleSet == tp.SampleSet &&
                       SampleIndex == tp.SampleIndex &&
                       Volume == tp.Volume &&
                       Kiai == tp.Kiai;
            }
            return MpB == tp.MpB &&
                   Meter == tp.Meter &&
                   SampleSet == tp.SampleSet &&
                   SampleIndex == tp.SampleIndex &&
                   Volume == tp.Volume &&
                   Kiai == tp.Kiai;
        }

        /// <summary>
        /// Grabs the current Beats Per Minute from the <see cref="TimingPoint"/>
        /// </summary>
        /// <returns></returns>
        public double GetBpm() {
            if( Uninherited ) {
                return 60000 / MpB;
            }

            return -100 / MpB;
        }

        public void SetBpm(double bpm) {
            if (Uninherited) {
                MpB = 60000 / bpm;
            } else {
                MpB = -100 / bpm;
            }
        }

        public int CompareTo(TimingPoint other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var offsetComparison = Offset.CompareTo(other.Offset);
            if (offsetComparison != 0) return offsetComparison;
            return -Uninherited.CompareTo(other.Uninherited);
        }
    }
}
