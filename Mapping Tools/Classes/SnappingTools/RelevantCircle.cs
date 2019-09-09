using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantCircle : IRelevantObject {
        public readonly Circle child;

        public double DistanceTo(Vector2 point) {
            var dist = Vector2.Distance(point, child.Centre);
            return Math.Abs(dist - child.Radius);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            if (other is RelevantPoint point) {
                intersections = new[] { point.child };
                return Precision.AlmostEquals(Vector2.Distance(child.Centre, point.child), child.Radius);
            } else if (other is RelevantLine line) {
                return Circle.Intersection(child, line.child, out intersections);
            } else if (other is RelevantCircle circle) {
                intersections = new Vector2[0];
                return false;
            } else {
                intersections = new Vector2[0];
                return false;
            }
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
