using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph;

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

