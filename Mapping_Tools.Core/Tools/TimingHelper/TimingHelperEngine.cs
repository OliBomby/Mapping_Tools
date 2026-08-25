using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.TimingHelper;

/// <summary>
///     Applies the legacy Timing Helper marker-to-redline algorithm to a beatmap.
/// </summary>
public static class TimingHelperEngine
{
    /// <summary>
    ///     Adjusts redline BPM values and inserts redlines so selected markers snap.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap to modify.</param>
    /// <param name="options">The marker sources and timing rules.</param>
    /// <param name="progress">Optional normalized progress receiver for the 0.2/0.4/1 stages and marker loop.</param>
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
        if (options.Leniency < 0 || !double.IsFinite(options.Leniency)) throw new ArgumentException("Timing Helper leniency must be a finite non-negative value.", nameof(options));
        if (options.BeatDivisors is null || options.BeatDivisors.Length == 0 || options.BeatDivisors.Any(divisor => divisor is null))
            throw new ArgumentException("Timing Helper requires at least one beat divisor.", nameof(options));

        // Count
        int redlinesAdded = 0;
        var timing = beatmap.BeatmapTiming;

        // Get all the times to snap
        List<Marker> markers = [];
        if (options.Objects) markers.AddRange(beatmap.HitObjects.Select(hitObject => new Marker(hitObject.Time)));
        if (options.Bookmarks) markers.AddRange(beatmap.GetBookmarks().Select(time => new Marker(time)));
        if (options.Greenlines)
            // Get the offsets of greenlines
            markers.AddRange(timing.TimingPoints
                .Where(timingPoint => !timingPoint.Uninherited)
                .Select(timingPoint => new Marker(timingPoint.Offset)));
        if (options.Redlines)
            // Get the offsets of redlines
            markers.AddRange(timing.TimingPoints
                .Where(timingPoint => timingPoint.Uninherited)
                .Select(timingPoint => new Marker(timingPoint.Offset)));

        // Update progressbar
        progress?.Report(0.2);
        cancellationToken.ThrowIfCancellationRequested();

        // Sort the markers
        markers = markers.OrderBy(marker => marker.Time).ToList();
        if (!timing.TimingPoints.Any(timingPoint => timingPoint.Uninherited))
            // If there are no redlines add one with a default BPM
            timing.Add(new TimingPoint(0, 1000, 4, SampleSet.Soft, 0, 100, true, false, false));

        // Remove multiple markers on the same tick
        List<Marker> newMarkers =
            [.. markers.Where((marker, index) => index == 0 || Math.Abs(marker.Time - markers[index - 1].Time) >= options.Leniency + Precision.DOUBLE_EPSILON)];
        markers = newMarkers;

        // Calculate the beats between time and the last time or redline for each time
        // Time the same is 0
        // Time a little after is smallest snap
        foreach (var marker in markers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double time = marker.Time;
            var redline = timing.GetRedlineAtTime(time - 1);
            // Resnap to that redline only
            double resnappedTime = timing.Resnap(time, options.BeatDivisors, false, redline);
            // Calculate beats from the redline
            double beatsFromRedline = (resnappedTime - redline.Offset) / redline.MpB;

            // Avoid problems
            if (MathHelper.ApproximatelyEquivalent(beatsFromRedline, 0, 0.0001)) beatsFromRedline = options.BeatDivisors.Min(divisor => divisor.GetValue());
            if (time == redline.Offset) beatsFromRedline = 0;

            // Initialize the beats from last marker
            double beatsFromLastMarker = beatsFromRedline;

            // Get the times between redline and this time
            var timesBefore = markers
                .Where(previous => previous.Time < time && previous.Time > redline.Offset)
                .ToList();
            if (timesBefore.Count > 0)
            {
                // Get the last time info
                double lastTime = timesBefore.Last().Time;
                double resnappedTimeLast = timing.Resnap(lastTime, options.BeatDivisors, false);

                // Change the beats from last marker
                beatsFromLastMarker = (resnappedTime - resnappedTimeLast) / redline.MpB;

                // Avoid problems
                if (MathHelper.ApproximatelyEquivalent(beatsFromLastMarker, 0, 0.0001)) beatsFromLastMarker = options.BeatDivisors.Min(divisor => divisor.GetValue());
                if (lastTime == time) beatsFromLastMarker = 0;
            }

            // Set the variable
            marker.BeatsFromLastMarker = options.BeatsBetween != -1
                ? options.BeatsBetween
                : beatsFromLastMarker;
        }

        // Remove redlines except the first redline
        if (!options.Redlines)
        {
            var first = timing.TimingPoints.FirstOrDefault(timingPoint => timingPoint.Uninherited);
            timing.RemoveAll(timingPoint => timingPoint.Uninherited && timingPoint != first);
        }

