using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public struct PathPoint {
        public Vector2 Pos;
        public Vector2 Dir;
        public double Dist;
        public double CumulativeLength;

        public PathPoint(Vector2 pos, Vector2 dir, double dist, double cumulativeLength) {
            Pos = pos;
            Dir = dir;
            Dist = dist;
            CumulativeLength = cumulativeLength;
        }
    }
}