using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the start position of every hit object.</summary>
public sealed class StartPointGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active generator with unit relevance.</summary>
    public StartPointGenerator()
    {
        Settings.RelevancyRatio = 1;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Points on Circles and Slider Heads";

    /// <inheritdoc />
    public override string Tooltip => "Generates virtual points on slider heads and circles.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates a hit object's start point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantHitObject hitObject)
    {
        return new RelevantPoint(hitObject.HitObject.Pos);
    }
}

