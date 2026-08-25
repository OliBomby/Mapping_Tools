using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a line parallel to a source line through a point.</summary>
public sealed class ParallelismGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public ParallelismGenerator()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Parallel Lines";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is parallel to the line.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates a parallel line.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point)
    {
        return new RelevantLine(new Line2(point.Child, line.Child.DirectionVector));
    }
}

