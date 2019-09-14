using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System.Windows;
using System.Windows.Media;
using Line = Mapping_Tools.Classes.MathUtil.Line;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantPoint : IRelevantObject {
        public readonly Vector2 child;
        private readonly SnappingToolsPreferences settings = SettingsManager.Settings.SnappingToolsPreferences;

        public bool IsHighlighted;

        public double DistanceTo(Vector2 point) {
            return Vector2.Distance(child, point);
        }

        private Pen GetDefaultPen() {
             Pen pen = new Pen() {
                 Brush = new SolidColorBrush {
                     Color = settings.PointColor,
                     Opacity = settings.PointOpacity,
                 },
                 DashStyle = settings.GetDashStyle(settings.PointDashstyle),
                 Thickness = settings.PointThickness,
             };
            return pen;
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

        public void DrawYourself(DrawingContext context, CoordinateConverter converter) {
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(child));
            context.DrawEllipse(null, GetDefaultPen(), new Point(cPos.X, cPos.Y), settings.PointSize, settings.PointSize);
        }

        public Vector2 NearestPoint(Vector2 point) {
            return child;
        }

        public RelevantPoint(Vector2 vec) {
            child = vec;
        }
    }
}