using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the midpoint of two points.</summary>
public sealed class AveragePointGenerator2 : RelevantObjectsGenerator
{
    /// <summary>Creates an active sequential deep generator.</summary>
    public AveragePointGenerator2()
    {
        Settings.IsActive = true;
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Average of Two Points";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual points and calculates the average of the points.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Calculates the midpoint.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        return new RelevantPoint((point1.Child + point2.Child) / 2);
    }
}

