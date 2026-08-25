using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the next point using a four-point transform.</summary>
public sealed class SameTransformGenerator4 : RelevantObjectsGenerator
{
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator4()
    {
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Successor of 4 Points";

    /// <inheritdoc />
    public override string Tooltip =>
        "Takes 4 virtual points and calculates the next virtual point using the same angle, angle change, velocity change and change of velocity change.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

    /// <summary>Projects the fourth-order complex transform.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3, RelevantPoint point4)
    {
        var a = point2.Child - point1.Child;
        var b = point3.Child - point2.Child;
        var c = point4.Child - point3.Child;
        if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon
            || Math.Abs(b.X) < double.Epsilon && Math.Abs(b.Y) < double.Epsilon
            || Math.Abs(c.X) < double.Epsilon && Math.Abs(c.Y) < double.Epsilon) return null;
        var d1 = Vector2.ComplexQuotient(b, a);
        var d2 = Vector2.ComplexQuotient(c, b);
        var dd = Vector2.ComplexQuotient(d2, d1);
        return new RelevantPoint(Vector2.ComplexProduct(c, Vector2.ComplexProduct(d2, dd)) + point4.Child);
    }
}

