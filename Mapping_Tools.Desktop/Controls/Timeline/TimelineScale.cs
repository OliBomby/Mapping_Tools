using System.Globalization;

namespace Mapping_Tools.Desktop.Controls.Timeline;

/// <summary>Projects timestamps into a finite viewport and supplies deterministic labels/hit testing.</summary>
public sealed class TimelineScale
{
    private const double minimum_span = 20;

    /// <summary>Creates a finite timeline scale with evenly spaced labels.</summary>
    /// <param name="startTime">The first visible timestamp in milliseconds.</param>
    /// <param name="endTime">The requested final visible timestamp in milliseconds.</param>
    /// <param name="intervalCount">The number of equal intervals between labels.</param>
    public TimelineScale(double startTime, double endTime, int intervalCount = 10)
    {
        if (!double.IsFinite(startTime) || !double.IsFinite(endTime)) throw new ArgumentOutOfRangeException(nameof(endTime));
        if (intervalCount <= 0) throw new ArgumentOutOfRangeException(nameof(intervalCount));
        StartTime = startTime;
        EndTime = Math.Max(endTime, startTime + minimum_span);
        IntervalCount = intervalCount;
    }

    /// <summary>Gets the first visible timestamp in milliseconds.</summary>
    public double StartTime { get; }

    /// <summary>Gets the normalized final timestamp after enforcing the minimum span.</summary>
    public double EndTime { get; }

    /// <summary>Gets the number of equal intervals between timeline labels.</summary>
    public int IntervalCount { get; }

    /// <summary>Returns all evenly spaced label timestamps, including both endpoints.</summary>
    /// <returns>The timeline label timestamps in ascending order.</returns>
    public IReadOnlyList<double> GetTicks()
    {
        return Enumerable.Range(0, IntervalCount + 1)
            .Select(index => StartTime + (EndTime - StartTime) * index / IntervalCount)
            .ToArray();
    }

    /// <summary>Projects a timestamp into the inclusive zero-to-one viewport range.</summary>
    /// <param name="time">The timestamp to project.</param>
    /// <returns>The clamped relative viewport position.</returns>
    public double ToUnit(double time)
    {
        return Math.Clamp(
            (time - StartTime) / (EndTime - StartTime),
            0,
            1);
    }

    /// <summary>Finds the closest marker within a horizontal hit-test tolerance.</summary>
    /// <param name="markers">The candidate markers.</param>
    /// <param name="x">The inspected horizontal coordinate.</param>
    /// <param name="width">The timeline viewport width.</param>
    /// <param name="tolerance">The maximum accepted distance in pixels.</param>
    /// <returns>The closest qualifying marker, or <see langword="null" />.</returns>
    public TimelineMarker? FindNearest(
        IEnumerable<TimelineMarker> markers,
        double x,
        double width,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(markers);
        if (!double.IsFinite(x) || !double.IsFinite(width) || width <= 0 || !double.IsFinite(tolerance) || tolerance < 0)
            return null;

        return markers
            .Select(marker => (Marker: marker, Distance: Math.Abs(ToUnit(marker.Time) * width - x)))
            .Where(item => item.Distance <= tolerance)
            .OrderBy(item => item.Distance)
            .ThenBy(item => item.Marker.Time)
            .Select(item => item.Marker)
            .FirstOrDefault();
    }

    /// <summary>Formats a timeline label with minute and second precision.</summary>
    /// <param name="milliseconds">The timestamp to format.</param>
    /// <returns>A non-negative <c>mm:ss</c> label.</returns>
    public static string FormatTick(double milliseconds)
    {
        var value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    }

    /// <summary>Formats an inspected marker with millisecond precision.</summary>
    /// <param name="milliseconds">The timestamp to format.</param>
    /// <returns>A non-negative <c>mm:ss:fff</c> label.</returns>
    public static string FormatMarker(double milliseconds)
    {
        var value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)value.TotalMinutes:00}:{value.Seconds:00}:{value.Milliseconds:000}");
    }
}
