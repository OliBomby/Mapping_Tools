using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Globalization;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class TimingPoint : ITextLine {
        // Offset, Milliseconds per Beat, Meter, Sample Set, Sample Index, Volume, Inherited, Kiai Mode
        public double Offset { get; set; }
        public double MpB { get; set; }
        public int Meter { get; set; }
        public SampleSet SampleSet { get; set; }
        public int SampleIndex { get; set; }
        public double Volume { get; set; }
        public bool Inherited { get; set; } // True is red line
        public bool Kiai { get; set; }
        public bool OmitFirstBarLine { get; set; }

        public TimingPoint(double Offset, double MpB, int Meter, SampleSet SampleSet, int SampleIndex, double Volume, bool Inherited, bool Kiai, bool OmitFirstBarLine) {
            this.Offset = Offset;
            this.MpB = MpB;
            this.Meter = Meter;
            this.SampleSet = SampleSet;
            this.SampleIndex = SampleIndex;
            this.Volume = Volume;
            this.Inherited = Inherited;
            this.Kiai = Kiai;
            this.OmitFirstBarLine = OmitFirstBarLine;
        }

        public TimingPoint(double Offset, double MpB, int Meter, int SampleSet, int SampleIndex, double Volume, bool Inherited, int effects) {
            this.Offset = Offset;
            this.MpB = MpB;
            this.Meter = Meter;
            this.SampleSet = (SampleSet)SampleSet;
            this.SampleIndex = SampleIndex;
            this.Volume = Volume;
            this.Inherited = Inherited;
            BitArray b = new BitArray(new int[] { effects });
            Kiai = b[0];
            OmitFirstBarLine = b[3];
        }

        public TimingPoint(string line) {
            SetLine(line);
        }

        public string GetLine() {
            int style = MathHelper.GetIntFromBitArray(new BitArray(new bool[] { Kiai, false, false, OmitFirstBarLine }));
            return Math.Round(Offset) + "," + MpB.ToString(CultureInfo.InvariantCulture) + "," + Meter + "," + (int)SampleSet + "," + SampleIndex + ","
                + Math.Round(Volume) + "," + Convert.ToInt32(Inherited) + "," + style;
        }

        public void SetLine(string line) {
            string[] values = line.Split(',');

            if (TryParseDouble(values[0], out double offset))
                Offset = offset;
            else throw new BeatmapParsingException("Failed to parse offset of timing point", line);

            if (TryParseDouble(values[1], out double mpb))
                MpB = mpb;
            else throw new BeatmapParsingException("Failed to parse milliseconds per beat of timing point", line);

            if (int.TryParse(values[2], out int meter))
                Meter = meter;
            else throw new BeatmapParsingException("Failed to parse meter of timing point", line);

            if (Enum.TryParse(values[3], out SampleSet ss))
                SampleSet = ss;
            else throw new BeatmapParsingException("Failed to parse sampleset of timing point", line);

            if (int.TryParse(values[4], out int ind))
                SampleIndex = ind;
            else throw new BeatmapParsingException("Failed to parse samle index of timing point", line);

            if (TryParseDouble(values[5], out double vol))
                Volume = vol;
            else throw new BeatmapParsingException("Failed to parse volume of timing point", line);

            Inherited = values[6] == "1";

            if (int.TryParse(values[7], out int style)) {
                BitArray b = new BitArray(new int[] { style });
                Kiai = b[0];
                OmitFirstBarLine = b[3];
            } else throw new BeatmapParsingException("Failed to style of timing point", line);
        }

        public TimingPoint Copy() {
            return new TimingPoint(Offset, MpB, Meter, SampleSet, SampleIndex, Volume, Inherited, Kiai, OmitFirstBarLine);
        }

        public bool ResnapSelf(Timing timing, int snap1, int snap2, bool floor=true, TimingPoint tp=null, TimingPoint firstTP = null) {
            double newTime = timing.Resnap(Offset, snap1, snap2, floor, tp, firstTP);
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
                Inherited == tp.Inherited &&
                Kiai == tp.Kiai &&
                OmitFirstBarLine == OmitFirstBarLine;
        }

        public bool SameEffect(TimingPoint tp) {
            if (tp.Inherited && !Inherited) {
                return MpB == -100 && Meter == tp.Meter && SampleSet == tp.SampleSet && SampleIndex == tp.SampleIndex && Volume == tp.Volume && Kiai == tp.Kiai;
            }
            return MpB == tp.MpB && Meter == tp.Meter && SampleSet == tp.SampleSet && SampleIndex == tp.SampleIndex && Volume == tp.Volume && Kiai == tp.Kiai;
        }

        public double GetBPM() {
            if( Inherited ) {
                return 60000 / MpB;
            }
            else {
                return -100 / MpB;
            }
        }

        private bool TryParseDouble(string d, out double result) {
            return double.TryParse(d, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
