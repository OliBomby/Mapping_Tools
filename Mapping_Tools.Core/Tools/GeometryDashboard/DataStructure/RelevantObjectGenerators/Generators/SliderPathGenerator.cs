using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates sampled points along slider paths.</summary>
public sealed class SliderPathGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the non-inheritable path sampler with legacy density.</summary>
    public SliderPathGenerator() : base(new SliderPathGeneratorSettings())
    {
        Settings.Generator = this;
        Settings.RelevancyRatio = 0.6;
        Settings.GeneratesInheritable = false;
        MySettings.PointDensity = 0.5;
    }

    /// <inheritdoc />
    public override string Name => "Points on Slider Paths";

    /// <inheritdoc />
    public override string Description => "Generates many virtual points on the paths of sliders. The density of generated points is configurable.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    private SliderPathGeneratorSettings MySettings => (SliderPathGeneratorSettings)Settings;

    /// <summary>Generates points along the slider path.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[]? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (!hitObject.IsSlider || hitObject.CurvePoints is null) return null;
        int numberOfPoints = (int)(hitObject.PixelLength * MySettings.PointDensity);
        if (numberOfPoints <= 0) return Array.Empty<RelevantPoint>();

        var points = new RelevantPoint[numberOfPoints];
        var sliderPath = hitObject.GetSliderPath();
        for (int i = 0; i < numberOfPoints; i++)
        {
            double fraction = numberOfPoints == 1 ? 0 : (double)i / (numberOfPoints - 1);
            points[i] = new RelevantPoint(sliderPath.PositionAt(fraction)) { CustomTime = fraction * (hitObject.EndTime - hitObject.Time) + hitObject.Time };
        }

        return points;
    }
}
