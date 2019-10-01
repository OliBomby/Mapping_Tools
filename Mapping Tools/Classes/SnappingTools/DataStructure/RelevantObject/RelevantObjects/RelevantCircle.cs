using System;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
    public class RelevantCircle : RelevantDrawable {
        public readonly Circle Child;

        public override double DistanceTo(Vector2 point) {
            var dist = Vector2.Distance(point, Child.Centre);
            return Math.Abs(dist - Child.Radius);
        }

        public override bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            switch (other) {
                case RelevantPoint point:
                    intersections = new[] { point.Child };
                    return Precision.AlmostEquals(Vector2.Distance(Child.Centre, point.Child), Child.Radius);
                case RelevantLine line:
                    return Circle.Intersection(Child, line.Child, out intersections);
                case RelevantCircle circle:
                    return Circle.Intersection(Child, circle.Child, out intersections);
                default:
                    intersections = new Vector2[0];
                    return false;
            }
        }

        public override void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            var roPref = preferences.GetReleventObjectPreferences("Virtual circle preferences");
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(Child.Centre));
            var radius = converter.ToDpi(converter.ScaleByRatio(new Vector2(Child.Radius)));
            context.DrawEllipse(null, roPref.GetPen(), new Point(cPos.X, cPos.Y), radius.X, radius.Y);
        }

        public override Vector2 NearestPoint(Vector2 point) {
            var diff = point - Child.Centre;
            var dist = diff.Length;
            return Child.Centre + diff / dist * Child.Radius;
        }

        public RelevantCircle(Circle circle) {
            Child = circle;
        }
    }
}
