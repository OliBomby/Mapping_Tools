using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the infinite line through two points.</summary>
public sealed class LineGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active sequential deep generator.</summary>
    public LineGenerator()
    {
        Settings.IsActive = true;
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Lines by Two Points";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual line that connects the two.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the line through two points.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        return new RelevantLine(Line2.FromPoints(point1.Child, point2.Child));
    }
}

