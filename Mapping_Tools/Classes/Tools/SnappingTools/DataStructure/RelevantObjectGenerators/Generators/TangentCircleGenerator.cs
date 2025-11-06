using System;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

public class TangentCircleGenerator : RelevantObjectsGenerator {
    public override string Name => "Tangent Circles on Circle";
    public override string Tooltip => "Takes a virtual circle and two points and generates virtual circles which intersect the circle in exactly one point.";
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    public TangentCircleGenerator() {
        Settings.IsActive = true;
        Settings.IsSequential = false;
        Settings.IsDeep = false;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true, MinRelevancy = 0.8});
    }

    [RelevantObjectsGeneratorMethod]
    public RelevantCircle[] GetRelevantObjects(RelevantCircle circle, RelevantPoint point1, RelevantPoint point2) {
        var p1 = point1.Child;
        var p2 = point2.Child;
        var c = circle.Child.Centre;
        var r = circle.Child.Radius;

        // If the points are too close to each other abort
        if (Precision.AlmostEquals(Vector2.DistanceSquared(p1, p2), 0)) {
            return Array.Empty<RelevantCircle>();
        }

        var d1 = Vector2.Distance(c, p1);
        var d2 = Vector2.Distance(c, p2);

        // For simplicity make point1 the closest point
        if (d1 > d2) {
            (p1, p2) = (p2, p1);
            (d1, d2) = (d2, d1);
        }

        // If one point is inside the circle and one point outside then there is no solution
        if (Precision.DefinitelyBigger(r, d1) && Precision.DefinitelyBigger(d2, r)) {
            return Array.Empty<RelevantCircle>();
        }

        // If one point is exactly on the circle then we just need the intersection of the perpendicular bisector
        // and the line between the circle centre and the point on the circle
        if (Precision.AlmostEquals(r, d1, 0.5) || Precision.AlmostEquals(r, d2, 0.5)) {
            // For simplicity make point1 the point on the circle
            if (Precision.AlmostEquals(r, d2, 0.5)) {
                (p1, p2) = (p2, p1);
            }

            var bisector = new Line2((p1 + p2) / 2, (p2 - p1).PerpendicularLeft);
            var connectingLine = Line2.FromPoints(c, p1);
            var centre = Line2.Intersection(bisector, connectingLine);

            return new[] { new RelevantCircle(new Circle(centre, p1)) };
        }

        // Transform all coordinates such that c and p1 are on the x-axis and (0,0) is in the middle of them
        var middle = (c + p1) / 2;
        var xAxis = (p1 - c).Normalized();
        var yAxis = xAxis.PerpendicularLeft;
        var transform = new Matrix2(xAxis, yAxis);

        var p1t = Matrix2.Mult(transform, p1 - middle);
        var p2t = Matrix2.Mult(transform, p2 - middle);
        var bisector2 = new Line2((p1t + p2t) / 2, (p2t - p1t).PerpendicularLeft);

        var d3 = d1 / 2;
        var a = r / 2;
        var b = Math.Sqrt(Math.Abs(a * a - d3 * d3));
        Vector2 c1;  // Circle centre 1
        Vector2 c2;  // Circle centre 2
        // There are two cases, either both points are inside the circle or both are outside the circle
        // and they need to be handled differently
        if (Precision.DefinitelyBigger(r, d1)) {
            // Both are inside, use the ellipsis
            // Calculate intersection points of the bisector and ellipsis (x/a)^2 + (y/b)^2 = 1
            if (!EllipsisIntersection(bisector2, a, b, out c1, out c2)) {
                return Array.Empty<RelevantCircle>();
            }
        } else {
            // Both are outside, use the hyperbola
            // Calculate intersection points of the bisector and hyperbola (x/a)^2 - (y/b)^2 = 1
            if (!HyperbolaIntersection(bisector2, a, b, out c1, out c2)) {
                return Array.Empty<RelevantCircle>();
            }
        }

        // Transform the coordinates back to the original space
        transform.Transpose();
        c1 = Matrix2.Mult(transform, c1) + middle;
        c2 = Matrix2.Mult(transform, c2) + middle;

        // In the case that there is only one solution
        if (double.IsNaN(c2.X)) {
            return new[] { new RelevantCircle(new Circle(c1, Vector2.Distance(c1, p1))) };
        }

        return new[] {
            new RelevantCircle(new Circle(c1, Vector2.Distance(c1, p1))),
            new RelevantCircle(new Circle(c2, Vector2.Distance(c2, p1)))
        };
    }

    private static bool EllipsisIntersection(Line2 line, double a, double b, out Vector2 p1, out Vector2 p2) {
        var x1 = line.PositionVector.X;
        var y1 = line.PositionVector.Y;
        var dx = line.DirectionVector.X;
        var dy = line.DirectionVector.Y;
        var c1 = b * b * dx * dx + a * a * dy * dy;
        var c2 = 2 * (b * b * x1 * dx + a * a * y1 * dy);
        var c3 = b * b * x1 * x1 + a * a * y1 * y1 - a * a * b * b;

        if (!SolveQuadratic(c1, c2, c3, out var t1, out var t2)) {
            p1 = p2 = Vector2.NaN;
            return false;
        }

        p1 = line.PositionVector + t1 * line.DirectionVector;
        p2 = line.PositionVector + t2 * line.DirectionVector;

        // Filter out far out solutions because they are unstable
        if (Math.Abs(t2) > 100) {
            p2 = Vector2.NaN;
        }
        if (Math.Abs(t1) > 100) {
            p1 = p2;
            p2 = Vector2.NaN;
        }
        return true;
    }

    private static bool HyperbolaIntersection(Line2 line, double a, double b, out Vector2 p1, out Vector2 p2) {
        var x1 = line.PositionVector.X;
        var y1 = line.PositionVector.Y;
        var dx = line.DirectionVector.X;
        var dy = line.DirectionVector.Y;
        var c1 = b * b * dx * dx - a * a * dy * dy;
        var c2 = 2 * (b * b * x1 * dx - a * a * y1 * dy);
        var c3 = b * b * x1 * x1 - a * a * y1 * y1 - a * a * b * b;

        if (!SolveQuadratic(c1, c2, c3, out var t1, out var t2)) {
            p1 = p2 = Vector2.NaN;
            return false;
        }

        p1 = line.PositionVector + t1 * line.DirectionVector;
        p2 = line.PositionVector + t2 * line.DirectionVector;

        // Filter out far out solutions because they are unstable
        if (Math.Abs(t2) > 100) {
            p2 = Vector2.NaN;
        }
        if (Math.Abs(t1) > 100) {
            p1 = p2;
            p2 = Vector2.NaN;
        }
        return true;
    }

    private static bool SolveQuadratic(double a, double b, double c, out double t1, out double t2) {
        if (Precision.AlmostEquals(a, 0)) {
            t2 = double.NaN;
            if (Precision.AlmostEquals(b, 0)) {
                t1 = double.NaN;
                return false;
            }

            // Solve linear equation
            t1 = -c / b;
            return true;
        }

        if (4 * a * c > b * b) {
            t1 = t2 = double.NaN;
            return false;
        }

        var s = Math.Sqrt(b * b - 4 * a * c);
        t1 = (-b + s) / (2 * a);
        t2 = (-b - s) / (2 * a);
        return true;
    }
}