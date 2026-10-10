using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the osu!stable cursor-tracking position at the nominal legacy tail judgement time.</summary>
public sealed class TrueSliderEndGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc />
    public override string Id => "true-slider-end";

    /// <summary>Creates the inactive-by-default true sliderend generator.</summary>
    public TrueSliderEndGenerator() { Settings.RelevancyRatio = 0.8; }

    /// <inheritdoc />
    public override string Name => "Points on True Slider Ends";

    /// <inheritdoc />
    public override string Description => "Generates osu!stable true slider ends using legacy integer timing and segment interpolation. Lazer uses different tracking and tail leniency.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    /// <summary>Generates a point at the tail judgement time, following the slider's repeats.</summary>
    /// <param name="relevantHitObject">The hit object supplying the slider path and timing.</param>
    /// <returns>The true sliderend point, or null for non-sliders and sliders without a path.</returns>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (hitObject is not { IsSlider: true, ControlPoints.Count: > 0 }) return null;

        int time = hitObject.GetLegacyTrueSliderEndTime();

        return new RelevantPoint(hitObject.GetLegacySliderBallPositionAtTime(time))
        {
            CustomTime = time,
        };
    }
}
