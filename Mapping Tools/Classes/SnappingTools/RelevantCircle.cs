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
            return Math.Abs(Vector2.Distance(point, child.Centre) - child.Radius);
        }

        public bool Intersection(IRelevantObject other, out Vector2[] intersections) {
            throw new NotImplementedException();
        }

        public Vector2 NearestPoint(Vector2 point) {
            var diff = point - child.Centre;
            return child.Centre + diff / diff.Length * child.Radius;
        }
    }
}
