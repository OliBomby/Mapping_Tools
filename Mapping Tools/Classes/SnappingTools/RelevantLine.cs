using Mapping_Tools.Classes.MathUtil;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Line = Mapping_Tools.Classes.MathUtil.Line;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantLine : IRelevantObject {
        public readonly Line child;
        public static Pen DefaultPen = new Pen()
        {
            Brush = new SolidColorBrush
            {
                Color = Colors.LawnGreen,
                Opacity = 0.8,
            },
            DashStyle = DashStyles.Dash,
            Thickness = 3,
        };
        public Pen Pen { get => FetchPrefferencesPen(); set => FetchPrefferencesPen(); }
        public bool IsHighlighted;

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
            if (!Line.Intersection(new Box2 { Bottom = 0, Left = 0, Top = 384, Right = 512 }, child, out Vector2[] points)) { return; }
            var cPos1 = converter.EditorToRelativeCoordinate(points[0]);
            var cPos2 = converter.EditorToRelativeCoordinate(points[1]);

            context.DrawLine(Pen, new Point(cPos1.X, cPos1.Y), new Point(cPos2.X, cPos2.Y));
        }

        public Vector2 NearestPoint(Vector2 point) {
            double a = child.A, b = child.B, c = -child.C;
            double abs = a * a + b * b;
            double x = (b * (b * point.X - a * point.Y) - a * c) / abs;
            double y = (a * (-b * point.X + a * point.Y) - b * c) / abs;
            return new Vector2(x, y);
        }

        public Pen FetchPrefferencesPen()
        {
            throw new System.NotImplementedException();
        }

        public RelevantLine(Line line) {
            child = line;
        }
    }
}
