using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Line2 = Mapping_Tools.Classes.MathUtil.Line2;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantLine : IRelevantObject {
        public readonly Line2 child;

        public bool IsHighlighted;

        public double DistanceTo(Vector2 point) {
            return Line2.Distance(child, point);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            switch (other) {
                case RelevantPoint point:
                    intersections = new[] { point.child };
                    return Precision.AlmostEquals(Line2.Distance(child, point.child), 0);
                case RelevantLine line: {
                    bool IsIntersecting = Line2.Intersection(child, line.child, out var intersection);
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
            if (!Line2.Intersection(new Box2 { Left = -1000, Top = -1000, Right = 1512, Bottom = 1384 }, child, out var points)) { return; }
            RelevantObjectPreferences roPref = preferences.GetReleventObjectPreferences("Virtual line preferences");
            var cPos1 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[0]));
            var cPos2 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[1]));
            context.DrawLine(roPref.Pen, new Point(cPos1.X, cPos1.Y), new Point(cPos2.X, cPos2.Y));
        }

        public Vector2 NearestPoint(Vector2 point) {
            return Line2.NearestPoint(child, point);
        }

        public RelevantLine(Line2 line) {
            child = line;
        }
    }
}