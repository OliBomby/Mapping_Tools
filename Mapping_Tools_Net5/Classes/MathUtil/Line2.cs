using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a 2D line with infinite length using a position vector and a direction vector.</summary>
    /// <remarks>
    /// The Line structure is suitable for interoperation with unmanaged code requiring three consecutive doubles.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Line2 :IEquatable<Line2> {
        /// <summary>
        /// The point where the Line originates from.
        /// </summary>
        public Vector2 PositionVector;

        /// <summary>
        /// The direction vector of the Line.
        /// </summary>
        public Vector2 DirectionVector;

        /// <summary>
        /// Constructs a new Line using a base vector and a direciton vector.
        /// </summary>
        /// <param name="positionVector">The base vector of the Line.</param>
        /// <param name="directionVector">The direction vector of the Line.</param>
        public Line2(Vector2 positionVector, Vector2 directionVector)
        {
            PositionVector = positionVector;
            DirectionVector = directionVector;
        }

        public Line2(Vector2 positionVector, double angle)
        {
            PositionVector = positionVector;
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
        /// Calculates point on Line at a given t
        /// </summary>
        /// <param name="t">Progression along the Line</param>
        /// <returns></returns>
        public Vector2 PointOnLine(double t) {
            return PositionVector + t * DirectionVector;
        }

        /// <summary>
        /// Gets the Line perpendicular on the left side of this line
        /// </summary>
        /// <returns></returns>
        public Line2 PerpendicularLeft() {
            return new Line2(PositionVector, DirectionVector.PerpendicularLeft);
        }

        /// <summary>
        /// Gets the Line perpendicular on the right side of this line
        /// </summary>
        /// <returns></returns>
        public Line2 PerpendicularRight() {
            return new Line2(PositionVector, DirectionVector.PerpendicularRight);
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
            return Math.Abs(left.DirectionVector.Y * right.X - left.DirectionVector.X * right.Y +
                            left.PositionVector.Y * left.DirectionVector.X -
                            left.PositionVector.X * left.DirectionVector.Y) /
                   left.DirectionVector.Length;
        }

        public static Vector2 NearestPoint(Line2 left, Vector2 right) {
            var perp = left.PerpendicularLeft();
            perp.PositionVector = right;
            Intersection(perp, left, out var intersection);
            return intersection;
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
        public static bool Intersection(Line2 left, Line2 right, out Vector2 result) {
            var p1 = left.PositionVector;
            var p2 = left.PositionVector + left.DirectionVector;
            var p3 = right.PositionVector;
            var p4 = right.PositionVector + right.DirectionVector;
            var denom = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
            if (Math.Abs(denom) < Precision.DOUBLE_EPSILON) {
                result = Vector2.NaN;
                return false;
            }

            var t = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / denom;
            result = left.PointOnLine(t);
            return true;
        }

        ///<summary>
        ///Calculates the intersection(s) between a Rectangle and a Line.
        ///</summary>
        ///<param name="rect">The rectangle</param>
        ///<param name="line">The line</param>
        /// <param name="intersections">The calculated intersection(s).</param>
        ///<returns>Whether there are exactly two intersections.</returns>
        public static bool Intersection(Box2 rect, Line2 line, out Vector2[] intersections) {
            var candidates = new List<Vector2>(4);
            if (Math.Abs(line.DirectionVector.X) > Precision.DOUBLE_EPSILON) {
                candidates.Add(line.PointOnLine((rect.Left - line.PositionVector.X) / line.DirectionVector.X));
                candidates.Add(line.PointOnLine((rect.Right - line.PositionVector.X) / line.DirectionVector.X));
            }
            if (Math.Abs(line.DirectionVector.Y) > Precision.DOUBLE_EPSILON) {
                candidates.Add(line.PointOnLine((rect.Top - line.PositionVector.Y) / line.DirectionVector.Y));
                candidates.Add(line.PointOnLine((rect.Bottom - line.PositionVector.Y) / line.DirectionVector.Y));
            }

            intersections = candidates.Where(p => (p[0] > rect.Left - Precision.DOUBLE_EPSILON) &&
                                                  (p[0] < rect.Right + Precision.DOUBLE_EPSILON) &&
                                                  (p[1] > rect.Top - Precision.DOUBLE_EPSILON) &&
                                                  (p[1] < rect.Bottom + Precision.DOUBLE_EPSILON)).ToArray();
            return intersections.Length >= 2;
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
            return string.Format("({0}{2} {1})", PositionVector, DirectionVector, listSeparator);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
            unchecked {
                return ((PositionVector.GetHashCode() * 397 ) ^ DirectionVector.GetHashCode()) * 397;
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
            return PositionVector == other.PositionVector &&
            DirectionVector == other.DirectionVector;
        }
    }
}
