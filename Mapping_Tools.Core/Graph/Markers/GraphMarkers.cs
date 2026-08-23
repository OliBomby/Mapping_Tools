using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Identifies the graph axis to which a marker is perpendicular.</summary>
public enum GraphMarkerOrientation
{
    /// <summary>A horizontal line representing a Y value.</summary>
    Horizontal,

    /// <summary>A vertical line representing an X value.</summary>
    Vertical,
}

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

/// <summary>Generates visible graph markers for a value interval.</summary>
public interface IGraphMarkerGenerator
{
    /// <summary>Generates markers for an interval and display budget.</summary>
    /// <param name="start">The first visible value.</param>
    /// <param name="end">The last visible value.</param>
    /// <param name="orientation">The orientation of generated markers.</param>
    /// <param name="maxMarkers">The maximum desired marker count.</param>
    /// <returns>The generated marker sequence.</returns>
    IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers);
}

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

/// <summary>Combines multiple graph marker generators in display order.</summary>
public sealed class CompositeMarkerGenerator : IGraphMarkerGenerator
{
    /// <summary>Creates a composite marker generator.</summary>
    /// <param name="generators">The generators to invoke in order.</param>
    public CompositeMarkerGenerator(IEnumerable<IGraphMarkerGenerator> generators)
    {
        Generators = generators?.ToArray() ?? throw new ArgumentNullException(nameof(generators));
    }

    /// <summary>Gets the child generators.</summary>
    public IReadOnlyList<IGraphMarkerGenerator> Generators { get; }

    /// <inheritdoc />
    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers)
    {
        foreach (var generator in Generators)
        foreach (var marker in generator.GenerateMarkers(start, end, orientation, maxMarkers))
            yield return marker;
    }
}
