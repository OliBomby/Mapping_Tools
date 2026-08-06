using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.MapCleaner;

internal struct TimingPointChange
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

    public TimingPoint TimingPoint;
    public bool Mpb;
    public bool Meter;
    public bool SampleSet;
    public bool Index;
    public bool Volume;
    public bool Uninherited;
    public bool Kiai;
    public bool OmitFirstBarLine;
    public double Fuzziness;

    public static void Apply(Timing timing, IEnumerable<TimingPointChange> changes)
    {
        foreach (TimingPointChange change in changes.OrderBy(change => change.TimingPoint.Offset))
        {
            change.Add(timing);
        }
    }

    private void Add(Timing timing)
    {
        TimingPoint? adding = null;
        TimingPoint? previous = null;
        List<TimingPoint> atOffset = [];
        bool hasRed = false;
        bool hasGreen = false;
        foreach (TimingPoint? point in timing)
        {
            if (point is null)
            {
                continue;
            }

            if (point.Offset < TimingPoint.Offset && (previous is null || point.Offset >= previous.Offset))
            {
                previous = point;
            }

            if (Math.Abs(point.Offset - TimingPoint.Offset) <= Fuzziness)
            {
                atOffset.Add(point);
                hasRed |= point.Uninherited;
                hasGreen |= !point.Uninherited;
            }
        }
        if (atOffset.Count > 0)
        {
            previous = atOffset[^1];
        }

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
            if (previous?.Uninherited == true)
            {
                adding.MpB = -100;
            }

            atOffset.Add(adding);
        }
        foreach (TimingPoint point in atOffset)
        {
            if (Mpb && (Uninherited ? point.Uninherited : !point.Uninherited))
            {
                point.MpB = TimingPoint.MpB;
            }
            if (Meter && Uninherited && point.Uninherited)
            {
                point.Meter = TimingPoint.Meter;
            }
            if (SampleSet)
            {
                point.SampleSet = TimingPoint.SampleSet;
            }
            if (Index)
            {
                point.SampleIndex = TimingPoint.SampleIndex;
            }
            if (Volume)
            {
                point.Volume = TimingPoint.Volume;
            }
            if (Kiai)
            {
                point.Kiai = TimingPoint.Kiai;
            }
            if (OmitFirstBarLine && Uninherited && point.Uninherited)
            {
                point.OmitFirstBarLine = TimingPoint.OmitFirstBarLine;
            }
        }

        if (adding is not null && (previous is null || !adding.SameEffect(previous) || Uninherited))
        {
            timing.Add(adding);
        }
    }
}
