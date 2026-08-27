using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the perpendicular points completing a square.</summary>
public sealed class SquareGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator.</summary>
    public SquareGenerator()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Square from Two Points (Type I)";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make a single square.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the two remaining square vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        var diff = point2.Child - point1.Child;
        var rotated = Vector2.Rotate(diff, Math.PI * 3 / 4) / Math.Sqrt(2);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}

