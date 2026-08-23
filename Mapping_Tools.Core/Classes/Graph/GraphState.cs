using System.Globalization;
using Mapping_Tools.Core.Classes.Graph.Interpolation;
using Mapping_Tools.Core.Classes.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Core.Classes.Graph;

/// <summary>Describes one editable anchor and the interpolation leading into it.</summary>
public sealed class GraphAnchor
{
    private IGraphInterpolator interpolator = new SingleCurveInterpolator();
    private double tension;

    /// <summary>Creates an anchor with the legacy single-curve interpolation by default.</summary>
    public GraphAnchor() : this(Vector2.Zero, new SingleCurveInterpolator())
    {
    }

    /// <summary>Creates an anchor from its position, interpolator, and tension.</summary>
    /// <param name="pos">The graph-space position.</param>
    /// <param name="interpolator">The interpolation used from the previous anchor.</param>
    /// <param name="tension">The interpolation parameter, normally in the range -1 to 1.</param>
    public GraphAnchor(Vector2 pos, IGraphInterpolator? interpolator = null, double tension = 0)
    {
        Pos = pos;
        Interpolator = interpolator ?? new SingleCurveInterpolator();
        Tension = tension;
        Interpolator.P = tension;
    }

    /// <summary>Gets or sets the graph-space position.</summary>
    public Vector2 Pos { get; set; }

    /// <summary>Gets or sets the interpolation used from the previous anchor.</summary>
    /// <exception cref="ArgumentNullException">Thrown when the value is <see langword="null" />.</exception>
    public IGraphInterpolator Interpolator
    {
        get => interpolator;
        set
        {
            interpolator = value ?? throw new ArgumentNullException(nameof(value));
            interpolator.P = tension;
        }
    }

    /// <summary>Gets or sets the interpolation parameter, normally in the range -1 to 1.</summary>
    public double Tension
    {
        get => tension;
        set
        {
            tension = value;
            if (Interpolator is not null) Interpolator.P = value;
        }
    }

    /// <summary>Creates an independent copy of the anchor and its interpolator.</summary>
    /// <returns>A mutable copy suitable for editing.</returns>
    public GraphAnchor Clone()
    {
        return new GraphAnchor(Pos, GraphInterpolatorCatalog.Clone(Interpolator), Tension);
    }
}

/// <summary>Contains the bounds, anchors, and evaluation rules for one value graph.</summary>
public sealed class GraphState
{
    /// <summary>Creates the default unit graph with a centered constant value.</summary>
    public GraphState()
        : this(new List<GraphAnchor>
        {
            new(new Vector2(0, 0.5f)),
            new(new Vector2(1, 0.5f)),
        }, 0, 0, 1, 1)
    {
    }

