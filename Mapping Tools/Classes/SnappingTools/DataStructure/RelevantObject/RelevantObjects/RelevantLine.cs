using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Line2 = Mapping_Tools.Classes.MathUtil.Line2;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
    public class RelevantLine : RelevantDrawable {
        public readonly Line2 Child;

        public override double DistanceTo(Vector2 point) {
            return Line2.Distance(Child, point);
        }

        public override bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            switch (other) {
                case RelevantPoint point:
                    intersections = new[] { point.Child };
                    return Precision.AlmostEquals(Line2.Distance(Child, point.Child), 0);
                case RelevantLine line: {
                    bool IsIntersecting = Line2.Intersection(Child, line.Child, out var intersection);
                    intersections = new[] { intersection };
                    return IsIntersecting;
                }
                case RelevantCircle circle:
                    return Circle.Intersection(circle.Child, Child, out intersections);
                default:
                    intersections = new Vector2[0];
                    return false;
            }
        }

        public override void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            if (!Line2.Intersection(new Box2 { Left = -1000, Top = -1000, Right = 1512, Bottom = 1384 }, Child, out var points)) { return; }
            var roPref = preferences.GetReleventObjectPreferences("Virtual line preferences");
            var cPos1 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[0]));
            var cPos2 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[1]));
            context.DrawLine(roPref.GetPen(), new Point(cPos1.X, cPos1.Y), new Point(cPos2.X, cPos2.Y));
        }

        public override Vector2 NearestPoint(Vector2 point) {
            return Line2.NearestPoint(Child, point);
        }

        public RelevantLine(Line2 line) {
            Child = line;
        }
    }
}