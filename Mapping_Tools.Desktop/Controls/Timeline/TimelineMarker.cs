namespace Mapping_Tools.Desktop.Controls.Timeline;

/// <summary>Describes one navigable timestamp without retaining a control or brush.</summary>
public sealed record TimelineMarker
{
    /// <summary>Creates a validated semantic timeline marker.</summary>
    /// <param name="time">The marker timestamp in milliseconds.</param>
    /// <param name="kind">The semantic style used to draw the marker.</param>
    public TimelineMarker(double time, TimelineMarkerKind kind)
    {
        if (!double.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
        Time = time;
        Kind = kind;
    }

    /// <summary>Gets the marker timestamp in milliseconds.</summary>
    public double Time { get; }

    /// <summary>Gets the semantic style used to draw the marker.</summary>
    public TimelineMarkerKind Kind { get; }

}
