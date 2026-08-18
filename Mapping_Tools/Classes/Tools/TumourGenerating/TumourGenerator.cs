using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Core.Classes.Graph.Interpolation;
using Mapping_Tools.Core.Classes.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders.Newgen;
using CoreTumourGenerator = Mapping_Tools.Core.Tools.TumourGenerating.TumourGenerator;
using CoreTumourLayer = Mapping_Tools.Core.Tools.TumourGenerating.TumourLayer;
using CoreTumourTemplate = Mapping_Tools.Core.Tools.TumourGenerating.TumourTemplate;
using CoreWrappingMode = Mapping_Tools.Core.Tools.TumourGenerating.WrappingMode;
using CoreTumourSidedness = Mapping_Tools.Core.Tools.TumourGenerating.TumourSidedness;
using LegacyGraphState = Mapping_Tools.Components.Graph.GraphState;
using CoreGraphAnchor = Mapping_Tools.Core.Classes.Graph.GraphAnchor;
using CoreGraphState = Mapping_Tools.Core.Classes.Graph.GraphState;
using CoreGraphInterpolatorCatalog = Mapping_Tools.Core.Classes.Graph.Interpolation.GraphInterpolatorCatalog;

namespace Mapping_Tools.Classes.Tools.TumourGenerating;

/// <summary>
/// Keeps the legacy WPF tumour-generator API while delegating path mutation
/// and template math to the framework-neutral Core implementation.
/// </summary>
public sealed class TumourGenerator
{
    /// <summary>Gets or sets the tumour sampling resolution.</summary>
    public double Resolution { get; set; } = 1;

    /// <summary>Gets or sets the global tumour size multiplier.</summary>
    public double Scalar { get; set; } = 1;

    /// <summary>Gets or sets whether only middle anchors are retained.</summary>
    public bool JustMiddleAnchors { get; set; }

    /// <summary>Gets or sets the legacy layer collection adapted to Core.</summary>
    public IReadOnlyList<ITumourLayer> TumourLayers { get; set; } = [];

    /// <summary>Gets the shared slider reconstruction strategy.</summary>
    public Reconstructor Reconstructor { get; init; } = new();

    /// <summary>Gets or sets the random source used by unseeded layers.</summary>
    public Random Random { get; set; } = new();

    /// <summary>Gets the layer lengths reported by the last generation.</summary>
    public IReadOnlyList<double> LayerLengths { get; private set; } = [];

    /// <summary>Delegates tumour generation to the Core engine.</summary>
    /// <param name="hitObject">The slider to mutate.</param>
    /// <param name="cancellationToken">Cancels generation.</param>
    /// <returns>Whether a new path was written.</returns>
    public bool TumourGenerate(Mapping_Tools.Core.Classes.BeatmapHelper.HitObject hitObject, CancellationToken cancellationToken = default)
    {
        CoreTumourGenerator core = CreateCore();
        bool generated = core.TumourGenerate(hitObject, cancellationToken);
        LayerLengths = core.LayerLengths.ToArray();
        return generated;
    }

    /// <summary>Delegates one legacy path interval to the Core placement algorithm.</summary>
    /// <param name="pathWithHints">The mutable slider path.</param>
    /// <param name="tumourLayer">The legacy layer to adapt.</param>
    /// <param name="layer">The hint precedence layer.</param>
    /// <param name="start">The interval start node.</param>
    /// <param name="end">The interval end node.</param>
    /// <param name="startTemplateT">The template start progress.</param>
    /// <param name="endTemplateT">The template end progress.</param>
    /// <param name="tumourStart">The sequence start distance.</param>
    /// <param name="tumourEnd">The sequence end distance.</param>
    /// <param name="otherSide">Whether to mirror the tumour.</param>
    /// <param name="initialLength">The original slider length.</param>
    public void PlaceTumour(
        PathWithHints pathWithHints,
        ITumourLayer tumourLayer,
        int layer,
        LinkedListNode<PathPoint> start,
        LinkedListNode<PathPoint> end,
        double startTemplateT,
        double endTemplateT,
        double tumourStart,
        double tumourEnd,
        bool otherSide,
        double initialLength)
    {
        CreateCore().PlaceTumour(
            pathWithHints,
            ConvertLayer(tumourLayer),
            layer,
            start,
            end,
            startTemplateT,
            endTemplateT,
            tumourStart,
            tumourEnd,
            otherSide,
            initialLength);
    }

    private CoreTumourGenerator CreateCore()
    {
        return new CoreTumourGenerator
        {
            Resolution = Resolution,
            Scalar = Scalar,
            JustMiddleAnchors = JustMiddleAnchors,
            TumourLayers = TumourLayers.Select(ConvertLayer).ToArray(),
            Reconstructor = Reconstructor,
            Random = Random
        };
    }

    private static CoreTumourLayer ConvertLayer(ITumourLayer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        CoreTumourTemplate template = source.TumourTemplate switch
        {
            Options.TumourTemplates.SquareTemplate => CoreTumourTemplate.Square,
            Options.TumourTemplates.CircleTemplate => CoreTumourTemplate.Circle,
            Options.TumourTemplates.ParabolaTemplate => CoreTumourTemplate.Parabola,
            _ => CoreTumourTemplate.Triangle
        };
        return new CoreTumourLayer
        {
            TumourTemplateEnum = template,
            WrappingMode = (CoreWrappingMode)source.WrappingMode,
            TumourSidedness = (CoreTumourSidedness)source.TumourSidedness,
            TumourLength = ConvertGraphState(source.TumourLength),
            TumourScale = ConvertGraphState(source.TumourScale),
            TumourRotation = ConvertGraphState(source.TumourRotation),
            TumourParameter = ConvertGraphState(source.TumourParameter),
            TumourDistance = ConvertGraphState(source.TumourDistance),
            TumourCount = source.TumourCount,
            TumourStart = source.TumourStart,
            TumourEnd = source.TumourEnd,
            RandomSeed = source.RandomSeed,
            UseAbsoluteRange = source.UseAbsoluteRange,
            Recalculate = source.Recalculate,
            IsActive = source.IsActive
        };
    }

    private static CoreGraphState ConvertGraphState(LegacyGraphState source)
    {
        if (source is null) return CoreGraphState.CreateDefault();
        List<CoreGraphAnchor> anchors = (source.Anchors ?? []).Select(anchor =>
        {
            IGraphInterpolator interpolator = CreateInterpolator(anchor.Interpolator);
            return new CoreGraphAnchor(anchor.Pos, interpolator, anchor.Tension);
        }).ToList();
        return new CoreGraphState(anchors, source.MinX, source.MinY, source.MaxX, source.MaxY);
    }

    private static IGraphInterpolator CreateInterpolator(Mapping_Tools.Components.Graph.Interpolation.IGraphInterpolator source)
    {
        Type? type = CoreGraphInterpolatorCatalog.GetInterpolators()
            .FirstOrDefault(candidate => candidate.Name == source.GetType().Name);
        IGraphInterpolator result = type is null
            ? new CustomInterpolator(source.GetInterpolation)
            : CoreGraphInterpolatorCatalog.GetInterpolator(type);
        result.P = source.P;
        return result;
    }
}
