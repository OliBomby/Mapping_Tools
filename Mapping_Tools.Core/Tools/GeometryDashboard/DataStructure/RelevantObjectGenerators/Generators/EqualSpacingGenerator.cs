using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates two equal-radius circles centered on two points.</summary>
public sealed class EqualSpacingGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active sequential deep generator.</summary>
    public EqualSpacingGenerator()
    {
        Settings.IsActive = true;
        Settings.IsSequential = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
    }

    /// <inheritdoc />
    public override string Name => "Circles by Two Points";

    /// <inheritdoc />
    public override string Tooltip =>
        "Takes a pair of virtual points and generates a pair of virtual circles with their centers on each point. Their radius is equal to the spacing between the two.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates the two equal-spacing circles.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        double radius = (point1.Child - point2.Child).Length;
        return [new RelevantCircle(new Circle(point1.Child, radius)), new RelevantCircle(new Circle(point2.Child, radius))];
    }
}

