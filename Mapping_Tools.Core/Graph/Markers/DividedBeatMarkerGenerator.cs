using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Generates markers on subdivisions of a beat.</summary>
public sealed class DividedBeatMarkerGenerator : IGraphMarkerGenerator
{
    /// <summary>Creates a quarter-beat marker generator.</summary>
    public DividedBeatMarkerGenerator() : this(4)
    {
    }

    /// <summary>Creates a beat-subdivision marker generator.</summary>
    /// <param name="beatDivisor">The number of subdivisions per beat.</param>
    /// <param name="snappable">Whether generated markers can snap anchors.</param>
    public DividedBeatMarkerGenerator(int beatDivisor, bool snappable = false)
    {
        BeatDivisor = beatDivisor;
        Snappable = snappable;
    }

    /// <summary>Gets or sets the number of subdivisions per beat.</summary>
    public int BeatDivisor { get; set; }

    /// <summary>Gets or sets whether generated markers can snap anchors.</summary>
    public bool Snappable { get; set; }

    /// <inheritdoc />
    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers)
    {
        if (BeatDivisor <= 0 || maxMarkers <= 0) yield break;
        double step = 1d / BeatDivisor;
        while ((end - start) / step > maxMarkers) step *= 2;
        double first = Math.Ceiling(start / step) * step;
        for (int index = 0;; index++)
        {
            double value = first + step * index;
            if (value > end + Precision.DOUBLE_EPSILON) yield break;
            (uint color, double length) = GetMarkerStyle(value);
            yield return new GraphMarker
            {
                Orientation = orientation,
                Value = value,
                DrawMarker = true,
                MarkerColorArgb = color,
                MarkerLength = length,
                Snappable = Snappable,
            };
        }
    }

    private static (uint Color, double Length) GetMarkerStyle(double value)
    {
        if (Math.Abs(value % 4) < Precision.DOUBLE_EPSILON) return (0xFFFFFFFF, 20);
        if (Math.Abs(value % 1) < Precision.DOUBLE_EPSILON) return (0xFFFFFFFF, 12);
        if (Math.Abs(value % 0.5) < Precision.DOUBLE_EPSILON) return (0xFFFF0000, 7);
        if (Math.Abs(value % 0.25) < Precision.DOUBLE_EPSILON) return (0xFF1E90FF, 7);
        if (Math.Abs(value % 0.125) < Precision.DOUBLE_EPSILON) return (0xFFFFFF00, 7);
        if (Math.Abs(value % (1d / 6)) < Precision.DOUBLE_EPSILON) return (0xFF800080, 7);
        return (0xFF808080, 7);
    }
}

