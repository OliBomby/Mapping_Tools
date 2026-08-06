using System.Globalization;

namespace Mapping_Tools.Application.Timeline;

/// <summary>Identifies a theme-resolved semantic marker style.</summary>
public enum TimelineMarkerKind
{
    Neutral,
    Added,
    Changed,
    Removed,
    Accent
}

/// <summary>Describes one navigable timestamp without retaining a control or brush.</summary>
public sealed record TimelineMarker
{
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

    public double Time { get; }

    public TimelineMarkerKind Kind { get; }

    public string? Label { get; }
}

/// <summary>Projects timestamps into a finite viewport and supplies deterministic labels/hit testing.</summary>
public sealed class TimelineScale
{
    private const double MinimumSpan = 20;

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

    public double StartTime { get; }

    public double EndTime { get; }

    public int IntervalCount { get; }

    public IReadOnlyList<double> GetTicks() => Enumerable.Range(0, IntervalCount + 1)
        .Select(index => StartTime + (EndTime - StartTime) * index / IntervalCount)
        .ToArray();

    public double ToUnit(double time) => Math.Clamp(
        (time - StartTime) / (EndTime - StartTime),
        0,
        1);

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

    public static string FormatTick(double milliseconds)
    {
        TimeSpan value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    }

    public static string FormatMarker(double milliseconds)
    {
        TimeSpan value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{(int)value.TotalMinutes:00}:{value.Seconds:00}:{value.Milliseconds:000}");
    }
}
