using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.Security.Policy;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a line with infinite length using three double-precision floating-point numbers in the equation AX + BY = C.</summary>
    /// <remarks>
    /// The Line structure is suitable for interoperation with unmanaged code requiring three consecutive doubles.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Line2 :IEquatable<Line2> {
        /// <summary>
        /// The base vector where the Line originates from.
        /// </summary>
        public Vector2 BaseVector;

        /// <summary>
        /// The direction vector of the Line.
        /// </summary>
        public Vector2 DirectionVector;

        /// <summary>
        /// Constructs a new Line using a base vector and a direciton vector.
        /// </summary>
        /// <param name="baseVector">The base vector of the Line.</param>
        /// <param name="directionVector">The direction vector of the Line.</param>
        public Line2(Vector2 baseVector, Vector2 directionVector)
        {
            BaseVector = baseVector;
            DirectionVector = directionVector;
        }

        public Line2(Vector2 baseVector, double angle)
        {
            BaseVector = baseVector;
            DirectionVector = new Vector2(Math.Cos(angle), Math.Sin(angle));
        }

        /// <summary>
        /// Constructs a new Line using two points.
        /// </summary>
        /// <param name="p1">The first point on the Line.</param>
        /// <param name="p2">The second point on the Line.</param>
        public static Line2 FromPoints(Vector2 p1, Vector2 p2) {
            return new Line2(p1, p2 - p1);
        }


        /// <summary>
        /// Defines a Line that is the X-axis.
        /// </summary>
        public static readonly Line2 AxisX = new Line2(Vector2.Zero, Vector2.UnitX);

        /// <summary>
        /// Defines a Line that is the Y-axis.
        /// </summary>
        public static readonly Line2 AxisY = new Line2(Vector2.Zero, Vector2.UnitY);

        /// <summary>
        /// Calculate the distance between a line and a point
        /// </summary>
        /// <param name="left">The line</param>
        /// <param name="right">The point</param>
        /// <returns>The distance between the line and the point</returns>
        public static double Distance(Line2 left, Vector2 right) {
            return Math.Abs(left.A * right.X + left.B * right.Y - left.C) / Math.Sqrt(left.A * left.A + left.B * left.B);
        }

        /// <summary>
        /// Calculate the intersection of two lines
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The intersection the two inputs</returns>
        public static Vector2 Intersection(Line2 left, Line2 right)
        {
            Intersection(left, right, out Vector2 result);
            return result;
        }

        /// <summary>
        /// Calculate the intersection of two lines
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The intersection the two inputs</returns>
        public static bool Intersection(Line2 left, Line2 right, out Vector2 result)
        {
            if (right.A == 0) { var temp = left; left = right; right = temp; } // swap inputs to prevent division by zero

            if (right.A * left.B == left.A * right.B) {
                result = Vector2.NaN;
                return false;
            }
            else{
                double y = (left.C - left.A * right.C / right.A) / (left.B - left.A * right.B / right.A);
                double x = right.C / right.A - right.B * y / right.A;
                result = new Vector2(x, y);
                return true;
            }
        }

        ///<summary>
        ///Calculates the intersection(s) between a Rectangle and a Line.
        ///</summary>
        ///<param name="rect">The rectangle</param>
        ///<param name="line">The line</param>
        /// <param name="intersections">The calculated intersection(s).</param>
        ///<returns>Whether there are exactly two intersections.</returns>
        public static bool Intersection(Box2 rect, Line2 line, out Vector2[] intersections)
        {
            List<Vector2> candidates = new List<Vector2>
            {
                new Vector2 {X = rect.Left, Y = (line.C - line.A * rect.Left) / line.B },
                new Vector2 {X = (line.C - line.B * rect.Top) / line.A, Y = rect.Top },
                new Vector2 {X = (line.C - line.B * rect.Bottom) / line.A, Y = rect.Bottom },
                new Vector2 {X = rect.Right, Y = (line.C - line.A * rect.Right) / line.B },
            };

            intersections = candidates.Where(p => (p[0] >= rect.Left) && (p[0] <= rect.Right) && (p[1] >= rect.Top) && (p[1] <= rect.Bottom)).ToArray();
            return intersections.Length == 2;
        }

        /// <summary>
        /// Compares the specified instances for equality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are equal; false otherwise.</returns>
        public static bool operator ==(Line2 left, Line2 right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified instances for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are not equal; false otherwise.</returns>
        public static bool operator !=(Line2 left, Line2 right) {
            return !left.Equals(right);
        }

        private static readonly string listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        /// <summary>
        /// Returns a System.string that represents the current Line.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format("({0}{2} {1})", BaseVector, DirectionVector, listSeparator);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
            unchecked {
                return ((BaseVector.GetHashCode() * 397 ) ^ DirectionVector.GetHashCode()) * 397;
            }
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            return obj is Line2 line2 && Equals(line2);
        }

        /// <summary>Indicates whether the current line is equal to another line.</summary>
        /// <param name="other">A line to compare with this line.</param>
        /// <returns>true if the current line is equal to the line parameter; otherwise, false.</returns>
        public bool Equals(Line2 other) {
            return BaseVector == other.BaseVector &&
            DirectionVector == other.DirectionVector;
        }
    }
}
