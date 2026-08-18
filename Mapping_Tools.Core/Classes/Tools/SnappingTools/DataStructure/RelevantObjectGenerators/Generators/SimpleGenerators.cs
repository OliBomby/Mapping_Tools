using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates points at every anchor of a slider.</summary>
public sealed class AnchorPointGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Points on Slider Anchors";
    /// <inheritdoc/>
    public override string Tooltip => "Generates virtual points on the anchor points of sliders.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    /// <summary>Creates the active anchor generator with its legacy relevance multiplier.</summary>
    public AnchorPointGenerator() { Settings.RelevancyRatio = 0.8; Settings.IsActive = true; }

    /// <summary>Generates slider anchor points with interpolated timestamps.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (!hitObject.IsSlider || hitObject.CurvePoints is null) return null;
        var curvePoints = hitObject.GetAllCurvePoints();
        if (curvePoints.Count == 0) return Array.Empty<RelevantPoint>();

        int lastPointIndex = Math.Max(1, curvePoints.Count - 1);
        return curvePoints.Select((point, index) => new RelevantPoint(point)
        {
            CustomTime = (double)index / lastPointIndex * (hitObject.EndTime - hitObject.Time) + hitObject.Time
        });
    }
}

/// <summary>Generates the two angle bisectors of two intersecting lines.</summary>
public sealed class AngleBisectorGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Bisectors of Angles";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair virtual lines and generates the bisector of the angle between those lines at the point of the intersection.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public AngleBisectorGenerator() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 }); }

    /// <summary>Generates both bisectors when the input lines intersect.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine[]? GetRelevantObjects(RelevantLine line1, RelevantLine line2)
    {
        if (!Line2.Intersection(line1.Child, line2.Child, out Vector2 intersection)) return null;
        Vector2 direction1 = Vector2.Normalize(line1.Child.DirectionVector);
        Vector2 direction2 = Vector2.Normalize(line2.Child.DirectionVector);
        return [new RelevantLine(new Line2(intersection, direction1 + direction2)), new RelevantLine(new Line2(intersection, direction1 - direction2))];
    }
}

/// <summary>Generates the midpoint of two points.</summary>
public sealed class AveragePointGenerator2 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Average of Two Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and calculates the average of the points.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active sequential deep generator.</summary>
    public AveragePointGenerator2() { Settings.IsActive = true; Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Calculates the midpoint.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) => new((point1.Child + point2.Child) / 2);
}

/// <summary>Generates the average of three points.</summary>
public sealed class AveragePointGenerator3 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Average of Three Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes three virtual points and calculates the average of the points.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active sequential deep generator.</summary>
    public AveragePointGenerator3() { Settings.IsActive = true; Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 }); }
    /// <summary>Calculates the three-point average.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3) => new((point1.Child + point2.Child + point3.Child) / 3);
}

/// <summary>Generates tangent lines from a point to a circle.</summary>
public sealed class CircleTangentGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Tangent Lines on Circle";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual circle and point and generates virtual lines that stretch to the sides of the circle and pass through the point.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public CircleTangentGenerator() { Settings.IsActive = true; Settings.IsSequential = false; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 }); }
    /// <summary>Generates one or two tangent lines.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine[] GetRelevantObjects(RelevantPoint point, RelevantCircle circle)
    {
        Vector2 centre = circle.Child.Centre;
        double distance = Vector2.Distance(point.Child, centre);
        double radius = circle.Child.Radius;
        if (Precision.AlmostEquals(distance, 0)) return Array.Empty<RelevantLine>();

        if (distance - radius < 0.5)
        {
            return [new RelevantLine(new Line2(point.Child, (point.Child - centre).PerpendicularLeft))];
        }

        double scalar = radius / (distance * Math.Sqrt(1 - radius * radius / (distance * distance)));
        Vector2 offset = (point.Child - centre).PerpendicularLeft * scalar;
        return [new RelevantLine(Line2.FromPoints(point.Child, centre + offset)), new RelevantLine(Line2.FromPoints(point.Child, centre - offset))];
    }
}

