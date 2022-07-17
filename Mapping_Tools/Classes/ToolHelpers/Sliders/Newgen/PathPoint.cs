using System;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen {
    public struct PathPoint : IComparable<PathPoint> {
        public Vector2 Pos;
        public Vector2 OgPos;
        public double PreAngle;
        public double PostAngle;
        public double CumulativeLength;
        /// <summary>
        /// Used to define distance between points which are on the same position. [0,1]
        /// </summary>
        public double T;
        /// <summary>
        /// If true, indicates that this point is not continuous in local curvature.
        /// </summary>
        public bool Red;

        public PathPoint(Vector2 pos, double preAngle = 0, double postAngle = 0, double cumulativeLength = 0,
            double t = double.NaN, bool red = false) : this(pos, pos, preAngle, postAngle, cumulativeLength, t, red) { }

        public PathPoint(Vector2 pos, Vector2 ogPos, double preAngle = 0, double postAngle = 0, double cumulativeLength = 0, double t = double.NaN, bool red = false) {
            Pos = pos;
            OgPos = ogPos;
            PreAngle = preAngle;
            PostAngle = postAngle;
            CumulativeLength = cumulativeLength;
            T = t;
            Red = red;
        }

        public double AvgAngle => double.IsNaN(PreAngle) ? PostAngle :
            double.IsNaN(PostAngle) ? PreAngle : MathHelper.LerpAngle(PreAngle, PostAngle, 0.5);

        public PathPoint SetT(double t) {
            return new PathPoint(Pos, OgPos, PreAngle, PostAngle, CumulativeLength, t, Red);
        }

        public PathPoint SetRed(bool red) {
            return new PathPoint(Pos, OgPos, PreAngle, PostAngle, CumulativeLength, T, red);
        }

        /// <summary>
        /// Adds the specified instances.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Result of addition.</returns>
        public static PathPoint operator +(PathPoint left, PathPoint right) {
            left.Pos += right.Pos;
            left.OgPos += right.OgPos;
            left.PreAngle += right.PreAngle;
            left.PostAngle += right.PostAngle;
            left.CumulativeLength += right.CumulativeLength;
            left.Red |= right.Red;
            return left;
        }

        /// <summary>
        /// Subtracts the specified instances.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Result of subtraction.</returns>
        public static PathPoint operator -(PathPoint left, PathPoint right) {
            left.Pos -= right.Pos;
            left.OgPos -= right.OgPos;
            left.PreAngle -= right.PreAngle;
            left.PostAngle -= right.PostAngle;
            left.CumulativeLength -= right.CumulativeLength;
            left.Red &= right.Red;
            return left;
        }

        /// <summary>
        /// Negates the specified instance.
        /// </summary>
        /// <param name="vec">Operand.</param>
        /// <returns>Result of negation.</returns>
        public static PathPoint operator -(PathPoint vec) {
            vec.Pos = -vec.Pos;
            vec.OgPos = -vec.OgPos;
            vec.PreAngle += Math.PI;
            vec.PostAngle += Math.PI;
            return vec;
        }

        /// <summary>
        /// Multiplies the specified instance by a scalar.
        /// </summary>
        /// <param name="vec">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of multiplication.</returns>
        public static PathPoint operator *(PathPoint vec, double scale) {
            vec.Pos *= scale;
            vec.OgPos *= scale;
            vec.CumulativeLength *= scale;
            return vec;
        }

        /// <summary>
        /// Multiplies the specified instance by a scalar.
        /// </summary>
        /// <param name="scale">Left operand.</param>
        /// <param name="vec">Right operand.</param>
        /// <returns>Result of multiplication.</returns>
        public static PathPoint operator *(double scale, PathPoint vec) {
            vec.Pos *= scale;
            vec.OgPos *= scale;
            vec.CumulativeLength *= scale;
            return vec;
        }

        /// <summary>
        /// Divides the specified instance by a scalar.
        /// </summary>
        /// <param name="vec">Left operand</param>
        /// <param name="scale">Right operand</param>
        /// <returns>Result of the division.</returns>
        public static PathPoint operator /(PathPoint vec, double scale) {
            vec.Pos /= scale;
            vec.OgPos /= scale;
            vec.CumulativeLength /= scale;
            return vec;
        }

        /// <summary>
        /// Returns a new PathPoint that is the linear blend of the 2 given PathPoint
        /// </summary>
        /// <param name="a">First input PathPoint</param>
        /// <param name="b">Second input PathPoint</param>
        /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
        /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
        public static PathPoint Lerp(PathPoint a, PathPoint b, double blend) {
            a.Pos = blend * (b.Pos - a.Pos) + a.Pos;
            a.OgPos = blend * (b.OgPos - a.OgPos) + a.OgPos;
            var angle1 = a.Red && !double.IsNaN(a.PostAngle) ? a.PostAngle : a.AvgAngle;
            var angle2 = b.Red && !double.IsNaN(b.PreAngle) ? b.PreAngle : b.AvgAngle;
            a.PreAngle = MathHelper.LerpAngle(angle1, angle2, blend);
            a.PostAngle = a.PreAngle;
            a.CumulativeLength = blend * (b.CumulativeLength - a.CumulativeLength) + a.CumulativeLength;
            a.T = blend * (b.T - a.T) + a.T;
            a.Red = false;
            return a;
        }

        public override string ToString() {
            return $"{Pos} {OgPos} ({PreAngle}, {PostAngle}) {CumulativeLength} {T} {Red}";
        }

        public int CompareTo(PathPoint other) {
            var cumulativeLengthComparison = CumulativeLength.CompareTo(other.CumulativeLength);
            return cumulativeLengthComparison != 0 ? cumulativeLengthComparison : T.CompareTo(other.T);
        }

        public static bool operator <(PathPoint left, PathPoint right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(PathPoint left, PathPoint right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(PathPoint left, PathPoint right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(PathPoint left, PathPoint right) {
            return left.CompareTo(right) >= 0;
        }
    }
}