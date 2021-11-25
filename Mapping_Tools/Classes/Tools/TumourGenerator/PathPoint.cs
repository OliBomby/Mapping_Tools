using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerator {
    public struct PathPoint {
        public Vector2 Pos;
        public Vector2 Dir;
        public double Dist;
        public double CumulativeLength;
    }
}