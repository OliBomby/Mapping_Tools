using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a point at a slider's playable end.</summary>
public sealed class SliderEndGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the inactive-by-default endpoint generator.</summary>
    public SliderEndGenerator() { Settings.RelevancyRatio = 0.8; }

    /// <inheritdoc />
    public override string Name => "Points on Slider Ends";

    /// <inheritdoc />
    public override string Description => "Generates virtual points on the actual ends of sliders.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    /// <summary>Generates the endpoint of a slider.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.CurvePoints is not null
            ? new RelevantPoint(hitObject.GetSliderPath().PositionAt(1)) { CustomTime = hitObject.EndTime }
            : null;
    }
}
