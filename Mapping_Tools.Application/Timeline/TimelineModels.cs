using System.Globalization;

namespace Mapping_Tools.Application.Timeline;

/// <summary>Identifies a theme-resolved semantic marker style.</summary>
public enum TimelineMarkerKind
{
    /// <summary>Represents a general event without change semantics.</summary>
    Neutral,

    /// <summary>Represents a newly added map element.</summary>
    Added,

    /// <summary>Represents an existing map element that was modified.</summary>
    Changed,

    /// <summary>Represents a removed map element.</summary>
    Removed,

    /// <summary>Represents a feature-specific highlighted event.</summary>
    Accent
}

/// <summary>Describes one navigable timestamp without retaining a control or brush.</summary>
public sealed record TimelineMarker
{
    /// <summary>Creates a validated semantic timeline marker.</summary>
    /// <param name="time">The marker timestamp in milliseconds.</param>
    /// <param name="kind">The semantic style used to draw the marker.</param>
    /// <param name="label">Optional text shown when the marker is inspected.</param>
    public TimelineMarker(double time, TimelineMarkerKind kind, string? label = null)
    {
        if (!double.IsFinite(time))
        {
            throw new ArgumentOutOfRangeException(nameof(time));
        }
        Time = time;
        Kind = kind;
        Label = string.IsNullOrWhiteSpace(label) ? null : label;
    }

    /// <summary>Gets the marker timestamp in milliseconds.</summary>
    public double Time { get; }

    /// <summary>Gets the semantic style used to draw the marker.</summary>
    public TimelineMarkerKind Kind { get; }

    /// <summary>Gets the optional descriptive label.</summary>
    public string? Label { get; }
}

/// <summary>Projects timestamps into a finite viewport and supplies deterministic labels/hit testing.</summary>
public sealed class TimelineScale
{
    private const double MinimumSpan = 20;

    /// <summary>Creates a finite timeline scale with evenly spaced labels.</summary>
    /// <param name="startTime">The first visible timestamp in milliseconds.</param>
    /// <param name="endTime">The requested final visible timestamp in milliseconds.</param>
    /// <param name="intervalCount">The number of equal intervals between labels.</param>
    public TimelineScale(double startTime, double endTime, int intervalCount = 10)
    {
        if (!double.IsFinite(startTime) || !double.IsFinite(endTime))
        {
            throw new ArgumentOutOfRangeException(nameof(endTime));
        }
        if (intervalCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalCount));
        }
        StartTime = startTime;
        EndTime = Math.Max(endTime, startTime + MinimumSpan);
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
    public IReadOnlyList<double> GetTicks() => Enumerable.Range(0, IntervalCount + 1)
        .Select(index => StartTime + (EndTime - StartTime) * index / IntervalCount)
        .ToArray();

    /// <summary>Projects a timestamp into the inclusive zero-to-one viewport range.</summary>
    /// <param name="time">The timestamp to project.</param>
    /// <returns>The clamped relative viewport position.</returns>
    public double ToUnit(double time) => Math.Clamp(
        (time - StartTime) / (EndTime - StartTime),
        0,
        1);

    /// <summary>Finds the closest marker within a horizontal hit-test tolerance.</summary>
    /// <param name="markers">The candidate markers.</param>
    /// <param name="x">The inspected horizontal coordinate.</param>
    /// <param name="width">The timeline viewport width.</param>
    /// <param name="tolerance">The maximum accepted distance in pixels.</param>
    /// <returns>The closest qualifying marker, or <see langword="null"/>.</returns>
    public TimelineMarker? FindNearest(
        IEnumerable<TimelineMarker> markers,
        double x,
        double width,
        double tolerance)
    {
        ArgumentNullException.ThrowIfNull(markers);
        if (!double.IsFinite(x) || !double.IsFinite(width) || width <= 0 ||
            !double.IsFinite(tolerance) || tolerance < 0)
        {
            return null;
        }

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
        TimeSpan value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    }

    /// <summary>Formats an inspected marker with millisecond precision.</summary>
    /// <param name="milliseconds">The timestamp to format.</param>
    /// <returns>A non-negative <c>mm:ss:fff</c> label.</returns>
    public static string FormatMarker(double milliseconds)
    {
        TimeSpan value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)value.TotalMinutes:00}:{value.Seconds:00}:{value.Milliseconds:000}");
    }
}