/// <summary>Generates two equal-radius circles centered on two points.</summary>
public sealed class EqualSpacingGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Circles by Two Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and generates a pair of virtual circles with their centers on each point. Their radius is equal to the spacing between the two.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active sequential deep generator.</summary>
    public EqualSpacingGenerator() { Settings.IsActive = true; Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates the two equal-spacing circles.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        double radius = (point1.Child - point2.Child).Length;
        return [new RelevantCircle(new Circle(point1.Child, radius)), new RelevantCircle(new Circle(point2.Child, radius))];
    }
}

/// <summary>Generates points where pairs of lines or circles intersect.</summary>
public sealed class IntersectionGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Intersection Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual lines or circles and generates a virtual point on each of their intersections.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    /// <summary>Creates an active deep generator accepting sufficiently relevant inputs.</summary>
    public IntersectionGenerator() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { MinRelevancy = 0.2 }); }
    /// <summary>Generates a line-line intersection point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetLineLineIntersection(RelevantLine line1, RelevantLine line2) => Line2.Intersection(line1.Child, line2.Child, out Vector2 intersection) ? new RelevantPoint(intersection) : null;
    /// <summary>Generates line-circle intersection points.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetLineCircleIntersection(RelevantLine line, RelevantCircle circle) => Circle.Intersection(circle.Child, line.Child, out Vector2[] intersections) ? intersections.Select(point => new RelevantPoint(point)) : null;
    /// <summary>Generates circle-circle intersection points.</summary>
    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint>? GetCircleCircleIntersection(RelevantCircle circle1, RelevantCircle circle2) => Circle.Intersection(circle1.Child, circle2.Child, out Vector2[] intersections) ? intersections.Select(point => new RelevantPoint(point)) : null;
}

/// <summary>Generates the infinite line through two points.</summary>
public sealed class LineGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Lines by Two Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual line that connects the two.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active sequential deep generator.</summary>
    public LineGenerator() { Settings.IsActive = true; Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates the line through two points.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) => new(Line2.FromPoints(point1.Child, point2.Child));
}

/// <summary>Generates a point on the last anchor of a slider.</summary>
public sealed class LastAnchorGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Points on Last Anchors";
    /// <inheritdoc/>
    public override string Tooltip => "Generates virtual points on the last anchors of sliders.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;
    /// <summary>Creates the active generator with unit relevance.</summary>
    public LastAnchorGenerator() { Settings.RelevancyRatio = 1; Settings.IsActive = true; }
    /// <summary>Generates the slider's final curve point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (hitObject.CurvePoints is null || hitObject.CurvePoints.Count == 0) return null;
        return hitObject.IsSlider && hitObject.CurvePoints is { Count: > 0 }
            ? new RelevantPoint(hitObject.CurvePoints.Last()) { CustomTime = hitObject.EndTime }
            : null;
    }
}

/// <summary>Generates a line matching a linear slider.</summary>
public sealed class LinearLineGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Lines on Linear Sliders";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a linear slider and generates a virtual line that matches it.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <summary>Creates the active generator with unit relevance.</summary>
    public LinearLineGenerator() { Settings.RelevancyRatio = 1; Settings.IsActive = true; }
    /// <summary>Generates the line represented by a linear slider.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.SliderType == PathType.Linear && hitObject.CurvePoints is { Count: >= 1 }
            ? new RelevantLine(Line2.FromPoints(hitObject.Pos, hitObject.CurvePoints.Last())) : null;
    }
}

/// <summary>Generates a line parallel to a source line through a point.</summary>
public sealed class ParallelismGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Parallel Lines";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is parallel to the line.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public ParallelismGenerator() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates a parallel line.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point) => new(new Line2(point.Child, line.Child.DirectionVector));
}

