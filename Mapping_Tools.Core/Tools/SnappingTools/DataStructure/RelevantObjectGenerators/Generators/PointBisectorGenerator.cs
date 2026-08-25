using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates the perpendicular bisector of two points.</summary>
public sealed class PointBisectorGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active sequential deep generator.</summary>
    public PointBisectorGenerator()
    {
        Settings.IsActive = true;
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Bisector of Two Points";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair virtual points and generates the bisector of those points.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the perpendicular bisector.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        return new RelevantLine(new Line2((point1.Child + point2.Child) / 2, (point2.Child - point1.Child).PerpendicularLeft));
    }
}

