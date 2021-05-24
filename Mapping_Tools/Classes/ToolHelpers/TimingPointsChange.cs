using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.ToolHelpers {
    public struct TimingPointsChange {

        public TimingPoint MyTP;
        public bool MpB;
        public bool Meter;
        public bool Sampleset;
        public bool Index;
        public bool Volume;
        public bool UnInherited;
        public bool Kiai;
        public bool OmitFirstBarLine;
        public double Fuzzyness;

        public TimingPointsChange(TimingPoint tpNew, bool mpb = false, bool meter = false, bool sampleset = false, bool index = false, bool volume = false, bool unInherited = false, bool kiai = false, bool omitFirstBarLine = false, double fuzzyness=2) {
            MyTP = tpNew;
            MpB = mpb;
            Meter = meter;
            Sampleset = sampleset;
            Index = index;
            Volume = volume;
            UnInherited = unInherited;
            Kiai = kiai;
            OmitFirstBarLine = omitFirstBarLine;
            Fuzzyness = fuzzyness;
        }

        public void AddChange(Timing timing, bool allAfter = false) {
            TimingPoint addingTimingPoint = null;
            TimingPoint prevTimingPoint = null;
            List<TimingPoint> onTimingPoints = new List<TimingPoint>();
            bool onHasRed = false;
            bool onHasGreen = false;

            foreach (TimingPoint tp in timing) {
                if (tp == null) { continue; }  // Continue nulls to avoid exceptions
                if (tp.Offset < MyTP.Offset && (prevTimingPoint == null || tp.Offset >= prevTimingPoint.Offset)) {
                    prevTimingPoint = tp;
                }
                if (Math.Abs(tp.Offset - MyTP.Offset) <= Fuzzyness) {
                    onTimingPoints.Add(tp);
                    onHasRed = tp.Uninherited || onHasRed;
                    onHasGreen = !tp.Uninherited || onHasGreen;
                }
            }

            if (onTimingPoints.Count > 0) {
                prevTimingPoint = onTimingPoints.Last();
            }

            if (UnInherited && !onHasRed) {
                // Make new redline
                if (prevTimingPoint == null) {
                    addingTimingPoint = MyTP.Copy();
                    addingTimingPoint.Uninherited = true;
                } else {
                    addingTimingPoint = prevTimingPoint.Copy();
                    addingTimingPoint.Offset = MyTP.Offset;
                    addingTimingPoint.Uninherited = true;
                }
                onTimingPoints.Add(addingTimingPoint);
            }
            if (!UnInherited && (onTimingPoints.Count == 0 || (MpB && !onHasGreen))) {
                // Make new greenline (based on prev)
                if (prevTimingPoint == null) {
                    addingTimingPoint = MyTP.Copy();
                    addingTimingPoint.Uninherited = false;
                } else {
                    addingTimingPoint = prevTimingPoint.Copy();
                    addingTimingPoint.Offset = MyTP.Offset;
                    addingTimingPoint.Uninherited = false;
                    if (prevTimingPoint.Uninherited) { addingTimingPoint.MpB = -100; }
                }
                onTimingPoints.Add(addingTimingPoint);
            }

            foreach (TimingPoint on in onTimingPoints) {
                if (MpB && (UnInherited ? on.Uninherited : !on.Uninherited)) { on.MpB = MyTP.MpB; }
                if (Meter && UnInherited && on.Uninherited) { on.Meter = MyTP.Meter; }
                if (Sampleset) { on.SampleSet = MyTP.SampleSet; }
                if (Index) { on.SampleIndex = MyTP.SampleIndex; }
                if (Volume) { on.Volume = MyTP.Volume; }
                if (Kiai) { on.Kiai = MyTP.Kiai; }
                if (OmitFirstBarLine && UnInherited && on.Uninherited) { on.OmitFirstBarLine = MyTP.OmitFirstBarLine; }
            }

            if (addingTimingPoint != null && (prevTimingPoint == null || !addingTimingPoint.SameEffect(prevTimingPoint) || UnInherited)) {
                timing.Add(addingTimingPoint);
            }

            if (allAfter) // Change every timingpoint after
            {
                foreach (TimingPoint tp in timing) {
                    if (tp.Offset > MyTP.Offset) {
                        if (Sampleset) { tp.SampleSet = MyTP.SampleSet; }
                        if (Index) { tp.SampleIndex = MyTP.SampleIndex; }
                        if (Volume) { tp.Volume = MyTP.Volume; }
                        if (Kiai) { tp.Kiai = MyTP.Kiai; }
                    }
                }
            }
        }

        public static void ApplyChanges(Timing timing, IEnumerable<TimingPointsChange> timingPointsChanges, bool allAfter = false) {
            timingPointsChanges = timingPointsChanges.OrderBy(o => o.MyTP.Offset);
            foreach (TimingPointsChange c in timingPointsChanges) {
                c.AddChange(timing, allAfter);
            }
        }

        public void Debug() {
            Console.WriteLine(MyTP.GetLine());
            Console.WriteLine($"{MpB}, {Meter}, {Sampleset}, {Index}, {Volume}, {UnInherited}, {Kiai}, {OmitFirstBarLine}");
        }
    }
}
