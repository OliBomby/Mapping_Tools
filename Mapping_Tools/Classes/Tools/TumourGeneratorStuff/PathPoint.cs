using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public struct PathPoint {
        public Vector2 Pos;
        public Vector2 Dir;
        public double Dist;
        public double CumulativeLength;
        /// <summary>
        /// Used to define distance between points which are on the same position. [0,1]
        /// </summary>
        public double T;

        public PathPoint(Vector2 pos, Vector2 dir, double dist, double cumulativeLength) : this (pos, dir, dist, cumulativeLength, 0) { }

        public PathPoint(Vector2 pos, Vector2 dir, double dist, double cumulativeLength, double t) {
            Pos = pos;
            Dir = dir;
            Dist = dist;
            CumulativeLength = cumulativeLength;
            T = t;
        }
    }
}