using Mapping_Tools.Classes.MathUtil;
using System;
using System.Windows;
using System.Windows.Media;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantCircle : IRelevantObject {
        public readonly Circle child;
        public static Pen DefaultPen = new Pen()
        {
            Brush = new SolidColorBrush
            {
                Color = Colors.Red,
                Opacity = 0.8,
            },
            DashStyle = DashStyles.Dash,
            Thickness = 3,
        };
        public Pen Pen { get => FetchPrefferencesPen(); set => FetchPrefferencesPen(); }
        public bool IsHighlighted;

        public double DistanceTo(Vector2 point) {
            var dist = Vector2.Distance(point, child.Centre);
            return Math.Abs(dist - child.Radius);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections)
        {
            if (other is RelevantPoint point) {
                intersections = new[] { point.child };
                return Precision.AlmostEquals(Vector2.Distance(child.Centre, point.child), child.Radius);
            }

            if (other is RelevantLine line) {
                return Circle.Intersection(child, line.child, out intersections);
            }

            if (other is RelevantCircle circle) {
                return Circle.Intersection(child, circle.child, out intersections);
            }

            intersections = new Vector2[0];
            return false;
        }

        public void DrawYourself(DrawingContext context, CoordinateConverter converter) {
            var cPos = converter.EditorToRelativeCoordinate(child.Centre);
            var radius = converter.EditorToScreenSize(child.Radius);
            context.DrawEllipse(null, Pen, new Point(cPos.X, cPos.Y), radius, radius);
        }

        public Vector2 NearestPoint(Vector2 point) {
            var diff = point - child.Centre;
            var dist = diff.Length;
            return child.Centre + diff / dist * child.Radius;
        }

        public Pen FetchPrefferencesPen()
        {
            
        }

        public RelevantCircle(Circle circle) {
            child = circle;
        }
    }
}
