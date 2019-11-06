using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects {
    public class RelevantPoint : RelevantDrawable {
        public readonly Vector2 Child;

        public override double DistanceTo(Vector2 point) {
            return Vector2.Distance(Child, point);
        }

        public override bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            intersections = new[] { Child };
            switch (other) {
                case RelevantPoint point:
                    return Precision.AlmostEquals(point.Child.X, Child.X) & Precision.AlmostEquals(point.Child.Y, Child.Y);
                case RelevantLine line:
                    return Precision.AlmostEquals(Line2.Distance(line.Child, Child), 0);
                case RelevantCircle circle:
                    return Precision.AlmostEquals(Vector2.Distance(circle.Child.Centre, Child), circle.Child.Radius);
                default:
                    return false;
            }
        }

        public override void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            var roPref = preferences.GetReleventObjectPreferences("Virtual point preferences");
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(Child));
            context.DrawEllipse(null, roPref.GetPen(), new Point(cPos.X, cPos.Y), roPref.Size, roPref.Size);
        }

        public override Vector2 NearestPoint(Vector2 point) {
            return Child;
        }

        public RelevantPoint(Vector2 vec) {
            Child = vec;
        }

        public override double DistanceTo(IRelevantObject relevantObject) {
            if (!(relevantObject is RelevantPoint relevantPoint)) {
                return double.PositiveInfinity;
            }

            return Vector2.Distance(Child, relevantPoint.Child);
        }
    }
}