/// <summary>Generates a line perpendicular to a source line through a point.</summary>
public sealed class PerpendicularGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Perpendicular Lines";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is perpendicular to the line.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep generator requiring selected inputs.</summary>
    public PerpendicularGenerator() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates a perpendicular line.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantLine line, RelevantPoint point) => new(new Line2(point.Child, line.Child.DirectionVector.PerpendicularLeft));
}

/// <summary>Generates the perpendicular bisector of two points.</summary>
public sealed class PointBisectorGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Bisector of Two Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair virtual points and generates the bisector of those points.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active sequential deep generator.</summary>
    public PointBisectorGenerator() { Settings.IsActive = true; Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates the perpendicular bisector.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) => new(new Line2((point1.Child + point2.Child) / 2, (point2.Child - point1.Child).PerpendicularLeft));
}

/// <summary>Generates the center point of a perfect-curve slider's blanket.</summary>
public sealed class PerfectCircleBlanketGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Points on Blanket Centers";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a circular arc slider and generates a virtual point on its blanket center.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <summary>Creates the active generator with a reduced relevance multiplier.</summary>
    public PerfectCircleBlanketGenerator() { Settings.RelevancyRatio = 0.8; Settings.IsActive = true; }
    /// <summary>Generates the perfect-curve center when the slider has two control points.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.SliderType == PathType.PerfectCurve && hitObject.CurvePoints is { Count: 2 }
            ? new RelevantPoint(new Circle(new CircleArc(hitObject.GetAllCurvePoints())).Centre) : null;
    }
}

/// <summary>Generates the complete circle represented by a perfect-curve slider.</summary>
public sealed class PerfectCircleGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Circles on 3-Point Sliders";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a circular arc slider and generates a virtual circle that completes the arc.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <summary>Creates the active generator with unit relevance.</summary>
    public PerfectCircleGenerator() { Settings.RelevancyRatio = 1; Settings.IsActive = true; }
    /// <summary>Generates the perfect-curve circle when the slider has two control points.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.SliderType == PathType.PerfectCurve && hitObject.CurvePoints is { Count: 2 }
            ? new RelevantCircle(new Circle(new CircleArc(hitObject.GetAllCurvePoints()))) : null;
    }
}

/// <summary>Generates the perpendicular points completing a square.</summary>
public sealed class SquareGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Square from Two Points (Type I)";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make a single square.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep generator.</summary>
    public SquareGenerator() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates the two remaining square vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        Vector2 diff = point2.Child - point1.Child;
        Vector2 rotated = Vector2.Rotate(diff, Math.PI * 3 / 4) / Math.Sqrt(2);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}

/// <summary>Generates all four points completing the two square orientations.</summary>
public sealed class SquareGenerator2 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Square from Two Points (Type II)";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and generates a pair of virtual points on each side to make two squares in total.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep generator.</summary>
    public SquareGenerator2() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates the four perpendicular square vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        Vector2 rotated = Vector2.Rotate(point2.Child - point1.Child, Math.PI / 2);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point1.Child + rotated), new RelevantPoint(point2.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}

/// <summary>Generates the next point using constant velocity.</summary>
public sealed class SameTransformGenerator2 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Successor of 2 Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes 2 virtual points and calculates the next virtual point using the same velocity.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator2() { Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true }); }
    /// <summary>Projects the last velocity once more.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        Vector2 difference = point2.Child - point1.Child;
        return Math.Abs(difference.X) < double.Epsilon && Math.Abs(difference.Y) < double.Epsilon ? null : new RelevantPoint(point2.Child + difference);
    }
}

/// <summary>Generates the next point using the same angle and velocity change.</summary>
public sealed class SameTransformGenerator3 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Successor of 3 Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes 3 virtual points and calculates the next virtual point using the same angle and velocity change.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator3() { Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true }); }
    /// <summary>Projects a complex velocity transform.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3)
    {
        Vector2 a = point2.Child - point1.Child;
        Vector2 b = point3.Child - point2.Child;
        return Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon ? null : new RelevantPoint(Vector2.ComplexProduct(b, Vector2.ComplexQuotient(b, a)) + point3.Child);
    }
}

