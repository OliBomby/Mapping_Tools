using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.TimingCopier;

/// <summary>
/// Applies the legacy Timing Copier transformation to an in-memory beatmap.
/// </summary>
public static class TimingCopierEngine
{
    /// <summary>
    /// Replaces target timing with source timing and applies the selected marker-placement mode.
    /// </summary>
    /// <param name="target">The mutable beatmap receiving the copied timing.</param>
    /// <param name="source">The beatmap supplying redlines and greenlines.</param>
    /// <param name="options">The mode and snap intervals to apply.</param>
    /// <param name="cancellationToken">Cancels between marker and timing operations.</param>
    /// <exception cref="ArgumentNullException">A beatmap or options argument is null.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    public static void Apply(
        Beatmap target,
        Beatmap source,
        TimingCopierOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        Timing timingTo = target.BeatmapTiming;
        Timing timingFrom = source.BeatmapTiming;

        List<Marker> markers = [];
        if (options.ResnapMode == TimingCopierResnapModes.PreserveBeatSpacing)
        {
            markers = GetMarkers(target, timingTo);
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<TimingPoint> removeList = [];
        foreach (TimingPoint redline in timingTo.Redlines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimingPoint greenlineHere = timingTo.GetGreenlineAtTime(redline.Offset);

            if (greenlineHere.Offset != redline.Offset)
            {
                TimingPoint newGreenline = redline.Copy();
                newGreenline.Uninherited = false;
                newGreenline.MpB = -100;
                timingTo.Add(newGreenline);
            }

            removeList.Add(redline);
        }

        foreach (TimingPoint timingPoint in removeList)
        {
            timingTo.Remove(timingPoint);
        }

        List<TimingPointChange> timingPointChanges = [];
        foreach (TimingPoint timingPoint in timingFrom.Redlines)
        {
            timingPointChanges.Add(
                new TimingPointChange(
                    timingPoint,
                    mpb: true,
                    meter: true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DoubleEpsilon));
        }

        TimingPointChange.Apply(timingTo, timingPointChanges);

        IReadOnlyList<TimingPoint> redlines = timingFrom.Redlines;
        if (options.ResnapMode == TimingCopierResnapModes.PreserveBeatSpacing && redlines.Count > 0)
        {
            redlines = timingTo.Redlines;
            List<double> newBookmarks = [];
            double lastTime = redlines.FirstOrDefault()?.Offset ?? 0;
            foreach (Marker marker in markers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TimingPoint redline = timingTo.GetRedlineAtTime(
                    lastTime,
                    redlines.FirstOrDefault());

                double beatsFromLastTime = marker.BeatsFromLastMarker;
                while (true)
                {
                    List<TimingPoint> redlinesBetween = redlines
                        .Where(point => point.Offset <= lastTime + redline.MpB * beatsFromLastTime &&
                                        point.Offset > lastTime)
                        .ToList();

                    if (redlinesBetween.Count == 0)
                    {
                        break;
                    }

                    TimingPoint first = redlinesBetween.First();
                    double difference = first.Offset - lastTime;
                    beatsFromLastTime -= difference / redline.MpB;
                    redline = first;
                    lastTime = first.Offset;
                }

                double newTime = lastTime + redline.MpB * beatsFromLastTime;
                newTime = timingTo.Resnap(
                    newTime,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
                marker.Time = newTime;
                lastTime = marker.Time;
            }

            foreach (Marker marker in markers)
            {
                if (marker.Object is double bookmark)
                {
                    newBookmarks.Add(bookmark);
                }
            }

            target.SetBookmarks(newBookmarks);
        }
        else if (options.ResnapMode == TimingCopierResnapModes.Resnap && redlines.Count > 0)
        {
            foreach (HitObject hitObject in target.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                hitObject.ResnapSelf(
                    timingTo,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
            }

            foreach (TimingPoint timingPoint in timingTo.Greenlines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                timingPoint.ResnapSelf(
                    timingTo,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
            }

            timingTo.Sort();
        }

        timingPointChanges = [];
        foreach (HitObject hitObject in target.HitObjects.Where(hitObject => hitObject.IsSlider))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimingPoint timingPoint = hitObject.TimingPoint.Copy();
            timingPoint.Offset = hitObject.Time;
            timingPoint.MpB = hitObject.SliderVelocity;
            timingPointChanges.Add(
                new TimingPointChange(
                    timingPoint,
                    mpb: true,
                    fuzziness: Precision.DoubleEpsilon));
        }

        TimingPointChange.Apply(timingTo, timingPointChanges);

        if ((options.ResnapMode == TimingCopierResnapModes.Resnap ||
             options.ResnapMode == TimingCopierResnapModes.PreserveBeatSpacing) &&
            redlines.Count > 0)
        {
            target.GiveObjectsGreenlines();
            target.CalculateSliderEndTimes();
            foreach (HitObject hitObject in target.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                hitObject.ResnapEnd(
                    timingTo,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
            }
        }
    }

    private static List<Marker> GetMarkers(Beatmap beatmap, Timing timing)
    {
        List<Marker> markers = [];
        IReadOnlyList<TimingPoint> redlines = timing.Redlines;

        foreach (HitObject hitObject in beatmap.HitObjects)
        {
            markers.Add(new Marker(hitObject));
        }

        foreach (double bookmark in beatmap.GetBookmarks())
        {
            markers.Add(new Marker(bookmark));
        }

        foreach (TimingPoint timingPoint in timing.TimingPoints)
        {
            markers.Add(new Marker(timingPoint));
        }

        markers = markers.OrderBy(marker => marker.Time).ToList();
        if (markers.Count == 0)
        {
            return markers;
        }

        double lastTime = redlines.First().Offset;
        foreach (Marker marker in markers)
        {
            List<TimingPoint> redlinesBetween = redlines
                .Where(point => point.Offset < marker.Time && point.Offset > lastTime)
                .ToList();
            TimingPoint redline = timing.GetRedlineAtTime(lastTime);

            double beatsFromLastMarker = 0;
            foreach (TimingPoint redlineBetween in redlinesBetween)
            {
                beatsFromLastMarker += (redlineBetween.Offset - lastTime) / redline.MpB;
                redline = redlineBetween;
                lastTime = redlineBetween.Offset;
            }

            beatsFromLastMarker += (marker.Time - lastTime) / redline.MpB;
            marker.BeatsFromLastMarker = beatsFromLastMarker;
            lastTime = marker.Time;
        }

        return markers;
    }

    private sealed class Marker
    {
        public Marker(object value)
        {
            Object = value;
        }

        public object Object { get; private set; }

        public double BeatsFromLastMarker { get; set; }

        public double Time
        {
            get => Object switch
            {
                double value => value,
                HitObject hitObject => hitObject.Time,
                TimingPoint timingPoint => timingPoint.Offset,
                _ => -1
            };
            set
            {
                switch (Object)
                {
                    case double:
                        Object = value;
                        break;
                    case HitObject hitObject:
                        hitObject.Time = value;
                        break;
                    case TimingPoint timingPoint:
                        timingPoint.Offset = value;
                        break;
                }
            }
        }
    }
}
