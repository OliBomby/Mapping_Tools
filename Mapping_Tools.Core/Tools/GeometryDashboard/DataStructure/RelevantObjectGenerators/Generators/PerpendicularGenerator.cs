using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a line perpendicular to a source line through a point.</summary>
public sealed class PerpendicularGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public PerpendicularGenerator()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Perpendicular Lines";

    /// <inheritdoc />
    public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is perpendicular to the line.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates a perpendicular line.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point)
    {
        return new RelevantLine(new Line2(point.Child, line.Child.DirectionVector.PerpendicularLeft));
    }
}