/// <summary>Generates the next point using the reversed angle transform.</summary>
public sealed class SameTransformGenerator3Reversed : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Successor of 3 Points Reversed";
    /// <inheritdoc/>
    public override string Tooltip => "Takes 3 virtual points and calculates the next virtual point using the same velocity change and opposite angle.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator3Reversed() { Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true }); }
    /// <summary>Projects a reflected complex velocity transform.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3)
    {
        Vector2 a = point2.Child - point1.Child;
        Vector2 b = point3.Child - point2.Child;
        if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) return null;
        Vector2 difference = Vector2.ComplexQuotient(b, a);
        difference.Y = -difference.Y;
        return new RelevantPoint(Vector2.ComplexProduct(b, difference) + point3.Child);
    }
}

/// <summary>Generates the next point using a four-point transform.</summary>
public sealed class SameTransformGenerator4 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Successor of 4 Points";
    /// <inheritdoc/>
    public override string Tooltip => "Takes 4 virtual points and calculates the next virtual point using the same angle, angle change, velocity change and change of velocity change.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;
    /// <summary>Creates an ordered deep successor generator.</summary>
    public SameTransformGenerator4() { Settings.IsSequential = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true }); }
    /// <summary>Projects the fourth-order complex transform.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3, RelevantPoint point4)
    {
        Vector2 a = point2.Child - point1.Child;
        Vector2 b = point3.Child - point2.Child;
        Vector2 c = point4.Child - point3.Child;
        if ((Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) || (Math.Abs(b.X) < double.Epsilon && Math.Abs(b.Y) < double.Epsilon) || (Math.Abs(c.X) < double.Epsilon && Math.Abs(c.Y) < double.Epsilon)) return null;
        Vector2 d1 = Vector2.ComplexQuotient(b, a);
        Vector2 d2 = Vector2.ComplexQuotient(c, b);
        Vector2 dd = Vector2.ComplexQuotient(d2, d1);
        return new RelevantPoint(Vector2.ComplexProduct(c, Vector2.ComplexProduct(d2, dd)) + point4.Child);
    }
}

/// <summary>Generates a transformed point, line, or circle around a selected origin.</summary>
public sealed class ScaleRotateGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Scale & Rotate around a Fixed Point";
    /// <inheritdoc/>
    public override string Tooltip => "Spins and scales any virtual object around a fixed point by a specified angle and scalar. In the settings you can set the angle, scalar and extra rules for selecting the fixed point.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    private ScaleRotateGeneratorSettings MySettings => (ScaleRotateGeneratorSettings)Settings;

    /// <summary>Creates the active deep transform generator with legacy defaults.</summary>
    public ScaleRotateGenerator() : base(new ScaleRotateGeneratorSettings())
    {
        Settings.Generator = this; Settings.RelevancyRatio = 0.8; Settings.IsActive = true; Settings.IsDeep = true; MySettings.Angle = 180; MySettings.Scalar = 1;
        MySettings.OriginInputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, NeedLocked = true, NeedGeneratedNotByThis = true });
    }

    /// <summary>Transforms a point around a selected point origin.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        if (MySettings.OriginInputPredicate.Check(point1, this) && MySettings.OtherInputPredicate.Check(point2, this)) return new(Transform(point2.Child, point1.Child));
        if (MySettings.OriginInputPredicate.Check(point2, this) && MySettings.OtherInputPredicate.Check(point1, this)) return new(Transform(point1.Child, point2.Child));
        return null;
    }

    /// <summary>Transforms a line around a selected point origin.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantPoint origin, RelevantLine line) =>
        !MySettings.OriginInputPredicate.Check(origin, this) || !MySettings.OtherInputPredicate.Check(line, this) ? null : new RelevantLine(Line2.FromPoints(Transform(line.Child.PositionVector, origin.Child), Transform(line.Child.PositionVector + line.Child.DirectionVector, origin.Child)));

    /// <summary>Transforms a circle around a selected point origin.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle? GetRelevantObjects(RelevantPoint origin, RelevantCircle circle) =>
        !MySettings.OriginInputPredicate.Check(origin, this) || !MySettings.OtherInputPredicate.Check(circle, this) ? null : new RelevantCircle(new Circle(Transform(circle.Child.Centre, origin.Child), circle.Child.Radius * MySettings.Scalar));

    private Vector2 Transform(Vector2 point, Vector2 origin) => Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), point - origin) * MySettings.Scalar + origin;
}

