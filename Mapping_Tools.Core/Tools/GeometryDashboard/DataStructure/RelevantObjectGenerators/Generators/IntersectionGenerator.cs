using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates points where pairs of lines or circles intersect.</summary>
public sealed class IntersectionGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates an active deep generator accepting sufficiently relevant inputs.</summary>
    public IntersectionGenerator()
    {
        Settings.IsActive = true;
        Settings.IsDeep = true;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { MinRelevancy = 0.2 });
    }

    /// <inheritdoc />
    public override string Name => "Intersection Points";

    /// <inheritdoc />
    public override string Description => "Takes a pair of virtual lines or circles and generates a virtual point on each of their intersections.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    /// <summary>Generates a line-line intersection point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetLineLineIntersection(RelevantLine line1, RelevantLine line2)
    {
        return Line2.Intersection(line1.Child, line2.Child, out var intersection) ? new RelevantPoint(intersection) : null;
    }

    /// <summary>Generates line-circle intersection points.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetLineCircleIntersection(RelevantLine line, RelevantCircle circle)
    {
        return Circle.Intersection(circle.Child, line.Child, out var intersections) ? intersections.Select(point => new RelevantPoint(point)) : null;
    }

    /// <summary>Generates circle-circle intersection points.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetCircleCircleIntersection(RelevantCircle circle1, RelevantCircle circle2)
    {
        return Circle.Intersection(circle1.Child, circle2.Child, out var intersections) ? intersections.Select(point => new RelevantPoint(point)) : null;
    }
}
