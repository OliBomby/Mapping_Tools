using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates circles tangent to a source circle and passing through two points.</summary>
public sealed class TangentCircleGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active non-deep tangent-circle generator.</summary>
    public TangentCircleGenerator()
    {
        Settings.IsActive = true;
        Settings.IsSequential = false;
        Settings.IsDeep = false;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.8 });
    }

    /// <inheritdoc />
    public override string Name => "Tangent Circles on Circle";

    /// <inheritdoc />
    public override string Tooltip => "Takes a virtual circle and two points and generates virtual circles which intersect the circle in exactly one point.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    /// <summary>Generates all stable tangent-circle solutions.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle[] GetRelevantObjects(RelevantCircle circle, RelevantPoint point1, RelevantPoint point2)
    {
        var p1 = point1.Child;
        var p2 = point2.Child;
        var centre = circle.Child.Centre;
        double radius = circle.Child.Radius;
        // If the points are too close to each other abort
        if (Precision.AlmostEquals(Vector2.DistanceSquared(p1, p2), 0)) return Array.Empty<RelevantCircle>();

        double distance1 = Vector2.Distance(centre, p1);
        double distance2 = Vector2.Distance(centre, p2);
        if (Precision.AlmostEquals(distance1, 0) || Precision.AlmostEquals(distance2, 0)) return Array.Empty<RelevantCircle>();

        // For simplicity make point1 the closest point
        if (distance1 > distance2)
        {
            (p1, p2) = (p2, p1);
            (distance1, distance2) = (distance2, distance1);
        }

        // If one point is inside the circle and one point outside then there is no solution
        if (Precision.DefinitelyBigger(radius, distance1) && Precision.DefinitelyBigger(distance2, radius)) return Array.Empty<RelevantCircle>();

        if (Precision.AlmostEquals(radius, distance1, 0.5) || Precision.AlmostEquals(radius, distance2, 0.5))
        {
            // If one point is exactly on the circle then we just need the intersection of the perpendicular bisector
            // and the line between the circle centre and the point on the circle
            if (Precision.AlmostEquals(radius, distance2, 0.5)) (p1, p2) = (p2, p1);
            // For simplicity make point1 the point on the circle
            Line2 bisector = new((p1 + p2) / 2, (p2 - p1).PerpendicularLeft);
            var connectingLine = Line2.FromPoints(centre, p1);
            var solutionCentre = Line2.Intersection(bisector, connectingLine);
            return [new RelevantCircle(new Circle(solutionCentre, p1))];
        }

        // Transform all coordinates such that c and p1 are on the x-axis and (0,0) is in the middle of them
        var middle = (centre + p1) / 2;
        var xAxis = (p1 - centre).Normalized();
        var yAxis = xAxis.PerpendicularLeft;
        Matrix2 transform = new(xAxis, yAxis);
        var p1Transformed = Matrix2.Mult(transform, p1 - middle);
        var p2Transformed = Matrix2.Mult(transform, p2 - middle);
        Line2 bisectorTransformed = new((p1Transformed + p2Transformed) / 2, (p2Transformed - p1Transformed).PerpendicularLeft);
        double halfDistance = distance1 / 2;
        double halfRadius = radius / 2;
        double otherAxis = Math.Sqrt(Math.Abs(halfRadius * halfRadius - halfDistance * halfDistance));
        Vector2 firstCentre;
        Vector2 secondCentre;
        // There are two cases, either both points are inside the circle or both are outside the circle
        // and they need to be handled differently
        bool found = Precision.DefinitelyBigger(radius, distance1)
            // Both are inside, use the ellipsis
            ? EllipsisIntersection(bisectorTransformed, halfRadius, otherAxis, out firstCentre, out secondCentre)
            // Both are outside, use the hyperbola
            : HyperbolaIntersection(bisectorTransformed, halfRadius, otherAxis, out firstCentre, out secondCentre);
        if (!found) return Array.Empty<RelevantCircle>();

        // Transform the coordinates back to the original space
        transform.Transpose();
        firstCentre = Matrix2.Mult(transform, firstCentre) + middle;
        secondCentre = Matrix2.Mult(transform, secondCentre) + middle;
        // In the case that there is only one solution
        if (double.IsNaN(secondCentre.X)) return [new RelevantCircle(new Circle(firstCentre, Vector2.Distance(firstCentre, p1)))];
        return [new RelevantCircle(new Circle(firstCentre, Vector2.Distance(firstCentre, p1))), new RelevantCircle(new Circle(secondCentre, Vector2.Distance(secondCentre, p1)))];
    }

    private static bool EllipsisIntersection(Line2 line, double a, double b, out Vector2 p1, out Vector2 p2)
    {
        double x1 = line.PositionVector.X, y1 = line.PositionVector.Y, dx = line.DirectionVector.X, dy = line.DirectionVector.Y;
        double c1 = b * b * dx * dx + a * a * dy * dy;
        double c2 = 2 * (b * b * x1 * dx + a * a * y1 * dy);
        double c3 = b * b * x1 * x1 + a * a * y1 * y1 - a * a * b * b;
        // Calculate intersection points of the bisector and ellipsis (x/a)^2 + (y/b)^2 = 1
        if (!SolveQuadratic(c1, c2, c3, out double t1, out double t2))
        {
            p1 = p2 = Vector2.NaN;
            return false;
        }

        p1 = line.PositionVector + t1 * line.DirectionVector;
        p2 = line.PositionVector + t2 * line.DirectionVector;
        // Filter out far out solutions because they are unstable
        if (Math.Abs(t2) > 100) p2 = Vector2.NaN;
        if (Math.Abs(t1) > 100)
        {
            p1 = p2;
            p2 = Vector2.NaN;
        }

        return true;
    }

    private static bool HyperbolaIntersection(Line2 line, double a, double b, out Vector2 p1, out Vector2 p2)
    {
        double x1 = line.PositionVector.X, y1 = line.PositionVector.Y, dx = line.DirectionVector.X, dy = line.DirectionVector.Y;
        double c1 = b * b * dx * dx - a * a * dy * dy;
        double c2 = 2 * (b * b * x1 * dx - a * a * y1 * dy);
        double c3 = b * b * x1 * x1 - a * a * y1 * y1 - a * a * b * b;
        // Calculate intersection points of the bisector and hyperbola (x/a)^2 - (y/b)^2 = 1
        if (!SolveQuadratic(c1, c2, c3, out double t1, out double t2))
        {
            p1 = p2 = Vector2.NaN;
            return false;
        }

        p1 = line.PositionVector + t1 * line.DirectionVector;
        p2 = line.PositionVector + t2 * line.DirectionVector;
        // Filter out far out solutions because they are unstable
        if (Math.Abs(t2) > 100) p2 = Vector2.NaN;
        if (Math.Abs(t1) > 100)
        {
            p1 = p2;
            p2 = Vector2.NaN;
        }

        return true;
    }

    private static bool SolveQuadratic(double a, double b, double c, out double t1, out double t2)
    {
        // Solve linear equation
        if (Precision.AlmostEquals(a, 0))
        {
            t2 = double.NaN;
            if (Precision.AlmostEquals(b, 0))
            {
                t1 = double.NaN;
                return false;
            }

            t1 = -c / b;
            return true;
        }

        if (4 * a * c > b * b)
        {
            t1 = t2 = double.NaN;
            return false;
        }

        double s = Math.Sqrt(b * b - 4 * a * c);
        t1 = (-b + s) / (2 * a);
        t2 = (-b - s) / (2 * a);
        return true;
    }
}
