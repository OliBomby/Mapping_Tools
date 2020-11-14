using System;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects {
    public class RelevantCircle : RelevantDrawable {
        public static string PreferencesNameStatic => "Virtual circle preferences";
        public override string PreferencesName => PreferencesNameStatic;

        public Circle Child { get; set; }

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

        public override void DrawYourself(DrawingContext context, CoordinateConverter converter, RelevantObjectPreferences preferences, Pen pen) {
            var cPos = converter.ToDpi(converter.EditorToRelativeCoordinate(Child.Centre));
            var radius = converter.ToDpi(converter.ScaleByRatio(new Vector2(Child.Radius)));
            context.DrawEllipse(null, pen, new Point(cPos.X, cPos.Y), radius.X, radius.Y);
        }

        public override Vector2 NearestPoint(Vector2 point) {
            var diff = point - Child.Centre;
            var dist = diff.Length;
            return Child.Centre + diff / dist * Child.Radius;
        }

        [UsedImplicitly]
        public RelevantCircle() { }

        public RelevantCircle(Circle circle) {
            Child = circle;
        }

        public override double DistanceTo(IRelevantObject relevantObject) {
            if (!(relevantObject is RelevantCircle relevantCircle)) {
                return double.PositiveInfinity;
            }

            return Vector2.Distance(Child.Centre, relevantCircle.Child.Centre) +
                   Math.Abs(Child.Radius - relevantCircle.Child.Radius);
        }
    }
}
