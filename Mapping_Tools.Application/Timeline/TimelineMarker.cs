using System.Globalization;

namespace Mapping_Tools.Application.Timeline;

/// <summary>Describes one navigable timestamp without retaining a control or brush.</summary>
public sealed record TimelineMarker
{
    /// <summary>Creates a validated semantic timeline marker.</summary>
    /// <param name="time">The marker timestamp in milliseconds.</param>
    /// <param name="kind">The semantic style used to draw the marker.</param>
    /// <param name="label">Optional text shown when the marker is inspected.</param>
    public TimelineMarker(double time, TimelineMarkerKind kind, string? label = null)
    {
        if (!double.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
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

