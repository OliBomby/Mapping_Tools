using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Line = Mapping_Tools.Classes.MathUtil.Line;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantLine : IRelevantObject {
        public readonly Line child;

        public bool IsHighlighted;

        public double DistanceTo(Vector2 point) {
            return Line.Distance(child, point);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            switch (other) {
                case RelevantPoint point:
                    intersections = new[] { point.child };
                    return Precision.AlmostEquals(Line.Distance(child, point.child), 0);
                case RelevantLine line: {
                    bool IsIntersecting = Line.Intersection(child, line.child, out var intersection);
                    intersections = new[] { intersection };
                    return IsIntersecting;
                }
                case RelevantCircle circle:
                    return Circle.Intersection(circle.child, child, out intersections);
                default:
                    intersections = new Vector2[0];
                    return false;
            }
        }

        public void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            if (!Line.Intersection(new Box2 { Left = -1000, Top = -1000, Right = 1512, Bottom = 1384 }, child, out var points)) { return; }
            RelevantObjectPreferences roPref = preferences.GetReleventObjectPreferences("Virtual line preferences");
            var cPos1 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[0]));
            var cPos2 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[1]));
            context.DrawLine(roPref.Pen, new Point(cPos1.X, cPos1.Y), new Point(cPos2.X, cPos2.Y));
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