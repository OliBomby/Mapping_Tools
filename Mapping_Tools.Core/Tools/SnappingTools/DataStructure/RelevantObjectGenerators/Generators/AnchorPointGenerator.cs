using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates points at every anchor of a slider.</summary>
public sealed class AnchorPointGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active anchor generator with its legacy relevance multiplier.</summary>
    public AnchorPointGenerator()
    {
        Settings.RelevancyRatio = 0.8;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Points on Slider Anchors";

    /// <inheritdoc />
    public override string Tooltip => "Generates virtual points on the anchor points of sliders.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    /// <summary>Generates slider anchor points with interpolated timestamps.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (!hitObject.IsSlider || hitObject.CurvePoints is null) return null;
        var curvePoints = hitObject.GetAllCurvePoints();
        if (curvePoints.Count == 0) return Array.Empty<RelevantPoint>();

        int lastPointIndex = Math.Max(1, curvePoints.Count - 1);
        return curvePoints.Select((point, index) => new RelevantPoint(point)
        {
            CustomTime = (double)index / lastPointIndex * (hitObject.EndTime - hitObject.Time) + hitObject.Time,
        });
    }
}