    /// <summary>Creates graph state from anchors and explicit graph bounds.</summary>
    /// <param name="anchors">Anchors sorted by increasing X position.</param>
    /// <param name="minX">The minimum graph X value.</param>
    /// <param name="minY">The minimum graph Y value.</param>
    /// <param name="maxX">The maximum graph X value.</param>
    /// <param name="maxY">The maximum graph Y value.</param>
    public GraphState(IEnumerable<GraphAnchor>? anchors, double minX, double minY, double maxX, double maxY)
    {
        Anchors = anchors?.Select(anchor => anchor ?? throw new ArgumentException("An anchor cannot be null.", nameof(anchors))).ToList()
                  ?? [];
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    /// <summary>Gets or sets the mutable anchors in ascending X order.</summary>
    public List<GraphAnchor> Anchors { get; set; }

    /// <summary>Gets or sets the minimum graph X value.</summary>
    public double MinX { get; set; }

    /// <summary>Gets or sets the minimum graph Y value.</summary>
    public double MinY { get; set; }

    /// <summary>Gets or sets the maximum graph X value.</summary>
    public double MaxX { get; set; } = 1;

    /// <summary>Gets or sets the maximum graph Y value.</summary>
    public double MaxY { get; set; } = 1;

    /// <summary>Creates the unit-bounded default curve used by an empty value editor.</summary>
    /// <returns>A graph with two zero-valued edge anchors.</returns>
    public static GraphState CreateDefault()
    {
        return new GraphState();
    }

    /// <summary>Evaluates the graph at the supplied X value.</summary>
    /// <param name="x">The graph X value.</param>
    /// <returns>The interpolated Y value, or zero when no anchors exist.</returns>
    public double GetValue(double x)
    {
        return GraphMath.GetValue(x, Anchors);
    }

    /// <summary>Evaluates the graph derivative at the supplied X value.</summary>
    /// <param name="x">The graph X value.</param>
    /// <returns>The derivative, with infinity for a vertical segment and zero for empty or single-anchor state.</returns>
    public double GetDerivative(double x)
    {
        return GraphMath.GetDerivative(x, Anchors);
    }

    /// <summary>Integrates the graph over a graph-space interval.</summary>
    /// <param name="t1">The interval start.</param>
    /// <param name="t2">The interval end.</param>
    /// <returns>The signed area under the graph.</returns>
    public double GetIntegral(double t1, double t2)
    {
        return GraphMath.GetIntegral(t1, t2, Anchors);
    }

    /// <summary>Gets the largest graph value sampled at anchors and declared curve extrema.</summary>
    /// <returns>The largest graph Y value, or zero when no segment exists.</returns>
    public double GetMaxValue()
    {
        return GraphMath.GetMaxValue(Anchors);
    }

    /// <summary>Gets the smallest graph value sampled at anchors and declared curve extrema.</summary>
    /// <returns>The smallest graph Y value, or zero when no segment exists.</returns>
    public double GetMinValue()
    {
        return GraphMath.GetMinValue(Anchors);
    }

    /// <summary>Gets the largest signed derivative sampled at segment extrema.</summary>
    /// <returns>The largest derivative, or zero when no segment exists.</returns>
    public double GetMaxDerivative()
    {
        return GraphMath.GetMaxDerivative(Anchors);
    }

    /// <summary>Gets the smallest signed derivative sampled at segment extrema.</summary>
    /// <returns>The smallest derivative, or zero when no segment exists.</returns>
    public double GetMinDerivative()
    {
        return GraphMath.GetMinDerivative(Anchors);
    }

    /// <summary>Gets the largest accumulated integral across the graph segments.</summary>
    /// <returns>The largest accumulated integral, or zero when no segment exists.</returns>
    public double GetMaxIntegral()
    {
        return GraphMath.GetMaxIntegral(Anchors);
    }

    /// <summary>Gets the smallest accumulated integral across the graph segments.</summary>
    /// <returns>The smallest accumulated integral, or zero when no segment exists.</returns>
    public double GetMinIntegral()
    {
        return GraphMath.GetMinIntegral(Anchors);
    }

    /// <summary>Creates a deep, mutable copy of this state.</summary>
    /// <returns>A copy whose anchors and interpolators can be changed independently.</returns>
    public GraphState Clone()
    {
        return new GraphState(Anchors.Select(anchor => anchor.Clone()), MinX, MinY, MaxX, MaxY);
    }

    /// <summary>Returns the average graph value over an interval.</summary>
    /// <param name="start">The interval start.</param>
    /// <param name="end">The interval end.</param>
    /// <returns>The average value, or zero for an empty interval.</returns>
    public double GetAverage(double start, double end)
    {
        double width = end - start;
        return Math.Abs(width) < Precision.DoubleEpsilon ? 0 : GetIntegral(start, end) / width;
    }
}

/// <summary>Evaluates graph state without depending on a UI framework.</summary>
public static class GraphMath
{
    /// <summary>Evaluates a graph represented by ordered anchors.</summary>
    /// <param name="x">The graph X value.</param>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The interpolated value, or zero for empty state.</returns>
    public static double GetValue(double x, IReadOnlyList<GraphAnchor> anchors)
    {
        if (anchors.Count == 0) return 0;
        if (anchors.Count == 1) return anchors[0].Pos.Y;

        var (previous, next) = FindSegment(x, anchors);
        var difference = next.Pos - previous.Pos;
        if (Math.Abs(difference.X) < Precision.DoubleEpsilon) return previous.Pos.Y;
        return previous.Pos.Y + difference.Y * next.Interpolator.GetInterpolation((x - previous.Pos.X) / difference.X);
    }

    /// <summary>Evaluates a graph derivative represented by ordered anchors.</summary>
    /// <param name="x">The graph X value.</param>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The derivative, or zero for empty or single-anchor state.</returns>
    public static double GetDerivative(double x, IReadOnlyList<GraphAnchor> anchors)
    {
        if (anchors.Count < 2) return 0;
        var (previous, next) = FindSegment(x, anchors);
        var difference = next.Pos - previous.Pos;
        if (Math.Abs(difference.X) < Precision.DoubleEpsilon) return difference.Y > 0 ? double.PositiveInfinity : double.NegativeInfinity;

        double derivative = next.Interpolator is IDerivableInterpolator derivable
            ? derivable.GetDerivative((x - previous.Pos.X) / difference.X)
            : 1;
        return derivative * difference.Y / difference.X;
    }

