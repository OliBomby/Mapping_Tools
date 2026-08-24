using System.Globalization;
using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Core.Graph;

/// <summary>Provides the legacy text representation for graph values.</summary>
public static class GraphStateTextCodec
{
    /// <summary>Formats constant graphs as one value and curves as pipe-separated anchors.</summary>
    /// <param name="state">The graph to format.</param>
    /// <returns>A culture-invariant, editable graph string.</returns>
    public static string Format(GraphState? state)
    {
        if (state?.Anchors.FirstOrDefault() is not { } first) return string.Empty;
        if (state.Anchors.All(anchor => Precision.AlmostEquals(anchor.Pos.Y, first.Pos.Y))) return first.Pos.Y.ToString(CultureInfo.InvariantCulture);

        return string.Join('|', state.Anchors.Select(anchor =>
            $"{anchor.Pos.X.ToString("0.###", CultureInfo.InvariantCulture)}:"
            + $"{anchor.Pos.Y.ToString("0.###", CultureInfo.InvariantCulture)}:"
            + $"{GraphInterpolatorCatalog.GetInterpolatorIndex(anchor.Interpolator.GetType()).ToString(CultureInfo.InvariantCulture)}:"
            + $"{anchor.Tension.ToString("0.###", CultureInfo.InvariantCulture)}"));
    }

    /// <summary>Parses a constant or pipe-separated graph string.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="state">The parsed graph, or a safe default for malformed graph input.</param>
    /// <returns><see langword="true" /> for a complete valid graph representation.</returns>
    public static bool TryParse(string? text, out GraphState state)
    {
        state = GraphState.CreateDefault();
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (TypeConverters.TryParseDouble(text, out double constant))
        {
            state = CreateConstant(constant);
            return true;
        }

        string[] anchorTexts = text.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (anchorTexts.Length == 0) return false;

        List<GraphAnchor> anchors = [];
        foreach (string anchorText in anchorTexts)
        {
            string[] values = anchorText.Split(':');
            if (values.Length < 4
                || !TypeConverters.TryParseDouble(values[0], out double x)
                || !TypeConverters.TryParseDouble(values[1], out double y)
                || !TypeConverters.TryParseInt(values[2], out int index)
                || !TypeConverters.TryParseDouble(values[3], out double tension))
                return false;

            var interpolator = GraphInterpolatorCatalog.GetInterpolator(
                GraphInterpolatorCatalog.GetInterpolatorByIndex(index));
            anchors.Add(new GraphAnchor(new Vector2(x, y), interpolator, tension));
        }

        if (anchors.Count < 2) return false;

        var min = anchors.Select(anchor => anchor.Pos).Aggregate(Vector2.ComponentMin);
        var max = anchors.Select(anchor => anchor.Pos).Aggregate(Vector2.ComponentMax);
        var size = Vector2.ComponentMax(Vector2.One, max - min);
        state = new GraphState(anchors, min.X, min.Y, min.X + size.X, min.Y + size.Y);
        return true;
    }

    /// <summary>Creates a constant graph with bounds that include the entered value.</summary>
    /// <param name="value">The constant value.</param>
    /// <returns>A two-anchor constant graph.</returns>
    public static GraphState CreateConstant(double value)
    {
        return new GraphState(
        [
            new GraphAnchor(new Vector2(0, value), new SingleCurveInterpolator()),
            new GraphAnchor(new Vector2(1, value), new SingleCurveInterpolator()),
        ], 0, Math.Min(0, value * 2), 1, Math.Max(1, value * 2));
    }
}
