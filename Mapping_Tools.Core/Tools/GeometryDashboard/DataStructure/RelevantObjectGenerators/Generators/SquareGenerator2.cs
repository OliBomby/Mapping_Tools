using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates all four points completing the two square orientations.</summary>
public sealed class SquareGenerator2 : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator.</summary>
    public SquareGenerator2()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Square from Two Points (Type II)";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual points and generates a pair of virtual points on each side to make two squares in total.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the four perpendicular square vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        var rotated = Vector2.Rotate(point2.Child - point1.Child, Math.PI / 2);
        return
        [
            new RelevantPoint(point1.Child - rotated), new RelevantPoint(point1.Child + rotated), new RelevantPoint(point2.Child - rotated),
            new RelevantPoint(point2.Child + rotated),
        ];
    }
}

