using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Windows;
using System.Windows.Media;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantCircle : IRelevantObject {
        public readonly Circle child;

        public bool IsHighlighted;

        public double DistanceTo(Vector2 point) {
            var dist = Vector2.Distance(point, child.Centre);
            return Math.Abs(dist - child.Radius);
        }
        private static Pen GetPen(SnappingToolsPreferences preferences) {
            var pen = new Pen {
                Brush = new SolidColorBrush {
                    Color = preferences.CircleColor,
                    Opacity = preferences.CircleOpacity,
                },
                DashStyle = preferences.GetDashStyle(preferences.CircleDashstyle),
                Thickness = preferences.CircleThickness,
            };
            return pen;
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            switch (other) {
                case RelevantPoint point:
                    intersections = new[] { point.child };
                    return Precision.AlmostEquals(Vector2.Distance(child.Centre, point.child), child.Radius);
                case RelevantLine line:
                    return Circle.Intersection(child, line.child, out intersections);
                case RelevantCircle circle:
                    return Circle.Intersection(child, circle.child, out intersections);
                default:
                    intersections = new Vector2[0];
                    return false;
            }
        }

        public void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(child.Centre));
            var radius = converter.ToDpi(converter.ScaleByRatio(new Vector2(child.Radius)));
            context.DrawEllipse(null, GetPen(preferences), new Point(cPos.X, cPos.Y), radius.X, radius.Y);
        }

        public Vector2 NearestPoint(Vector2 point) {
            var diff = point - child.Centre;
            var dist = diff.Length;
            return child.Centre + diff / dist * child.Radius;
        }

        public RelevantCircle(Circle circle) {
            child = circle;
        }
    }
}
