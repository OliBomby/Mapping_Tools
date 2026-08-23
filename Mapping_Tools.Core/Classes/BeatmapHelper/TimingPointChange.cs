namespace Mapping_Tools.Core.Classes.BeatmapHelper;

internal readonly struct TimingPointChange
{
    public TimingPointChange(
        TimingPoint timingPoint,
        bool mpb = false,
        bool meter = false,
        bool sampleSet = false,
        bool index = false,
        bool volume = false,
        bool uninherited = false,
        bool kiai = false,
        bool omitFirstBarLine = false,
        double fuzziness = 2)
    {
        TimingPoint = timingPoint;
        Mpb = mpb;
        Meter = meter;
        SampleSet = sampleSet;
        Index = index;
        Volume = volume;
        Uninherited = uninherited;
        Kiai = kiai;
        OmitFirstBarLine = omitFirstBarLine;
        Fuzziness = fuzziness;
    }

    public TimingPoint TimingPoint { get; }
    public bool Mpb { get; }
    public bool Meter { get; }
    public bool SampleSet { get; }
    public bool Index { get; }
    public bool Volume { get; }
    public bool Uninherited { get; }
    public bool Kiai { get; }
    public bool OmitFirstBarLine { get; }
    public double Fuzziness { get; }

    public static void Apply(
        Timing timing,
        IEnumerable<TimingPointChange> changes,
        bool allAfter = false)
    {
        foreach (var change in changes.OrderBy(change => change.TimingPoint.Offset)) change.Add(timing, allAfter);
    }

    private void Add(Timing timing, bool allAfter)
    {
        TimingPoint? adding = null;
        TimingPoint? previous = null;
        List<TimingPoint> atOffset = [];
        bool hasRed = false;
        bool hasGreen = false;

        foreach (var point in timing)
        {
            if (point is null) continue;

            if (point.Offset < TimingPoint.Offset && (previous is null || point.Offset >= previous.Offset))
                previous = point;

            if (Math.Abs(point.Offset - TimingPoint.Offset) <= Fuzziness)
            {
                atOffset.Add(point);
                hasRed |= point.Uninherited;
                hasGreen |= !point.Uninherited;
            }
        }

        if (atOffset.Count > 0) previous = atOffset[^1];

        if (Uninherited && !hasRed)
        {
            adding = previous?.Copy() ?? TimingPoint.Copy();
            adding.Offset = TimingPoint.Offset;
            adding.Uninherited = true;
            atOffset.Add(adding);
        }

        if (!Uninherited && (atOffset.Count == 0 || Mpb && !hasGreen))
        {
            adding = previous?.Copy() ?? TimingPoint.Copy();
            adding.Offset = TimingPoint.Offset;
            adding.Uninherited = false;
            if (previous?.Uninherited == true) adding.MpB = -100;

            atOffset.Add(adding);
        }

        foreach (var point in atOffset)
        {
            if (Mpb && (Uninherited ? point.Uninherited : !point.Uninherited)) point.MpB = TimingPoint.MpB;

            if (Meter && Uninherited && point.Uninherited) point.Meter = TimingPoint.Meter;

            if (SampleSet) point.SampleSet = TimingPoint.SampleSet;

            if (Index) point.SampleIndex = TimingPoint.SampleIndex;

            if (Volume) point.Volume = TimingPoint.Volume;

            if (Kiai) point.Kiai = TimingPoint.Kiai;

            if (OmitFirstBarLine && Uninherited && point.Uninherited) point.OmitFirstBarLine = TimingPoint.OmitFirstBarLine;
        }

        if (adding is not null && (previous is null || !adding.SameEffect(previous) || Uninherited))
            timing.Add(adding);

        if (allAfter)
            foreach (var point in timing)
            {
                if (point.Offset <= TimingPoint.Offset) continue;
                if (SampleSet) point.SampleSet = TimingPoint.SampleSet;
                if (Index) point.SampleIndex = TimingPoint.SampleIndex;
                if (Volume) point.Volume = TimingPoint.Volume;
                if (Kiai) point.Kiai = TimingPoint.Kiai;
            }
    }
}
