using Mapping_Tools.Classes.MathUtil;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Line = Mapping_Tools.Classes.MathUtil.Line;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantLine : IRelevantObject {
        public readonly Line child;

        public double DistanceTo(Vector2 point) {
            return Line.Distance(child, point);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections)
        {
            if (other is RelevantPoint point) {
                intersections = new[] { point.child };
                return Precision.AlmostEquals(Line.Distance(child, point.child), 0);
            }

            if (other is RelevantLine line) {
                bool IsIntersecting = Line.Intersection(child, line.child, out var intersection);
                intersections = new[] { intersection };
                return IsIntersecting;
            }

            if (other is RelevantCircle circle) {
                return Circle.Intersection(circle.child, child, out intersections);
            }

            intersections = new Vector2[0];
            return false;
        }

        public void DrawYourself(DrawingContext context, CoordinateConverter converter) {
            bool b = Line.Intersection(new Box2 { Bottom = 0, Left = 0, Top = 384, Right = 512 }, child, out Vector2[] points);
            var cPos1 = converter.EditorToRelativeCoordinate(points[0]);
            var cPos2 = converter.EditorToRelativeCoordinate(points[1]);

            context.DrawLine(new Pen(Brushes.LawnGreen, 5), new Point(cPos1.X, cPos1.Y), new Point(cPos2.X, cPos2.Y));
        }

        public Vector2 NearestPoint(Vector2 point) {
            double a = child.A, b = child.B, c = -child.C;
            double abs = a * a + b * b;
            double x = (b * (b * point.X - a * point.Y) - a * c) / abs;
            double y = (a * (-b * point.X + a * point.Y) - b * c) / abs;
            return new Vector2(x, y);
        }

        public RelevantLine(Line line) {
            child = line;
        }

        public struct Vector2Comparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 a, Vector2 b)
            {
                return a.X == b.X && a.Y == b.Y;
            }

            public int GetHashCode(Vector2 a)
            {
                return a.GetHashCode();
            }
        }
    }
}
