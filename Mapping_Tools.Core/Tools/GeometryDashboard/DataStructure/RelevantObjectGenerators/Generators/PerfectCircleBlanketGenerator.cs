using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the center point of a perfect-curve slider's blanket.</summary>
public sealed class PerfectCircleBlanketGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active generator with a reduced relevance multiplier.</summary>
    public PerfectCircleBlanketGenerator()
    {
        Settings.RelevancyRatio = 0.8;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Points on Blanket Centers";

    /// <inheritdoc />
    public override string Description => "Takes a circular arc slider and generates a virtual point on its blanket center.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates the perfect-curve center when the slider has two control points.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.SliderType == PathType.PerfectCurve && hitObject.CurvePoints is { Count: 2 }
            ? new RelevantPoint(new Circle(new CircleArc(hitObject.GetAllCurvePoints())).Centre)
            : null;
    }
}
