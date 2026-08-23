using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.TimingCopier;

/// <summary>
///     Applies the legacy Timing Copier transformation to an in-memory beatmap.
/// </summary>
public static class TimingCopierEngine
{
    /// <summary>
    ///     Replaces target timing with source timing and applies the selected marker-placement mode.
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

        var timingTo = target.BeatmapTiming;
        var timingFrom = source.BeatmapTiming;

        List<Marker> markers = [];
        if (options.ResnapMode == TimingCopierResnapModes.PreserveBeatSpacing)
            // Get markers for hitobjects if mode 1 is used
            markers = GetMarkers(target, timingTo);

        cancellationToken.ThrowIfCancellationRequested();
        List<TimingPoint> removeList = [];
        // Rid the beatmap of redlines
        foreach (var redline in timingTo.Redlines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var greenlineHere = timingTo.GetGreenlineAtTime(redline.Offset);

            // If a greenline exists at the same time as a redline then the redline ceizes to exist
            // Else convert the redline to a greenline: Inherited = false & MpB = -100
            if (greenlineHere.Offset != redline.Offset)
            {
                var newGreenline = redline.Copy();
                newGreenline.Uninherited = false;
                newGreenline.MpB = -100;
                timingTo.Add(newGreenline);
            }

            removeList.Add(redline);
        }

        foreach (var timingPoint in removeList) timingTo.Remove(timingPoint);

        // Make new timing points changes
        List<TimingPointChange> timingPointChanges = [];
        // Add redlines
        foreach (var timingPoint in timingFrom.Redlines)
            timingPointChanges.Add(
                new TimingPointChange(
                    timingPoint,
                    true,
                    true,
                    uninherited: true,
                    omitFirstBarLine: true,
                    fuzziness: Precision.DoubleEpsilon));

        // Apply timing changes
        TimingPointChange.Apply(timingTo, timingPointChanges);

        IReadOnlyList<TimingPoint> redlines = timingFrom.Redlines;
        if (options.ResnapMode == TimingCopierResnapModes.PreserveBeatSpacing && redlines.Count > 0)
        {
            redlines = timingTo.Redlines;
            List<double> newBookmarks = [];
            double lastTime = redlines.FirstOrDefault()?.Offset ?? 0;
            foreach (var marker in markers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var redline = timingTo.GetRedlineAtTime(
                    lastTime,
                    redlines.FirstOrDefault());

                double beatsFromLastTime = marker.BeatsFromLastMarker;
                while (true)
                {
                    // Get redlines between this and last marker
                    var redlinesBetween = redlines
                        .Where(point => point.Offset <= lastTime + redline.MpB * beatsFromLastTime && point.Offset > lastTime)
                        .ToList();

                    if (redlinesBetween.Count == 0) break;

                    var first = redlinesBetween.First();
                    double difference = first.Offset - lastTime;
                    beatsFromLastTime -= difference / redline.MpB;
                    redline = first;
                    lastTime = first.Offset;
                }

                // Last time is the time of the last redline in between
                double newTime = lastTime + redline.MpB * beatsFromLastTime;
                newTime = timingTo.Resnap(
                    newTime,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
                marker.Time = newTime;
                lastTime = marker.Time;
            }

            foreach (var marker in markers)
                if (marker.Object is double bookmark)
                    newBookmarks.Add(bookmark);

            target.SetBookmarks(newBookmarks);
        }
        else if (options.ResnapMode == TimingCopierResnapModes.Resnap && redlines.Count > 0)
        {
            // Resnap hitobjects
            foreach (var hitObject in target.HitObjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                hitObject.ResnapSelf(
                    timingTo,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
            }

            // Resnap greenlines
            foreach (var timingPoint in timingTo.Greenlines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                timingPoint.ResnapSelf(
                    timingTo,
                    options.BeatDivisors,
                    firstTp: redlines.FirstOrDefault());
            }

            timingTo.Sort();
        }

        // Fix SV for if new redlines were added
        timingPointChanges = [];
        foreach (var hitObject in target.HitObjects.Where(hitObject => hitObject.IsSlider))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timingPoint = hitObject.TimingPoint.Copy();
            timingPoint.Offset = hitObject.Time;
            timingPoint.MpB = hitObject.SliderVelocity;
            timingPointChanges.Add(
                new TimingPointChange(
                    timingPoint,
                    true,
                    fuzziness: Precision.DoubleEpsilon));
        }

        // Apply timing changes
        TimingPointChange.Apply(timingTo, timingPointChanges);

        if ((options.ResnapMode == TimingCopierResnapModes.Resnap || options.ResnapMode == TimingCopierResnapModes.PreserveBeatSpacing) && redlines.Count > 0)
        {
            target.GiveObjectsGreenlines();
            target.CalculateSliderEndTimes();
            // Resnap slider ends and spinner ends
            foreach (var hitObject in target.HitObjects)
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

        foreach (var hitObject in beatmap.HitObjects) markers.Add(new Marker(hitObject));

        foreach (double bookmark in beatmap.GetBookmarks()) markers.Add(new Marker(bookmark));

        foreach (var timingPoint in timing.TimingPoints) markers.Add(new Marker(timingPoint));

        // Sort the markers
        markers = markers.OrderBy(marker => marker.Time).ToList();
        if (markers.Count == 0) return markers;

        double lastTime = redlines.First().Offset;
        foreach (var marker in markers)
        {
            // Calculate the beats between this marker and the last marker
            // If there is a redline in between then calculate beats from last marker to the redline and beats from redline to this marker
            // Time the same is 0
            var redlinesBetween = redlines
                .Where(point => point.Offset < marker.Time && point.Offset > lastTime)
                .ToList();
            var redline = timing.GetRedlineAtTime(lastTime);

            // Set the variable
            double beatsFromLastMarker = 0;
            foreach (var redlineBetween in redlinesBetween)
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
                _ => -1,
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
