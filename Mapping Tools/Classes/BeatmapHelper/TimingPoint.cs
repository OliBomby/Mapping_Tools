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
            BitArray b = new BitArray(new int[] { int.Parse(values[7]) });

            Offset = ParseDouble(values[0]);
            MpB = ParseDouble(values[1]);
            Meter = int.Parse(values[2]);
            SampleSet = (SampleSet)int.Parse(values[3]);
            SampleIndex = int.Parse(values[4]);
            Volume = ParseDouble(values[5]);
            Inherited = values[6] == "1";
            Kiai = b[0];
            OmitFirstBarLine = b[3];
        }

        public TimingPoint Copy() {
            return new TimingPoint(Offset, MpB, Meter, SampleSet, SampleIndex, Volume, Inherited, Kiai, OmitFirstBarLine);
        }

        public bool ResnapSelf(Timing timing, int snap1, int snap2, bool floor=true, TimingPoint tp=null) {
            double newTime = timing.Resnap(Offset, snap1, snap2, floor, tp);
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

        private double ParseDouble(string d) {
            return double.Parse(d, CultureInfo.InvariantCulture);
        }
    }
}
