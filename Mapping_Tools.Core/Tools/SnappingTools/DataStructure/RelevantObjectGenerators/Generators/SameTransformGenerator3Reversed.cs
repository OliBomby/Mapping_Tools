using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the next point using the reversed angle transform.</summary>
public sealed class SameTransformGenerator3Reversed : RelevantObjectsGenerator
{
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator3Reversed()
    {
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Successor of 3 Points Reversed";

    /// <inheritdoc />
    public override string Tooltip => "Takes 3 virtual points and calculates the next virtual point using the same velocity change and opposite angle.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

    /// <summary>Projects a reflected complex velocity transform.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3)
    {
        var a = point2.Child - point1.Child;
        var b = point3.Child - point2.Child;
        if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) return null;
        var difference = Vector2.ComplexQuotient(b, a);
        difference.Y = -difference.Y;
        return new RelevantPoint(Vector2.ComplexProduct(b, difference) + point3.Child);
    }
}

