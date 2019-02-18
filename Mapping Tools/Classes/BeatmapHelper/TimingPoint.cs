using System;
using System.Globalization;

namespace Mapping_Tools.classes.BeatmapHelper {
    class TimingPoint {
        // Offset, Milliseconds per Beat, Meter, Sample Set, Sample Index, Volume, Inherited, Kiai Mode
        public double Offset { get; set; }
        public double MpB { get; set; }
        public int Meter { get; set; }
        public int SampleSet { get; set; }
        public int SampleIndex { get; set; }
        public double Volume { get; set; }
        public bool Inherited { get; set; } // True is red line
        public bool Kiai { get; set; }
        public TimingPoint(double Offset, double MpB, int Meter, int SampleSet, int SampleIndex, double Volume, bool Inherited, bool Kiai) {
            this.Offset = Offset;
            this.MpB = MpB;
            this.Meter = Meter;
            this.SampleSet = SampleSet;
            this.SampleIndex = SampleIndex;
            this.Volume = Volume;
            this.Inherited = Inherited;
            this.Kiai = Kiai;
        }

        public string GetLine() {
            return Math.Round(Offset) + "," + MpB.ToString(CultureInfo.InvariantCulture) + "," + Meter + "," + SampleSet + "," + SampleIndex + ","
                + Math.Round(Volume) + "," + Convert.ToInt32(Inherited) + "," + Convert.ToInt32(Kiai);
        }

        public TimingPoint Copy() {
            return new TimingPoint(Offset, MpB, Meter, SampleSet, SampleIndex, Volume, Inherited, Kiai);
        }

        public bool ResnapSelf(Timing timing, int snap1, int snap2) {
            double newTime = Math.Floor(timing.Resnap(Offset, snap1, snap2));
            double deltaTime = newTime - Offset;
            Offset += deltaTime;
            return deltaTime != 0;
        }

        public bool Equals(TimingPoint tp) {
            if( tp.Inherited && !Inherited ) {
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
    }
}
