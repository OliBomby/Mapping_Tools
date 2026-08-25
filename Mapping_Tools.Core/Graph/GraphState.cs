using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph;

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
        return Math.Abs(width) < Precision.DOUBLE_EPSILON ? 0 : GetIntegral(start, end) / width;
    }
}

