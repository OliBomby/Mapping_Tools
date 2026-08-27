using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the second equilateral-triangle orientation.</summary>
public sealed class TriangleGenerator2 : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep triangle generator.</summary>
    public TriangleGenerator2()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Equilateral Triangle from Two Points (Type II)";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make two equilateral triangles.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the second pair of triangle vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        var rotated = Vector2.Rotate(point2.Child - point1.Child, Math.PI * 5 / 6) / Math.Sqrt(3);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}
