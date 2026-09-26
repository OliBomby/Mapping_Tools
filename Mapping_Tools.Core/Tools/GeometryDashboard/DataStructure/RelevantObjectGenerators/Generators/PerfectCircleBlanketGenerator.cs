using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the center points of perfect-curve slider segments' blankets.</summary>
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
    public override string Description => "Generates a virtual point at the blanket center of each perfect-curve segment.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates the center of each three-point perfect-curve segment.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider
            ? PerfectCircleGenerator.GetCircleArcs(hitObject).Select(arc => new RelevantPoint(new Circle(arc).Centre))
            : null;
    }
}
