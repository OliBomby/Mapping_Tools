using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the complete circle represented by a perfect-curve slider.</summary>
public sealed class PerfectCircleGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active generator with unit relevance.</summary>
    public PerfectCircleGenerator()
    {
        Settings.RelevancyRatio = 1;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Circles on 3-Point Sliders";

    /// <inheritdoc />
    public override string Tooltip => "Takes a circular arc slider and generates a virtual circle that completes the arc.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates the perfect-curve circle when the slider has two control points.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.SliderType == PathType.PerfectCurve && hitObject.CurvePoints is { Count: 2 }
            ? new RelevantCircle(new Circle(new CircleArc(hitObject.GetAllCurvePoints())))
            : null;
    }
}

