using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.TumourGenerator.Templates;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.TumourGenerator.Models;

/// <summary>Describes one configurable tumour layer without UI dependencies.</summary>
public sealed class TumourLayer
{
    /// <summary>Gets or sets the selected template.</summary>
    public TumourTemplate TumourTemplateEnum { get; set; }

    /// <summary>Gets a fresh configured template instance for this layer.</summary>
    [JsonIgnore]
    public ITumourTemplate TumourTemplate => TumourTemplateEnum switch
    {
        Templates.TumourTemplate.Triangle => new TriangleTemplate(),
        Templates.TumourTemplate.Square => new SquareTemplate(),
        Templates.TumourTemplate.Circle => new CircleTemplate(),
        Templates.TumourTemplate.Parabola => new ParabolaTemplate(),
        _ => new TriangleTemplate(),
    };

    /// <summary>Gets or sets how the tumour follows the slider path.</summary>
    public WrappingMode WrappingMode { get; set; }

    /// <summary>Gets or sets the side-selection policy.</summary>
    public TumourSidedness TumourSidedness { get; set; }

    /// <summary>Gets or sets the graph controlling tumour length.</summary>
    public GraphState TumourLength { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling tumour scale.</summary>
    public GraphState TumourScale { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling tumour rotation in degrees.</summary>
    public GraphState TumourRotation { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling the template parameter.</summary>
    public GraphState TumourParameter { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling spacing between tumours.</summary>
    public GraphState TumourDistance { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the explicit tumour count, or zero for distance-based count.</summary>
    public int TumourCount { get; set; }

    /// <summary>Gets or sets the sequence start in relative or absolute units.</summary>
    public double TumourStart { get; set; }

    /// <summary>Gets or sets the sequence end in relative or absolute units.</summary>
    public double TumourEnd { get; set; }

    /// <summary>Gets or sets the deterministic random-side seed; zero uses the generator sequence.</summary>
    public int RandomSeed { get; set; }

    /// <summary>Gets or sets whether this layer recalculates the path before placement.</summary>
    public bool Recalculate { get; set; }

    /// <summary>Gets or sets whether range and shape values are absolute osu! pixels.</summary>
    public bool UseAbsoluteRange { get; set; }

    /// <summary>Gets or sets whether this layer participates in generation.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the user-facing layer name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Creates the legacy default layer, including constant graph values.</summary>
    /// <returns>A new active triangle layer.</returns>
    public static TumourLayer GetDefaultLayer()
    {
        return new TumourLayer
        {
            TumourTemplateEnum = Templates.TumourTemplate.Triangle,
            WrappingMode = WrappingMode.Simple,
            TumourSidedness = TumourSidedness.Left,
            IsActive = true,
            Name = "Layer",
            TumourCount = 0,
            TumourStart = 0,
            TumourEnd = 256,
            TumourLength = GetGraphState(15),
            TumourScale = GetGraphState(30),
            TumourRotation = GetGraphState(0),
            TumourParameter = GetGraphState(0),
            TumourDistance = GetGraphState(100),
            RandomSeed = 0,
            UseAbsoluteRange = true,
            Recalculate = true,
        };
    }

    /// <summary>Creates a constant graph with bounds matching the legacy editor.</summary>
    /// <param name="value">The constant graph value.</param>
    /// <returns>A graph with two single-curve edge anchors.</returns>
    public static GraphState GetGraphState(double value)
    {
        return new GraphState(
            [
                new GraphAnchor(new Vector2(0, (float)value), new SingleCurveInterpolator()),
                new GraphAnchor(new Vector2(1, (float)value), new SingleCurveInterpolator()),
            ],
            0,
            Math.Min(0, value * 2),
            1,
            Math.Max(0, value * 2));
    }

    /// <summary>Creates a deep copy suitable for editing independently.</summary>
    /// <returns>A mutable copy of this layer and all graph state.</returns>
    public TumourLayer Copy()
    {
        return new TumourLayer
        {
            TumourTemplateEnum = TumourTemplateEnum,
            WrappingMode = WrappingMode,
            TumourSidedness = TumourSidedness,
            TumourLength = TumourLength.Clone(),
            TumourScale = TumourScale.Clone(),
            TumourRotation = TumourRotation.Clone(),
            TumourParameter = TumourParameter.Clone(),
            TumourDistance = TumourDistance.Clone(),
            TumourCount = TumourCount,
            TumourStart = TumourStart,
            TumourEnd = TumourEnd,
            RandomSeed = RandomSeed,
            Recalculate = Recalculate,
            UseAbsoluteRange = UseAbsoluteRange,
            IsActive = IsActive,
            Name = Name,
        };
    }
}

