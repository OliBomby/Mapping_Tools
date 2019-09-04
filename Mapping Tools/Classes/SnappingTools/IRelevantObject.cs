using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools {
    public interface IRelevantObject {
        double DistanceTo(Vector2 point);
        bool Intersection(IRelevantObject other, out Vector2[] intersections);
    }
}
