using Mapping_Tools.Domain.Beatmaps.Parsing.V14;
using Mapping_Tools.Domain.Beatmaps.Timings;

namespace Mapping_Tools.Domain.ToolHelpers;

/// <summary>
/// Struct for helping making changes to <see cref="Timing"/>.
/// </summary>
public readonly struct ControlChange(
    TimingPoint tpNew,
    bool mpb = false,
    bool meter = false,
    bool sampleset = false,
    bool index = false,
    bool volume = false,
    bool uninherited = false,
    bool kiai = false,
    bool omitFirstBarLine = false,
    double fuzzyness = 2) {

    public TimingPoint NewTimingPoint { get; } = tpNew;
    public bool MpB { get; } = mpb;
    public bool Meter { get; } = meter;
    public bool Sampleset { get; } = sampleset;
    public bool Index { get; } = index;
    public bool Volume { get; } = volume;
    public bool Uninherited { get; } = uninherited;
    public bool Kiai { get; } = kiai;
    public bool OmitFirstBarLine { get; } = omitFirstBarLine;
    public double Fuzzyness { get; } = fuzzyness;

    public void AddChange(Timing timing, bool allAfter = false) {
        TimingPoint? addingTimingPoint = null;
        TimingPoint? prevTimingPoint = null;
        List<TimingPoint> onTimingPoints = new List<TimingPoint>();
        bool onHasRed = false;
        bool onHasGreen = false;

        foreach (TimingPoint tp in timing) {
            if (tp.Offset < NewTimingPoint.Offset && (prevTimingPoint == null || tp.Offset >= prevTimingPoint.Offset)) {
                prevTimingPoint = tp;
            }
            if (Math.Abs(tp.Offset - NewTimingPoint.Offset) <= Fuzzyness) {
                onTimingPoints.Add(tp);
                onHasRed = tp.Uninherited || onHasRed;
                onHasGreen = !tp.Uninherited || onHasGreen;
            }
        }

        if (onTimingPoints.Count > 0) {
            prevTimingPoint = onTimingPoints.Last();
        }

        if (Uninherited && !onHasRed) {
            // Make new redline
            if (prevTimingPoint == null) {
                addingTimingPoint = NewTimingPoint.Copy();
                addingTimingPoint.Uninherited = true;
            } else {
                addingTimingPoint = prevTimingPoint.Copy();
                addingTimingPoint.Offset = NewTimingPoint.Offset;
                addingTimingPoint.Uninherited = true;
            }
            onTimingPoints.Add(addingTimingPoint);
        }
        if (!Uninherited && (onTimingPoints.Count == 0 || MpB && !onHasGreen)) {
            // Make new greenline (based on prev)
            if (prevTimingPoint == null) {
                addingTimingPoint = NewTimingPoint.Copy();
                addingTimingPoint.Uninherited = false;
            } else {
                addingTimingPoint = prevTimingPoint.Copy();
                addingTimingPoint.Offset = NewTimingPoint.Offset;
                addingTimingPoint.Uninherited = false;
                if (prevTimingPoint.Uninherited) { addingTimingPoint.MpB = -100; }
            }
            onTimingPoints.Add(addingTimingPoint);
        }

        foreach (TimingPoint on in onTimingPoints) {
            if (MpB && (Uninherited ? on.Uninherited : !on.Uninherited)) { on.MpB = NewTimingPoint.MpB; }
            if (Meter && Uninherited && on.Uninherited) { on.Meter = NewTimingPoint.Meter; }
            if (Sampleset) { on.SampleSet = NewTimingPoint.SampleSet; }
            if (Index) { on.SampleIndex = NewTimingPoint.SampleIndex; }
            if (Volume) { on.Volume = NewTimingPoint.Volume; }
            if (Kiai) { on.Kiai = NewTimingPoint.Kiai; }
            if (OmitFirstBarLine && Uninherited && on.Uninherited) { on.OmitFirstBarLine = NewTimingPoint.OmitFirstBarLine; }
        }

        if (addingTimingPoint != null && (prevTimingPoint == null || !addingTimingPoint.SameEffect(prevTimingPoint) || Uninherited)) {
            timing.Add(addingTimingPoint);
        }

        if (allAfter) // Change every timingpoint after
        {
            foreach (TimingPoint tp in timing) {
                if (tp.Offset > NewTimingPoint.Offset) {
                    if (Sampleset) { tp.SampleSet = NewTimingPoint.SampleSet; }
                    if (Index) { tp.SampleIndex = NewTimingPoint.SampleIndex; }
                    if (Volume) { tp.Volume = NewTimingPoint.Volume; }
                    if (Kiai) { tp.Kiai = NewTimingPoint.Kiai; }
                }
            }
        }
    }

    public static void ApplyChanges(Timing timing, IEnumerable<ControlChange> timingPointsChanges, bool allAfter = false) {
        timingPointsChanges = timingPointsChanges.OrderBy(o => o.NewTimingPoint.Offset);
        foreach (ControlChange c in timingPointsChanges) {
            c.AddChange(timing, allAfter);
        }
    }

    public override string ToString() {
        return $"{new TimingPointEncoder(true).Encode(NewTimingPoint)} | MpB: {MpB}, Meter: {Meter}, Sampleset: {Sampleset}, Index: {Index}, Volume: {Volume}, Uninherited: {Uninherited}, Kiai: {Kiai}, OmitFirstBarLine: {OmitFirstBarLine}";    }
}