using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject {
    public interface IRelevantDrawable : IRelevantObject {
        string PreferencesName { get; }
        double DistanceTo(Vector2 point);
        Vector2 NearestPoint(Vector2 point);
        bool Intersection(IRelevantObject other, out Vector2[] intersections);
        void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences);
        void DrawYourself(DrawingContext context, CoordinateConverter converter, RelevantObjectPreferences preferences, Pen pen);
    }
}
