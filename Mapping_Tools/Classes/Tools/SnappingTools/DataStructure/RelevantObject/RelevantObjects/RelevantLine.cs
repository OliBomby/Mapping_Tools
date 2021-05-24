using System;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects {
    public class RelevantLine : RelevantDrawable {
        public static string PreferencesNameStatic => "Virtual line preferences";
        public override string PreferencesName => PreferencesNameStatic;

        public Line2 Child { get; set; }

        public override double DistanceTo(Vector2 point) {
            return Line2.Distance(Child, point);
        }

        public override bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            switch (other) {
                case RelevantPoint point:
                    intersections = new[] { point.Child };
                    return Precision.AlmostEquals(Line2.Distance(Child, point.Child), 0);
                case RelevantLine line: {
                    bool isIntersecting = Line2.Intersection(Child, line.Child, out var intersection);
                    intersections = new[] { intersection };
                    return isIntersecting;
                }
                case RelevantCircle circle:
                    return Circle.Intersection(circle.Child, Child, out intersections);
                default:
                    intersections = new Vector2[0];
                    return false;
            }
        }

        public override void DrawYourself(DrawingContext context, CoordinateConverter converter, RelevantObjectPreferences preferences, Pen pen) {
            if (!Line2.Intersection(new Box2 { Left = -1000, Top = -1000, Right = 1512, Bottom = 1384 }, Child, out var points)) { return; }
            var cPos1 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[0]));
            var cPos2 = converter.ToDpi(converter.EditorToRelativeCoordinate(points[1]));
            context.DrawLine(pen, new Point(cPos1.X, cPos1.Y), new Point(cPos2.X, cPos2.Y));
        }

        public override Vector2 NearestPoint(Vector2 point) {
            return Line2.NearestPoint(Child, point);
        }

        [UsedImplicitly]
        public RelevantLine() { }

        public RelevantLine(Line2 line) {
            Child = line;
        }

        public override double DistanceTo(IRelevantObject relevantObject) {
            if (!(relevantObject is RelevantLine relevantLine)) {
                return double.PositiveInfinity;
            }

            var cosAlpha = Vector2.Dot(Child.DirectionVector, relevantLine.Child.DirectionVector) /
                    (Child.DirectionVector.Length * relevantLine.Child.DirectionVector.Length);
            // This is the length of the opposite side in a right triangle with an adjacent side length of 100
            var angleDiff = Math.Sqrt(10000 / (cosAlpha * cosAlpha) - 10000);
            return Line2.Distance(Child, relevantLine.Child.PositionVector) + angleDiff;
        }
    }
}