    /// <summary>Integrates ordered graph segments over an interval.</summary>
    /// <param name="t1">The interval start.</param>
    /// <param name="t2">The interval end.</param>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The signed area under the graph.</returns>
    public static double GetIntegral(double t1, double t2, IReadOnlyList<GraphAnchor> anchors)
    {
        if (anchors.Count < 2) return 0;
        if (t2 < t1) return -GetIntegral(t2, t1, anchors);

        double height = 0;
        for (int index = 1; index < anchors.Count; index++)
        {
            var previous = anchors[index - 1];
            var next = anchors[index];
            double start = Math.Max(t1, previous.Pos.X);
            double end = Math.Min(t2, next.Pos.X);
            if (end <= start + Precision.DoubleEpsilon) continue;

            var difference = next.Pos - previous.Pos;
            if (Math.Abs(difference.X) < Precision.DoubleEpsilon) continue;
            double u1 = (start - previous.Pos.X) / difference.X;
            double u2 = (end - previous.Pos.X) / difference.X;
            double integral = next.Interpolator is IIntegrableInterpolator integrable
                ? integrable.GetIntegral(u1, u2)
                : 0.5 * (u2 * u2 - u1 * u1);
            height += integral * difference.X * difference.Y + (end - start) * previous.Pos.Y;
        }

        return height;
    }

    /// <summary>Gets the largest value at anchors and declared curve extrema.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The maximum value, or zero when there are no segments.</returns>
    public static double GetMaxValue(IReadOnlyList<GraphAnchor> anchors)
    {
        return GetExtremum(anchors, true, false);
    }

    /// <summary>Gets the smallest value at anchors and declared curve extrema.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The minimum value, or zero when there are no segments.</returns>
    public static double GetMinValue(IReadOnlyList<GraphAnchor> anchors)
    {
        return GetExtremum(anchors, false, false);
    }

    /// <summary>Gets the largest derivative at segment endpoints and declared extrema.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The maximum derivative, or zero when there are no segments.</returns>
    public static double GetMaxDerivative(IReadOnlyList<GraphAnchor> anchors)
    {
        return GetDerivativeExtremum(anchors, true);
    }

    /// <summary>Gets the smallest derivative at segment endpoints and declared extrema.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The minimum derivative, or zero when there are no segments.</returns>
    public static double GetMinDerivative(IReadOnlyList<GraphAnchor> anchors)
    {
        return GetDerivativeExtremum(anchors, false);
    }

    /// <summary>Gets the largest accumulated integral using declared integral extrema.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The maximum accumulated integral, or zero when there are no segments.</returns>
    public static double GetMaxIntegral(IReadOnlyList<GraphAnchor> anchors)
    {
        return GetIntegralExtremum(anchors, true);
    }

    /// <summary>Gets the smallest accumulated integral using declared integral extrema.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The minimum accumulated integral, or zero when there are no segments.</returns>
    public static double GetMinIntegral(IReadOnlyList<GraphAnchor> anchors)
    {
        return GetIntegralExtremum(anchors, false);
    }

    /// <summary>Gets the absolute value-distance travelled through graph segments.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The sum of absolute changes in evaluated graph value.</returns>
    public static double GetDistanceTraveled(IReadOnlyList<GraphAnchor> anchors)
    {
        double distance = 0;
        for (int index = 1; index < anchors.Count; index++) distance += Math.Abs(GetValue(anchors[index].Pos.X, anchors) - GetValue(anchors[index - 1].Pos.X, anchors));

        return distance;
    }

    /// <summary>Gets the sum of absolute segment integrals.</summary>
    /// <param name="anchors">The ordered graph anchors.</param>
    /// <returns>The integral distance travelled by the curve.</returns>
    public static double GetIntegralDistanceTraveled(IReadOnlyList<GraphAnchor> anchors)
    {
        double distance = 0;
        for (int index = 1; index < anchors.Count; index++) distance += Math.Abs(GetIntegral(anchors[index - 1].Pos.X, anchors[index].Pos.X, anchors));

        return distance;
    }

    private static (GraphAnchor Previous, GraphAnchor Next) FindSegment(double x, IReadOnlyList<GraphAnchor> anchors)
    {
        var previous = anchors[0];
        var next = anchors[^1];
        foreach (var anchor in anchors)
            if (anchor.Pos.X < x)
            {
                previous = anchor;
            }
            else
            {
                next = anchor;
                break;
            }

        return (previous, next);
    }

    private static double GetExtremum(IReadOnlyList<GraphAnchor> anchors, bool maximum, bool integral)
    {
        if (anchors.Count == 0) return 0;
        if (anchors.Count == 1) return anchors[0].Pos.Y;
        double result = maximum ? double.NegativeInfinity : double.PositiveInfinity;
        for (int index = 1; index < anchors.Count; index++)
        {
            var previous = anchors[index - 1];
            var next = anchors[index];
            var difference = next.Pos - previous.Pos;
            double[] positions = next.Interpolator.GetType().GetCustomAttributes(typeof(CustomExtremaAttribute), false)
                .OfType<CustomExtremaAttribute>().SelectMany(attribute => attribute.ExtremaPositions).ToArray();
            if (positions.Length == 0) positions = [0, 1];
            foreach (double position in positions)
            {
                double value = previous.Pos.Y + difference.Y * next.Interpolator.GetInterpolation(position);
                result = maximum ? Math.Max(result, value) : Math.Min(result, value);
            }
        }

        return double.IsInfinity(result) ? 0 : result;
    }

