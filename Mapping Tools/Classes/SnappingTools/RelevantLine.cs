using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantLine : IRelevantObject {
        public readonly Line child;

        public double DistanceTo(Vector2 point) {
            return Line.Distance(child, point);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            if (other is RelevantPoint point) {
                intersections = new[] { point.child };
                return Precision.AlmostEquals(Line.Distance(child, point.child), 0);
            }
            else if (other is RelevantLine line) {
                bool IsIntersecting = Line.Intersection(child, line.child, out var intersection);
                intersections = new[] { intersection };
                return IsIntersecting;
            }
            else if (other is RelevantCircle circle) {
                intersections = new Vector2[] { };
                return false;
            }
            else {
                intersections = new Vector2[] { };
                return false;
            }
        }

        public Vector2 NearestPoint(Vector2 point) {
            double a = child.A, b = child.B, c = -child.C;
            double abs = a * a + b * b;
            double x = (b * (b * point.X - a * point.Y) - a * c) / abs;
            double y = (a * (-b * point.X + a * point.Y) - b * c) / abs;
            return new Vector2(x, y);
        }

        public RelevantLine(Line line) {
            child = line;
        }
    }
}
