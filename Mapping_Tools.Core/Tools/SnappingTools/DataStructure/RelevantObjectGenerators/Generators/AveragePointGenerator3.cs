using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the average of three points.</summary>
public sealed class AveragePointGenerator3 : RelevantObjectsGenerator
{
    /// <summary>Creates an active sequential deep generator.</summary>
    public AveragePointGenerator3()
    {
        Settings.IsActive = true;
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 });
    }

    /// <inheritdoc />
    public override string Name => "Average of Three Points";

    /// <inheritdoc />
    public override string Tooltip => "Takes three virtual points and calculates the average of the points.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Calculates the three-point average.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3)
    {
        return new RelevantPoint((point1.Child + point2.Child + point3.Child) / 3);
    }
}

