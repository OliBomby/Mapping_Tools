using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates tangent lines from a point to a circle.</summary>
public sealed class CircleTangentGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public CircleTangentGenerator()
    {
        Settings.IsActive = true;
        Settings.IsSequential = false;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 });
    }

    /// <inheritdoc />
    public override string Name => "Tangent Lines on Circle";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual circle and point and generates virtual lines that stretch to the sides of the circle and pass through the point.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates one or two tangent lines.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine[] GetRelevantObjects(RelevantPoint point, RelevantCircle circle)
    {
        var centre = circle.Child.Centre;
        double distance = Vector2.Distance(point.Child, centre);
        double radius = circle.Child.Radius;
        if (Precision.AlmostEquals(distance, 0)) return Array.Empty<RelevantLine>();

        if (distance - radius < 0.5) return [new RelevantLine(new Line2(point.Child, (point.Child - centre).PerpendicularLeft))];

        double scalar = radius / (distance * Math.Sqrt(1 - radius * radius / (distance * distance)));
        var offset = (point.Child - centre).PerpendicularLeft * scalar;
        return [new RelevantLine(Line2.FromPoints(point.Child, centre + offset)), new RelevantLine(Line2.FromPoints(point.Child, centre - offset))];
    }
}

