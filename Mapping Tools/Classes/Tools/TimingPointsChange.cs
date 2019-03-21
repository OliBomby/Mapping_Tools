using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.Tools {
    public struct TimingPointsChange {

        public TimingPoint TP;
        public bool MpB;
        public bool Meter;
        public bool Sampleset;
        public bool Index;
        public bool Volume;
        public bool Inherited;
        public bool Kiai;

        public TimingPointsChange(TimingPoint tpNew, bool mpb = false, bool meter = false, bool sampleset = false, bool index = false, bool volume = false, bool inherited = false, bool kiai = false) {
            TP = tpNew;
            MpB = mpb;
            Meter = meter;
            Sampleset = sampleset;
            Index = index;
            Volume = volume;
            Inherited = inherited;
            Kiai = kiai;
        }

        public void AddChange(List<TimingPoint> list, Timing timing, bool allAfter=false) {
            TimingPoint prev = null;
            TimingPoint on = null;
            foreach (TimingPoint tp in list) {
                if (tp == null) {
                    continue;
                }
                if (prev == null) {
                    if (tp.Offset < TP.Offset) {
                        prev = tp;
                    }
                } else if (tp.Offset >= prev.Offset && tp.Offset < TP.Offset) {
                    prev = tp;
                }
                if (tp.Offset == TP.Offset) {
                    if (tp.Inherited && MpB) {
                        prev = tp;
                    } else {
                        on = tp;
                    }
                }
            }

            if (on != null) {
                if (MpB) { on.MpB = TP.MpB; }
                if (Meter) { on.Meter = TP.Meter; }
                if (Sampleset) { on.SampleSet = TP.SampleSet; }
                if (Index) { on.SampleIndex = TP.SampleIndex; }
                if (Volume) { on.Volume = TP.Volume; }
                if (Inherited) { on.Inherited = TP.Inherited; }
                if (Kiai) { on.Kiai = TP.Kiai; }
            } else {
                if (prev != null) {
                    // Make new timingpoint
                    if (prev.Inherited) {
                        on = new TimingPoint(TP.Offset, -100, prev.Meter, prev.SampleSet, prev.SampleIndex, prev.Volume, false, prev.Kiai);
                    } else {
                        on = new TimingPoint(TP.Offset, prev.MpB, prev.Meter, prev.SampleSet, prev.SampleIndex, prev.Volume, false, prev.Kiai);
                    }
                    if (MpB) { on.MpB = TP.MpB; }
                    if (Meter) { on.Meter = TP.Meter; }
                    if (Sampleset) { on.SampleSet = TP.SampleSet; }
                    if (Index) { on.SampleIndex = TP.SampleIndex; }
                    if (Volume) { on.Volume = TP.Volume; }
                    if (Inherited) { on.Inherited = TP.Inherited; }
                    if (Kiai) { on.Kiai = TP.Kiai; }

                    if (!on.Equals(prev) || Inherited) {
                        list.Add(on);
                    }
                } else {
                    list.Add(TP);
                }
            }

            if (allAfter) // Change every timingpoint after
            {
                foreach (TimingPoint tp in list) {
                    if (tp.Offset > TP.Offset) {
                        if (Sampleset) { tp.SampleSet = TP.SampleSet; }
                        if (Index) { tp.SampleIndex = TP.SampleIndex; }
                        if (Volume) { tp.Volume = TP.Volume; }
                        if (Kiai) { tp.Kiai = TP.Kiai; }
                    }
                }
            }
        }
    }
}
