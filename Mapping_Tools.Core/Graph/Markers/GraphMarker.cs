namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Describes marker geometry and snapping semantics without a UI brush type.</summary>
public sealed class GraphMarker
{
    /// <summary>Gets or sets the marker orientation.</summary>
    public GraphMarkerOrientation Orientation { get; set; }

    /// <summary>Gets or sets the value represented by the marker.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets optional display text.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets whether the marker participates in anchor snapping.</summary>
    public bool Snappable { get; set; }

    /// <summary>Gets or sets whether the marker has a short extension beyond the graph edge.</summary>
    public bool DrawMarker { get; set; }

    /// <summary>Gets or sets the extension length in device-independent pixels.</summary>
    public double MarkerLength { get; set; }

    /// <summary>Gets or sets an optional ARGB line color for the marker.</summary>
    public uint? MarkerColorArgb { get; set; }

    /// <summary>Gets or sets an optional ARGB color for the marker line.</summary>
    public uint? CustomLineColorArgb { get; set; }

    /// <summary>Gets or sets whether the marker is visible.</summary>
    public bool Visible { get; set; } = true;
}

