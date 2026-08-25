using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph;

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
        if (Math.Abs(difference.X) < Precision.DOUBLE_EPSILON) return previous.Pos.Y;
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
        if (Math.Abs(difference.X) < Precision.DOUBLE_EPSILON) return difference.Y > 0 ? double.PositiveInfinity : double.NegativeInfinity;

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
            if (end <= start + Precision.DOUBLE_EPSILON) continue;

            var difference = next.Pos - previous.Pos;
            if (Math.Abs(difference.X) < Precision.DOUBLE_EPSILON) continue;
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
            if (Math.Abs(dx) < Precision.DOUBLE_EPSILON) continue;
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
            if (difference.X <= Precision.DOUBLE_EPSILON) continue;

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

