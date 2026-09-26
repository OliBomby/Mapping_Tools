using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a point on the last anchor of a slider.</summary>
public sealed class LastAnchorGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active generator with unit relevance.</summary>
    public LastAnchorGenerator()
    {
        Settings.RelevancyRatio = 1;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Points on Last Anchors";

    /// <inheritdoc />
    public override string Description => "Generates virtual points on the last anchors of sliders.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    /// <summary>Generates the slider's final curve point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (hitObject.ControlPoints.Count < 2) return null;
        return hitObject.IsSlider
            ? new RelevantPoint(hitObject.Pos + hitObject.ControlPoints[^1].Position) { CustomTime = hitObject.EndTime }
            : null;
    }
}