/// <summary>Generates reflected points, lines, and circles across a selected axis.</summary>
public sealed class SymmetryGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Reflection across a Line";
    /// <inheritdoc/>
    public override string Tooltip => "Mirrors any virtual object over a virtual line where the virtual line is the symmetry axis. In the settings you can set extra rules for selecting the symmetry axis.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Advanced;
    private SymmetryGeneratorSettings MySettings => (SymmetryGeneratorSettings)Settings;

    /// <summary>Creates the active deep reflection generator with legacy defaults.</summary>
    public SymmetryGenerator() : base(new SymmetryGeneratorSettings())
    {
        Settings.Generator = this; Settings.RelevancyRatio = 0.8; Settings.IsActive = true; Settings.IsDeep = true;
        MySettings.AxisInputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, NeedLocked = true, NeedGeneratedNotByThis = true });
    }

    /// <summary>Reflects a point across an axis.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantLine axis, RelevantPoint point) => !MySettings.AxisInputPredicate.Check(axis, this) || !MySettings.OtherInputPredicate.Check(point, this) ? null : new RelevantPoint(Vector2.Mirror(point.Child, axis.Child));

    /// <summary>Reflects one line across another line.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantLine line1, RelevantLine line2)
    {
        if (MySettings.AxisInputPredicate.Check(line1, this) && MySettings.OtherInputPredicate.Check(line2, this)) return ReflectedLine(line1, line2);
        if (MySettings.AxisInputPredicate.Check(line2, this) && MySettings.OtherInputPredicate.Check(line1, this)) return ReflectedLine(line2, line1);
        return null;
    }

    /// <summary>Reflects a circle across an axis.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle? GetRelevantObjects(RelevantLine axis, RelevantCircle circle) => !MySettings.AxisInputPredicate.Check(axis, this) || !MySettings.OtherInputPredicate.Check(circle, this) ? null : new RelevantCircle(new Circle(Vector2.Mirror(circle.Child.Centre, axis.Child), circle.Child.Radius));

    private static RelevantLine ReflectedLine(RelevantLine axis, RelevantLine line) => new(Line2.FromPoints(Vector2.Mirror(line.Child.PositionVector, axis.Child), Vector2.Mirror(line.Child.PositionVector + line.Child.DirectionVector, axis.Child)));
}

/// <summary>Generates a configurable circle centered on each point.</summary>
public sealed class SinglePointCircleGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Circle from Single Point";
    /// <inheritdoc/>
    public override string Tooltip => "Generates circles with a specified radius on every virtual point.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    private SinglePointCircleGeneratorSettings MySettings => (SinglePointCircleGeneratorSettings)Settings;
    /// <summary>Creates the inactive generator with a 100-pixel radius.</summary>
    public SinglePointCircleGenerator() : base(new SinglePointCircleGeneratorSettings())
    {
        Settings.Generator = this; Settings.IsActive = false; Settings.IsDeep = false; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); MySettings.Radius = 100;
    }
    /// <summary>Generates a circle centered at the input point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle GetRelevantObjects(RelevantPoint point) => new(new Circle(point.Child, MySettings.Radius));
}

