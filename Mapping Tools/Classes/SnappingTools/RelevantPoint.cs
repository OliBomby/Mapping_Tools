using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System.Windows;
using System.Windows.Media;
using Line = Mapping_Tools.Classes.MathUtil.Line;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantPoint : IRelevantObject {
        public readonly Vector2 child;

        public bool IsHighlighted;

        public double DistanceTo(Vector2 point) {
            return Vector2.Distance(child, point);
        }

        private static Pen GetPen(SnappingToolsPreferences preferences) {
             var pen = new Pen {
                 Brush = new SolidColorBrush {
                     Color = preferences.PointColor,
                     Opacity = preferences.PointOpacity,
                 },
                 DashStyle = preferences.GetDashStyle(preferences.PointDashstyle),
                 Thickness = preferences.PointThickness,
             };
            return pen;
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            intersections = new[] { child };
            switch (other) {
                case RelevantPoint point:
                    return Precision.AlmostEquals(point.child.X, child.X) & Precision.AlmostEquals(point.child.Y, child.Y);
                case RelevantLine line:
                    return Precision.AlmostEquals(Line.Distance(line.child, child), 0);
                case RelevantCircle circle:
                    return Precision.AlmostEquals(Vector2.Distance(circle.child.Centre, child), circle.child.Radius);
                default:
                    return false;
            }
        }

        public void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(child));
            context.DrawEllipse(null, GetPen(preferences), new Point(cPos.X, cPos.Y), preferences.PointSize, preferences.PointSize);
        }

        public Vector2 NearestPoint(Vector2 point) {
            return child;
        }

        public RelevantPoint(Vector2 vec) {
            child = vec;
        }
    }
}