    private static double GetDerivativeExtremum(IReadOnlyList<GraphAnchor> anchors, bool maximum)
    {
        if (anchors.Count < 2) return 0;
        double result = maximum ? double.NegativeInfinity : double.PositiveInfinity;
        for (int index = 1; index < anchors.Count; index++)
        {
            var previous = anchors[index - 1];
            var next = anchors[index];
            double dx = next.Pos.X - previous.Pos.X;
            if (Math.Abs(dx) < Precision.DoubleEpsilon) continue;
            double slope = next.Pos.Y - previous.Pos.Y;
            double[] positions = next.Interpolator.GetType().GetCustomAttributes(typeof(CustomDerivativeExtremaAttribute), false)
                .OfType<CustomDerivativeExtremaAttribute>().SelectMany(attribute => attribute.ExtremaPositions).ToArray();
            if (positions.Length == 0) positions = [0, 1];
            foreach (double position in positions)
            {
                double derivative = next.Interpolator is IDerivableInterpolator derivable
                    ? derivable.GetDerivative(position) * slope / dx
                    : slope / dx;
                result = maximum ? Math.Max(result, derivative) : Math.Min(result, derivative);
            }
        }

        return double.IsInfinity(result) ? 0 : result;
    }

    private static double GetIntegralExtremum(IReadOnlyList<GraphAnchor> anchors, bool maximum)
    {
        if (anchors.Count < 2) return 0;

        double accumulated = 0;
        double result = maximum ? 0 : double.PositiveInfinity;
        for (int index = 1; index < anchors.Count; index++)
        {
            var previous = anchors[index - 1];
            var next = anchors[index];
            var difference = next.Pos - previous.Pos;
            if (difference.X <= Precision.DoubleEpsilon) continue;

            double endIntegral;
            Func<double, double> integralAt;
            if (next.Interpolator is IIntegrableInterpolator integrable)
            {
                endIntegral = integrable.GetIntegral(0, 1) * difference.X * difference.Y + difference.X * previous.Pos.Y;
                integralAt = position => integrable.GetIntegral(0, position) * difference.X * difference.Y + position * difference.X * previous.Pos.Y;

                double[] extremaPositions = next.Interpolator.GetType()
                    .GetCustomAttributes(typeof(CustomIntegralExtremaAttribute), false)
                    .OfType<CustomIntegralExtremaAttribute>()
                    .SelectMany(attribute => attribute.ExtremaPositions)
                    .Append(0)
                    .Append(1)
                    .ToArray();
                double localExtremum = maximum
                    ? extremaPositions.Select(integralAt).Max()
                    : extremaPositions.Select(integralAt).Min();

                if (difference.Y * previous.Pos.Y < 0)
                {
                    double target = -previous.Pos.Y / difference.Y;
                    var zeroes = next.Interpolator is IInvertibleInterpolator invertible
                        ? invertible.GetInverse(target).Where(position => position is >= 0 and <= 1)
                        :
                        [
                            maximum
                                ? GradientDescentUtil.GradientAscent(integralAt, 0, 1, 0.1)
                                : GradientDescentUtil.GradientDescent(integralAt, 0, 1, 0.1),
                        ];

                    foreach (double zero in zeroes)
                        localExtremum = maximum
                            ? Math.Max(localExtremum, integralAt(zero))
                            : Math.Min(localExtremum, integralAt(zero));
                }

                result = maximum
                    ? Math.Max(result, accumulated + localExtremum)
                    : Math.Min(result, accumulated + localExtremum);
            }
            else
            {
                endIntegral = 0.5 * difference.X * difference.Y + difference.X * previous.Pos.Y;
                integralAt = position => 0.5 * position * position * difference.X * difference.Y + position * difference.X * previous.Pos.Y;
                double localExtremum = endIntegral;

                if (difference.Y * previous.Pos.Y < 0)
                {
                    double zero = -previous.Pos.Y / difference.Y;
                    if (zero is >= 0 and <= 1)
                    {
                        double candidate = integralAt(zero);
                        localExtremum = maximum
                            ? Math.Max(localExtremum, candidate)
                            : Math.Min(localExtremum, candidate);
                    }
                }

                result = maximum
                    ? Math.Max(result, accumulated + localExtremum)
                    : Math.Min(result, accumulated + localExtremum);
            }

            accumulated += endIntegral;
        }

        return double.IsInfinity(result) ? 0 : result;
    }
}

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