/// <summary>Generates sampled points along slider paths.</summary>
public sealed class SliderPathGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Points on Slider Paths";
    /// <inheritdoc/>
    public override string Tooltip => "Generates many virtual points on the paths of sliders. The density of generated points is configurable.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;
    private SliderPathGeneratorSettings MySettings => (SliderPathGeneratorSettings)Settings;
    /// <summary>Creates the non-inheritable path sampler with legacy density.</summary>
    public SliderPathGenerator() : base(new SliderPathGeneratorSettings())
    {
        Settings.Generator = this; Settings.RelevancyRatio = 0.6; Settings.GeneratesInheritable = false; MySettings.PointDensity = 0.5;
    }
    /// <summary>Generates points along the slider path.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[]? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        if (!hitObject.IsSlider || hitObject.CurvePoints is null) return null;
        int numberOfPoints = (int)(hitObject.PixelLength * MySettings.PointDensity);
        if (numberOfPoints <= 0) return Array.Empty<RelevantPoint>();

        RelevantPoint[] points = new RelevantPoint[numberOfPoints];
        var sliderPath = hitObject.GetSliderPath();
        for (int i = 0; i < numberOfPoints; i++)
        {
            double fraction = numberOfPoints == 1 ? 0 : (double)i / (numberOfPoints - 1);
            points[i] = new RelevantPoint(sliderPath.PositionAt(fraction)) { CustomTime = fraction * (hitObject.EndTime - hitObject.Time) + hitObject.Time };
        }
        return points;
    }
}

/// <summary>Generates a point at a slider's playable end.</summary>
public sealed class SliderEndGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Points on Slider Ends";
    /// <inheritdoc/>
    public override string Tooltip => "Generates virtual points on the actual ends of sliders.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <inheritdoc/>
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;
    /// <summary>Creates the inactive-by-default endpoint generator.</summary>
    public SliderEndGenerator() { Settings.RelevancyRatio = 0.8; }
    /// <summary>Generates the endpoint of a slider.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantHitObject relevantHitObject)
    {
        var hitObject = relevantHitObject.HitObject;
        return hitObject.IsSlider && hitObject.CurvePoints is not null
            ? new RelevantPoint(hitObject.GetSliderPath().PositionAt(1)) { CustomTime = hitObject.EndTime }
            : null;
    }
}

/// <summary>Generates the start position of every hit object.</summary>
public sealed class StartPointGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Points on Circles and Slider Heads";
    /// <inheritdoc/>
    public override string Tooltip => "Generates virtual points on slider heads and circles.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    /// <summary>Creates the active generator with unit relevance.</summary>
    public StartPointGenerator() { Settings.RelevancyRatio = 1; Settings.IsActive = true; }
    /// <summary>Generates a hit object's start point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantHitObject hitObject) => new(hitObject.HitObject.Pos);
}

/// <summary>Generates the line represented by a linear slider.</summary>
/// <remarks>This compatibility alias is intentionally separate from <see cref="LinearLineGenerator"/> only where the legacy catalog names differ.</remarks>
public sealed class TriangleGenerator : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Equilateral Triangle from Two Points (Type I)";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make two equilateral triangles.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep triangle generator.</summary>
    public TriangleGenerator() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedGeneratedByThis = true }); }
    /// <summary>Generates the two equilateral-triangle vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        Vector2 rotated = Vector2.Rotate(point2.Child - point1.Child, Math.PI * 2 / 3);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}

/// <summary>Generates the second equilateral-triangle orientation.</summary>
public sealed class TriangleGenerator2 : RelevantObjectsGenerator
{
    /// <inheritdoc/>
    public override string Name => "Equilateral Triangle from Two Points (Type II)";
    /// <inheritdoc/>
    public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make two equilateral triangles.";
    /// <inheritdoc/>
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;
    /// <summary>Creates an active deep triangle generator.</summary>
    public TriangleGenerator2() { Settings.IsActive = true; Settings.IsDeep = true; Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 }); }
    /// <summary>Generates the second pair of triangle vertices.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint[] GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        Vector2 rotated = Vector2.Rotate(point2.Child - point1.Child, Math.PI * 5 / 6) / Math.Sqrt(3);
        return [new RelevantPoint(point1.Child - rotated), new RelevantPoint(point2.Child + rotated)];
    }
}
