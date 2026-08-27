using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the line represented by a linear slider.</summary>
/// <remarks>
///     This compatibility alias is intentionally separate from <see cref="LinearLineGenerator" /> only where the
///     legacy catalog names differ.
/// </remarks>
public sealed class TriangleGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep triangle generator.</summary>
    public TriangleGenerator()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Equilateral Triangle from Two Points (Type I)";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make two equilateral triangles.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the two equilateral-triangle vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        var rotated = Vector2.Rotate(point2.Child - point1.Child, Math.PI * 2 / 3);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}

