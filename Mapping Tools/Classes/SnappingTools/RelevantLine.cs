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
            throw new NotImplementedException();
        }
    }
}
