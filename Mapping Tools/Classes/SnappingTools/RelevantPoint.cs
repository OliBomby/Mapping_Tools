using Mapping_Tools.Classes.MathUtil;
using System.Windows;
using System.Windows.Media;
using Line = Mapping_Tools.Classes.MathUtil.Line;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantPoint : IRelevantObject {
        public readonly Vector2 child;

        public double DistanceTo(Vector2 point) {
            return Vector2.Distance(child, point);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            intersections = new[] { child };
            if (other is RelevantPoint point) {
                return Precision.AlmostEquals(point.child.X, child.X) & Precision.AlmostEquals(point.child.Y, child.Y);
            }

            if (other is RelevantLine line) {
                return Precision.AlmostEquals(Line.Distance(line.child, child), 0);
            }

            if (other is RelevantCircle circle) {
                return Precision.AlmostEquals(Vector2.Distance(circle.child.Centre, child), circle.child.Radius);
            }

            return false;
        }

        public void DrawYourself(DrawingContext context, CoordinateConverter converter)
        {
            var cPos = converter.EditorToRelativeCoordinate(child);
            context.DrawEllipse(null, new Pen(Brushes.Cyan, 5), new Point(cPos.X, cPos.Y), 5, 5);
        }

        public Vector2 NearestPoint(Vector2 point) {
            return child;
        }

        public RelevantPoint(Vector2 vec) {
            child = vec;
        }
    }
}
