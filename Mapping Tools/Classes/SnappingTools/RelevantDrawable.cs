using System.Collections.Generic;
using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators;

namespace Mapping_Tools.Classes.SnappingTools {
    public abstract class RelevantDrawable : RelevantObject, IRelevantDrawable {

        public abstract double DistanceTo(Vector2 point);

        public abstract Vector2 NearestPoint(Vector2 point);

        public abstract bool Intersection(IRelevantObject other, out Vector2[] intersections);

        public abstract void DrawYourself(DrawingContext context, CoordinateConverter converter,
            SnappingToolsPreferences preferences);
    }
}
