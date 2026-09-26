using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a line matching a linear slider.</summary>
public sealed class LinearLineGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active generator with unit relevance.</summary>
    public LinearLineGenerator()
    {
        Settings.RelevancyRatio = 1;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Lines on Linear Sliders";

    /// <inheritdoc />
    public override string Description => "Takes a linear slider and generates a virtual line that matches it.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates the line represented by a linear slider.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject is { IsSlider: true, ControlPoints.Count: >= 2 } &&
               hitObject.ControlPoints[0].Type == PathType.Linear &&
               hitObject.ControlPoints.Skip(1).All(point => !point.Type.HasValue)
            ? new RelevantLine(Line2.FromPoints(hitObject.Pos, hitObject.Pos + hitObject.ControlPoints[^1].Position))
            : null;
    }
}
