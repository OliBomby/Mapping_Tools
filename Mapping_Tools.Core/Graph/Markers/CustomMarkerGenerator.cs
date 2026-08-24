using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Generates markers using a caller-supplied value formatter.</summary>
public sealed class CustomMarkerGenerator : IGraphMarkerGenerator
{
    /// <summary>Formats one marker value.</summary>
    /// <param name="value">The marker value.</param>
    /// <returns>The display text, or null for no label.</returns>
    public delegate string? ToStringFunction(double value);

    /// <summary>Gets or sets the grid offset.</summary>
    public double Offset { get; set; }

    /// <summary>Gets or sets the marker spacing.</summary>
    public double StepSize { get; set; }

    /// <summary>Gets or sets whether markers can snap anchors.</summary>
    public bool Snappable { get; set; }

    /// <summary>Gets or sets whether the spacing may double to meet the marker budget.</summary>
    public bool Reduce { get; set; }

    /// <summary>Gets or sets whether markers have edge extensions.</summary>
    public bool DrawMarker { get; set; }

    /// <summary>Gets or sets the edge extension length.</summary>
    public double MarkerLength { get; set; }

    /// <summary>Gets or sets the ARGB edge-extension color.</summary>
    public uint MarkerColorArgb { get; set; }

    /// <summary>Gets or sets the optional text formatter.</summary>
    public ToStringFunction? ValueToString { get; set; }

    /// <inheritdoc />
    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers)
    {
        if (StepSize <= 0 || maxMarkers <= 0) yield break;
        double step = StepSize;
        if ((end - start) / step > maxMarkers)
        {
            if (!Reduce) yield break;
            while ((end - start) / step > maxMarkers) step *= 2;
        }

        double first = Math.Ceiling((start - Offset) / step) * step + Offset;
        for (int index = 0;; index++)
        {
            double value = first + step * index;
            if (value > end + Precision.DOUBLE_EPSILON) yield break;
            yield return new GraphMarker
            {
                Orientation = orientation,
                Value = value,
                Text = ValueToString?.Invoke(value),
                Snappable = Snappable,
                DrawMarker = DrawMarker,
                MarkerLength = MarkerLength,
                MarkerColorArgb = MarkerColorArgb,
            };
        }
    }
}

