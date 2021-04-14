using System;
using System.Collections;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper.TimingStuff {
    public class TimingPoint : IComparable<TimingPoint> {
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
        /// StartTime signature to x/4
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
        public override string ToString() {
            int style = MathHelper.GetIntFromBitArray(new BitArray(new[] { Kiai, false, false, OmitFirstBarLine }));
            return $"{Offset.ToInvariant()},{MpB.ToInvariant()},{Meter.TempoNumerator.ToInvariant()},{SampleSet.ToIntInvariant()},{SampleIndex.ToInvariant()},{Volume.ToInvariant()},{Convert.ToInt32(Uninherited).ToInvariant()},{style.ToInvariant()}";
        }

        /// <summary>
        /// Creates a new <see cref="TimingPoint"/> from the selected <see cref="TimingPoint"/>.
        /// </summary>
        /// <returns>An exact replica of the <see cref="TimingPoint"/></returns>
        public TimingPoint Copy() {
            return new TimingPoint(Offset, MpB, Meter, SampleSet, SampleIndex, Volume, Uninherited, Kiai, OmitFirstBarLine);
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
        /// assuming this is an uninherited timing point.
        /// </summary>
        /// <returns></returns>
        public double GetBpm() {
            if( Uninherited ) {
                return 60000 / MpB;
            }

            throw new InvalidOperationException("Cannot get BPM from an inherited timingpoint.");
        }

        /// <summary>
        /// Sets the current Beats Per Minute of the <see cref="TimingPoint"/>
        /// assuming this is an uninherited timing point.
        /// </summary>
        /// <returns></returns>
        public void SetBpm(double bpm) {
            if (Uninherited) {
                MpB = 60000 / bpm;
            } else {
                throw new InvalidOperationException("Cannot set BPM on an inherited timingpoint.");
            }
        }

        /// <summary>
        /// Grabs the current slider velocity multiplier from the <see cref="TimingPoint"/>
        /// assuming this is an inherited timing point.
        /// Returns 1x slider velocity if this is an uninherited timing point.
        /// </summary>
        /// <returns></returns>
        public double GetSliderVelocity() {
            if (!Uninherited) {
                return -100 / MpB;
            }

            return 1;
        }

        /// <summary>
        /// Sets the current slider velocity multiplier of the <see cref="TimingPoint"/>
        /// assuming this is an inherited timing point.
        /// </summary>
        /// <returns></returns>
        public void SetSliderVelocity(double bpm) {
            if (!Uninherited) {
                MpB = -100 / bpm;
            } else {
                throw new InvalidOperationException("Cannot set slider velocity on an uninherited timingpoint.");
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
