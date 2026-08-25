using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the next point using the same angle and velocity change.</summary>
public sealed class SameTransformGenerator3 : RelevantObjectsGenerator
{
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator3()
    {
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Successor of 3 Points";

    /// <inheritdoc />
    public override string Tooltip => "Takes 3 virtual points and calculates the next virtual point using the same angle and velocity change.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

    /// <summary>Projects a complex velocity transform.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3)
    {
        var a = point2.Child - point1.Child;
        var b = point3.Child - point2.Child;
        return Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon ? null : new RelevantPoint(Vector2.ComplexProduct(b, Vector2.ComplexQuotient(b, a)) + point3.Child);
    }
}

