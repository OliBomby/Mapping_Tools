using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the complete circles represented by perfect-curve slider segments.</summary>
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
    public override string Description => "Generates a virtual circle for each perfect-curve segment in a slider.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Basic;

    /// <summary>Generates a circle for each three-point perfect-curve segment.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantCircle>? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider
            ? GetCircleArcs(hitObject).Select(arc => new RelevantCircle(new Circle(arc)))
            : null;
    }

    internal static IEnumerable<CircleArc> GetCircleArcs(HitObject hitObject)
    {
        var points = hitObject.ControlPoints;
        if (points.Count == 0) yield break;

        int start = 0;
        PathType activeType = points[0].Type ?? PathType.Linear;
        for (int i = 0; i < points.Count; i++)
        {
            bool typedBoundary = i > start && points[i].Type.HasValue;
            if (i != points.Count - 1 && !typedBoundary) continue;

            if (activeType == PathType.PerfectCurve && i - start == 2)
                yield return new CircleArc(points.Skip(start).Take(3)
                    .Select(point => point.Position + hitObject.Pos).ToList());

            if (typedBoundary) activeType = points[i].Type!.Value;
            start = i;
        }
    }
}
