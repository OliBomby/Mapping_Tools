using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects {
    public class RelevantPoint : RelevantDrawable {
        public static string PreferencesNameStatic => "Virtual point preferences";
        public override string PreferencesName => PreferencesNameStatic;

        public Vector2 Child { get; set; }

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

        public override void DrawYourself(DrawingContext context, CoordinateConverter converter, RelevantObjectPreferences preferences, Pen pen) {
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(Child));
            context.DrawEllipse(null, pen, new Point(cPos.X, cPos.Y), preferences.Size, preferences.Size);
        }

        public override Vector2 NearestPoint(Vector2 point) {
            return Child;
        }

        [UsedImplicitly]
        public RelevantPoint() { }

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