using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.TimingHelper;

/// <summary>
/// Applies the legacy Timing Helper marker-to-redline algorithm to a beatmap.
/// </summary>
public static class TimingHelperEngine
{
    /// <summary>
    /// Adjusts redline BPM values and inserts redlines so selected markers snap.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap to modify.</param>
    /// <param name="options">The marker sources and timing rules.</param>
    /// <param name="progress">Optional progress receiver for the legacy 20/40/100 stages and marker loop.</param>
    /// <param name="cancellationToken">Cancels before and between timing mutations.</param>
    /// <returns>The number of redlines inserted into the beatmap.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException">The leniency is negative or no beat divisor is supplied.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    public static int Apply(
        Beatmap beatmap,
        TimingHelperOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Leniency < 0 || !double.IsFinite(options.Leniency))
        {
            throw new ArgumentException("Timing Helper leniency must be a finite non-negative value.", nameof(options));
        }
        if (options.BeatDivisors is null ||
            options.BeatDivisors.Length == 0 ||
            options.BeatDivisors.Any(divisor => divisor is null))
        {
            throw new ArgumentException("Timing Helper requires at least one beat divisor.", nameof(options));
        }

        int redlinesAdded = 0;
        Timing timing = beatmap.BeatmapTiming;
        List<Marker> markers = [];
        if (options.Objects)
        {
            markers.AddRange(beatmap.HitObjects.Select(hitObject => new Marker(hitObject.Time)));
        }
        if (options.Bookmarks)
        {
            markers.AddRange(beatmap.GetBookmarks().Select(time => new Marker(time)));
        }
        if (options.Greenlines)
        {
            markers.AddRange(timing.TimingPoints
                .Where(timingPoint => !timingPoint.Uninherited)
                .Select(timingPoint => new Marker(timingPoint.Offset)));
        }
        if (options.Redlines)
        {
            markers.AddRange(timing.TimingPoints
                .Where(timingPoint => timingPoint.Uninherited)
                .Select(timingPoint => new Marker(timingPoint.Offset)));
        }

        progress?.Report(20);
        cancellationToken.ThrowIfCancellationRequested();

        markers = markers.OrderBy(marker => marker.Time).ToList();
        if (!timing.TimingPoints.Any(timingPoint => timingPoint.Uninherited))
        {
            timing.Add(new TimingPoint(0, 1000, 4, SampleSet.Soft, 0, 100, true, false, false));
        }

        List<Marker> newMarkers = [.. markers.Where(
            (marker, index) => index == 0 ||
                Math.Abs(marker.Time - markers[index - 1].Time) >=
                options.Leniency + Precision.DoubleEpsilon)];
        markers = newMarkers;

        foreach (Marker marker in markers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double time = marker.Time;
            TimingPoint redline = timing.GetRedlineAtTime(time - 1);
            double resnappedTime = timing.Resnap(time, options.BeatDivisors, false, tp: redline);
            double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;

            if (MathHelper.ApproximatelyEquivalent(beatsFromRedline, 0, 0.0001))
            {
                beatsFromRedline = options.BeatDivisors.Min(divisor => divisor.GetValue());
            }
            if (time == redline.Offset)
            {
                beatsFromRedline = 0;
            }

            double beatsFromLastMarker = beatsFromRedline;
            List<Marker> timesBefore = markers
                .Where(previous => previous.Time < time && previous.Time > redline.Offset)
                .ToList();
            if (timesBefore.Count > 0)
            {
                double lastTime = timesBefore.Last().Time;
                double resnappedTimeLast = timing.Resnap(lastTime, options.BeatDivisors, false);
                beatsFromLastMarker = (resnappedTime - resnappedTimeLast) / redline.MpB;

                if (MathHelper.ApproximatelyEquivalent(beatsFromLastMarker, 0, 0.0001))
                {
                    beatsFromLastMarker = options.BeatDivisors.Min(divisor => divisor.GetValue());
                }
                if (lastTime == time)
                {
                    beatsFromLastMarker = 0;
                }
            }

            marker.BeatsFromLastMarker = options.BeatsBetween != -1
                ? options.BeatsBetween
                : beatsFromLastMarker;
        }

        if (!options.Redlines)
        {
            TimingPoint? first = timing.TimingPoints.FirstOrDefault(timingPoint => timingPoint.Uninherited);
            timing.RemoveAll(timingPoint => timingPoint.Uninherited && timingPoint != first);
        }

        progress?.Report(40);
        for (int index = 0; index < markers.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Marker marker = markers[index];
            double time = marker.Time;
            TimingPoint redline = timing.GetRedlineAtTime(time - 1);
            double beatsFromLastMarker = marker.BeatsFromLastMarker;
            if (beatsFromLastMarker == 0)
            {
                continue;
            }

            List<Marker> markersBefore = markers
                .Where(previous => previous.Time < time && previous.Time > redline.Offset)
                .ToList();
            markersBefore.Add(marker);

            double mpb = 0;
            double beatsFromRedline = 0;
            foreach (Marker markerBefore in markersBefore)
            {
                cancellationToken.ThrowIfCancellationRequested();
                beatsFromRedline += markerBefore.BeatsFromLastMarker;
                mpb += GetMpB(markerBefore.Time - redline.Offset, beatsFromRedline, 0);
            }
            mpb /= markersBefore.Count;

            if (CheckMpB(mpb, markersBefore, redline, options))
            {
                redline.MpB = HumanRoundMpB(mpb, markersBefore, redline, options);
            }
            else
            {
                markersBefore.Remove(marker);
                double lastTime = markersBefore.Last().Time;
                TimingPoint newRedline = redline.Copy();
                TimingPoint lastHitsounds = timing.GetTimingPointAtTime(lastTime + 5);
                newRedline.Offset = lastTime;
                newRedline.OmitFirstBarLine = options.OmitBarline;
                newRedline.Kiai = lastHitsounds.Kiai;
                newRedline.SampleIndex = lastHitsounds.SampleIndex;
                newRedline.SampleSet = lastHitsounds.SampleSet;
                newRedline.Volume = lastHitsounds.Volume;
                timing.Add(newRedline);
                newRedline.MpB = GetMpB(time - lastTime, beatsFromLastMarker, options.Leniency);
                redlinesAdded++;
            }

            progress?.Report(index * 60d / markers.Count + 40);
        }

        progress?.Report(100);
        return redlinesAdded;
    }

    private static bool CheckMpB(
        double mpbNew,
        IEnumerable<Marker> markers,
        TimingPoint redline,
        TimingHelperOptions options)
    {
        double mpbOld = redline.MpB;
        double beatsFromRedline = 0;
        bool canChangeRedline = true;
        foreach (Marker marker in markers)
        {
            double time = marker.Time;
            beatsFromRedline += marker.BeatsFromLastMarker;
            redline.MpB = mpbNew;
            double resnappedTime = redline.Offset + redline.MpB * beatsFromRedline;
            double resnappedBeats = (resnappedTime - redline.Offset) / redline.MpB;
            redline.MpB = mpbOld;

            if (!MathHelper.ApproximatelyEquivalent(resnappedBeats, beatsFromRedline, 0.1) ||
                !IsSnapped(time, resnappedTime, options.Leniency))
            {
                canChangeRedline = false;
            }
        }
        return canChangeRedline;
    }

    private static double HumanRoundMpB(
        double mpb,
        IReadOnlyCollection<Marker> markers,
        TimingPoint redline,
        TimingHelperOptions options)
    {
        double bpm = 60000 / mpb;
        double[] precisions = [1, 2, 10, 100, 1000];
        foreach (double precision in precisions)
        {
            double roundedBpm = Math.Round(bpm * precision) / precision;
            double roundedMpb = 60000 / roundedBpm;
            if (CheckMpB(roundedMpb, markers, redline, options))
            {
                return roundedMpb;
            }
        }
        return mpb;
    }

    private static double GetMpB(double timeFromRedline, double beatsFromRedline, double leniency)
    {
        double mpb = timeFromRedline / beatsFromRedline;
        double bpm = 60000 / mpb;
        double[] precisions = [1, 2, 10, 100, 1000];
        foreach (double precision in precisions)
        {
            double roundedBpm = Math.Round(bpm * precision) / precision;
            double roundedMpb = 60000 / roundedBpm;
            if (IsSnapped(timeFromRedline, roundedMpb * beatsFromRedline, leniency))
            {
                return roundedMpb;
            }
        }
        return mpb;
    }

    private static bool IsSnapped(double time, double resnappedTime, double leniency = 3) =>
        Math.Abs(resnappedTime - time) <= leniency;

    private sealed class Marker(double time)
    {
        public double Time { get; } = time;

        public double BeatsFromLastMarker { get; set; }
    }
}
