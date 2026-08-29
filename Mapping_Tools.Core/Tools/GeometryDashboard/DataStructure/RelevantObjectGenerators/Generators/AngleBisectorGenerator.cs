using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the two angle bisectors of two intersecting lines.</summary>
public sealed class AngleBisectorGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public AngleBisectorGenerator()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 });
    }

    /// <inheritdoc />
    public override string Name => "Bisectors of Angles";

    /// <inheritdoc />
    public override string Description => "Takes a pair virtual lines and generates the bisector of the angle between those lines at the point of the intersection.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates both bisectors when the input lines intersect.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine[]? GetRelevantObjects(RelevantLine line1, RelevantLine line2)
    {
        if (!Line2.Intersection(line1.Child, line2.Child, out var intersection)) return null;
        var direction1 = Vector2.Normalize(line1.Child.DirectionVector);
        var direction2 = Vector2.Normalize(line2.Child.DirectionVector);
        return [new RelevantLine(new Line2(intersection, direction1 + direction2)), new RelevantLine(new Line2(intersection, direction1 - direction2))];
    }
}
