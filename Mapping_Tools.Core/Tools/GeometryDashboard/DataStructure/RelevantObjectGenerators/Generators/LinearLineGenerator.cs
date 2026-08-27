using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a line matching a linear slider.</summary>
public sealed class LinearLineGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active generator with unit relevance.</summary>
    public LinearLineGenerator()
    {
        Settings.RelevancyRatio = 1;
        Settings.IsActive = true;
    }

    /// <inheritdoc />
    public override string Name => "Lines on Linear Sliders";

    /// <inheritdoc />
    public override string Tooltip => "Takes a linear slider and generates a virtual line that matches it.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates the line represented by a linear slider.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.SliderType == PathType.Linear && hitObject.CurvePoints is { Count: >= 1 }
            ? new RelevantLine(Line2.FromPoints(hitObject.Pos, hitObject.CurvePoints.Last()))
            : null;
    }
}

