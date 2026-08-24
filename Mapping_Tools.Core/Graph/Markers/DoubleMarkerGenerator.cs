using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Generates evenly spaced numeric markers.</summary>
public sealed class DoubleMarkerGenerator : IGraphMarkerGenerator
{
    /// <summary>Creates numeric markers without a unit suffix.</summary>
    /// <param name="offset">The grid offset.</param>
    /// <param name="step">The initial spacing.</param>
    public DoubleMarkerGenerator(double offset, double step) : this(offset, step, string.Empty)
    {
    }

    /// <summary>Creates numeric markers with snapping and no unit suffix.</summary>
    /// <param name="offset">The grid offset.</param>
    /// <param name="step">The initial spacing.</param>
    /// <param name="snappable">Whether generated markers can snap anchors.</param>
    public DoubleMarkerGenerator(double offset, double step, bool snappable) : this(offset, step, string.Empty, snappable)
    {
    }

    /// <summary>Creates numeric markers with an optional unit suffix.</summary>
    /// <param name="offset">The grid offset.</param>
    /// <param name="step">The initial spacing.</param>
    /// <param name="unit">The display suffix.</param>
    /// <param name="snappable">Whether generated markers can snap anchors.</param>
    public DoubleMarkerGenerator(double offset, double step, string unit, bool snappable = false)
    {
        Offset = offset;
        Step = step;
        Unit = unit ?? string.Empty;
        Snappable = snappable;
    }

    /// <summary>Gets or sets the grid offset.</summary>
    public double Offset { get; set; }

    /// <summary>Gets or sets the initial grid step.</summary>
    public double Step { get; set; }

    /// <summary>Gets or sets the display suffix.</summary>
    public string Unit { get; set; }

    /// <summary>Gets or sets whether generated markers can snap anchors.</summary>
    public bool Snappable { get; set; }

    /// <inheritdoc />
    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers)
    {
        if (Step <= 0 || maxMarkers <= 0) yield break;
        double step = Step;
        while ((end - start) / step > maxMarkers) step *= 2;
        double first = Math.Ceiling((start - Offset) / step) * step + Offset;
        for (int index = 0;; index++)
        {
            double value = first + step * index;
            if (value > end + Precision.DOUBLE_EPSILON) yield break;
            yield return new GraphMarker
            {
                Orientation = orientation,
                Value = value,
                Text = $"{value:g2}{Unit}",
                Snappable = Snappable,
            };
        }
    }
}