        // Update progressbar
        progress?.Report(0.4);
        // Loop through all the markers
        for (int index = 0; index < markers.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var marker = markers[index];
            double time = marker.Time;
            var redline = timing.GetRedlineAtTime(time - 1);
            double beatsFromLastMarker = marker.BeatsFromLastMarker;
            // Skip if 0 beats from last marker
            if (beatsFromLastMarker == 0) continue;

            // Get the times between redline and this time including this time
            var markersBefore = markers
                .Where(previous => previous.Time < time && previous.Time > redline.Offset)
                .ToList();
            markersBefore.Add(marker);

            // Calculate MpB
            // Average MpB from timesBefore and use time from redline
            double mpb = 0;
            double beatsFromRedline = 0;
            foreach (var markerBefore in markersBefore)
            {
                cancellationToken.ThrowIfCancellationRequested();
                beatsFromRedline += markerBefore.BeatsFromLastMarker;
                mpb += GetMpB(markerBefore.Time - redline.Offset, beatsFromRedline, 0);
            }

            mpb /= markersBefore.Count;

            // Check if this MpB doesn't make the markers go offsnap too far
            if (CheckMpB(mpb, markersBefore, redline, options))
            {
                // Make changes
                // Round the MpB to human values first
                redline.MpB = HumanRoundMpB(mpb, markersBefore, redline, options);
            }
            else
            {
                // Get the last time info and not the current
                markersBefore.Remove(marker);
                double lastTime = markersBefore.Last().Time;

                // Make new redline
                var newRedline = redline.Copy();
                var lastHitsounds = timing.GetTimingPointAtTime(lastTime + 5);
                newRedline.Offset = lastTime;
                newRedline.OmitFirstBarLine = options.OmitBarline; // Set omit to the argument
                newRedline.Kiai = lastHitsounds.Kiai;
                newRedline.SampleIndex = lastHitsounds.SampleIndex;
                newRedline.SampleSet = lastHitsounds.SampleSet;
                newRedline.Volume = lastHitsounds.Volume;
                timing.Add(newRedline);
                // Set the MpB
                newRedline.MpB = GetMpB(time - lastTime, beatsFromLastMarker, options.Leniency);
                // Update the counter
                redlinesAdded++;
            }

            progress?.Report(index * 0.6d / markers.Count + 0.4);
        }

        progress?.Report(1);
        return redlinesAdded;
    }

    private static bool CheckMpB(
        double mpbNew,
        IEnumerable<Marker> markers,
        TimingPoint redline,
        TimingHelperOptions options)
    {
        // For each their beatsFromRedline must stay the same AND their time must be within leniency of their resnapped time
        // If any of these times becomes incompatible, place a new anchor on the last time and not change the previous redline
        double mpbOld = redline.MpB;
        double beatsFromRedline = 0;
        bool canChangeRedline = true;
        foreach (var marker in markers)
        {
            double time = marker.Time;
            beatsFromRedline += marker.BeatsFromLastMarker;

            // Get the beatsFromRedline after changing mpb
            redline.MpB = mpbNew;
            double resnappedTime = redline.Offset + redline.MpB * beatsFromRedline;
            double resnappedBeats = (resnappedTime - redline.Offset) / redline.MpB;
            // Change MpB back so the redline doesn't get changed
            redline.MpB = mpbOld;

            // Check changes
            if (!MathHelper.ApproximatelyEquivalent(resnappedBeats, beatsFromRedline, 0.1) || !IsSnapped(time, resnappedTime, options.Leniency))
                canChangeRedline = false;
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

        // Round bpm
        double[] precisions = [1, 2, 10, 100, 1000];
        foreach (double precision in precisions)
        {
            double roundedBpm = Math.Round(bpm * precision) / precision;
            double roundedMpb = 60000 / roundedBpm;
            if (CheckMpB(roundedMpb, markers, redline, options)) return roundedMpb;
        }

        return mpb;
    }

    private static double GetMpB(double timeFromRedline, double beatsFromRedline, double leniency)
    {
        // Will make human-like BPM values like integers, halves and tenths
        // If that doesn't work (like the time is really far from the redline) it will try thousandths

        // Exact MpB and BPM
        double mpb = timeFromRedline / beatsFromRedline;
        double bpm = 60000 / mpb;

        // Round bpm
        double[] precisions = [1, 2, 10, 100, 1000];
        foreach (double precision in precisions)
        {
            double roundedBpm = Math.Round(bpm * precision) / precision;
            double roundedMpb = 60000 / roundedBpm;
            if (IsSnapped(timeFromRedline, roundedMpb * beatsFromRedline, leniency)) return roundedMpb;
        }

        return mpb;
    }

    private static bool IsSnapped(double time, double resnappedTime, double leniency = 3)
    {
        return Math.Abs(resnappedTime - time) <= leniency;
    }

    private sealed class Marker(double time)
    {
        public double Time { get; } = time;

        public double BeatsFromLastMarker { get; set; }
    }
}
