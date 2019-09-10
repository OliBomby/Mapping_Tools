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
            List<Vector2> candidates = new List<Vector2>
            {
                new Vector2 {X = 0, Y = child.C / child.B },
                new Vector2 {X = child.C / child.A, Y = 0 },
                new Vector2 {X = (child.C - 384 * child.B) / child.A , Y = 384 },
                new Vector2 {X = 512 , Y = (child.C - 512 * child.A) / child.B },
            };

            List<Vector2> points = candidates.Where(p => (p[0] >= 0) && (p[0] <= 512) && (p[1] >= 0) && (p[1] <= 384)).ToList();
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
    }
}
