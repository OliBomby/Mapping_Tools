using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

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
    public override string Tooltip => "Generates virtual points on the last anchors of sliders.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    /// <summary>Generates the slider's final curve point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (hitObject.CurvePoints is null || hitObject.CurvePoints.Count == 0) return null;
        return hitObject.IsSlider && hitObject.CurvePoints is { Count: > 0 }
            ? new RelevantPoint(hitObject.CurvePoints.Last()) { CustomTime = hitObject.EndTime }
            : null;
    }
}

