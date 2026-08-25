using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the next point using constant velocity.</summary>
public sealed class SameTransformGenerator2 : RelevantObjectsGenerator
{
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator2()
    {
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Successor of 2 Points";

    /// <inheritdoc />
    public override string Tooltip => "Takes 2 virtual points and calculates the next virtual point using the same velocity.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    /// <inheritdoc />
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

    /// <summary>Projects the last velocity once more.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        var difference = point2.Child - point1.Child;
        return Math.Abs(difference.X) < double.Epsilon && Math.Abs(difference.Y) < double.Epsilon ? null : new RelevantPoint(point2.Child + difference);
    }
}

