using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a line with infinite length using three double-precision floating-point numbers in the equation AX + BY = C.</summary>
    /// <remarks>
    /// The LineSegment structure is suitable for interoperation with unmanaged code requiring three consecutive doubles.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct LineSegment :IEquatable<LineSegment> {
        /// <summary>
        /// The first point of the LineSegment.
        /// </summary>
        public Vector2 P1;

        /// <summary>
        /// The second point of the LineSegment.
        /// </summary>
        public Vector2 P2;

        /// <summary>
        /// Constructs a new LineSegment.
        /// </summary>
        /// <param name="p1">The first point of the LineSegment.</param>
        /// <param name="p2">The second point of the LineSegment.</param>
        public LineSegment(Vector2 p1, Vector2 p2)
        {
            P1 = p1;
            P2 = p2;
        }

        /// <summary>
        /// Calculate the intersection of two lines
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The intersection the two inputs</returns>
        public static Vector2 Intersection(LineSegment left, LineSegment right)
        {
            Intersection(ref left, ref right, out Vector2 result);
            return result;
        }

        /// <summary>
        /// Calculate the intersection of two lines
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The intersection the two inputs</returns>
        public static bool Intersection(ref LineSegment left, ref LineSegment right, out Vector2 result)
        {
            Vector2 s1 = left.P2 - left.P1;
            Vector2 s2 = right.P2 - right.P1;

            double denom = -s2.X * s1.Y + s1.X * s2.Y;
            if (denom == 0) {
                result = Vector2.NaN;
                return false;
            }

            double s = (-s1.Y * (left.P1.X - right.P1.X) + s1.X * (left.P1.Y - right.P1.Y)) / denom;
            double t = ( s2.X * (left.P1.Y - right.P1.Y) - s2.Y * (left.P1.X - right.P1.X)) / denom;

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {
                // Collision detected
                result = new Vector2(left.P1.X + (t * s1.X), left.P1.Y + (t * s1.Y));
                return true;
            }

            result = Vector2.NaN;
            return false; // No collision
        }

        /// <summary>
        /// Calculate the distance between a LineSegment and a point
        /// </summary>
        /// <param name="l">The line segment</param>
        /// <param name="p">The point</param>
        /// <returns>The intersection the two inputs</returns>
        public static double Distance(LineSegment l, Vector2 p) {
            Distance(ref l, ref p, out double result);
            return result;
        }

        /// <summary>
        /// Calculate the distance between a LineSegment and a point
        /// </summary>
        /// <param name="l">The line segment</param>
        /// <param name="p">The point</param>
        /// <returns>The intersection the two inputs</returns>
        public static void Distance(ref LineSegment l, ref Vector2 p, out double result) {
            // Return minimum distance between line segment vw and point p
            double l2 = Vector2.DistanceSquared(l.P1, l.P2);  // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0) { result = Vector2.Distance(l.P1, p); return; }  // v == w case
                                                    // Consider the line extending the segment, parameterized as v + t (w - v).
                                                    // We find projection of point p onto the line. 
                                                    // It falls where t = [(p-v) . (w-v)] / |w-v|^2
                                                    // We clamp t from [0,1] to handle points outside the segment vw.
            double t = Math.Max(0, Math.Min(1, Vector2.Dot(p - l.P1, l.P2 - l.P1) / l2));
            Vector2 projection = l.P1 + t * (l.P2 - l.P1);  // Projection falls on the segment
            result = Vector2.Distance(p, projection);
        }

        /// <summary>
        /// Compares the specified instances for equality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are equal; false otherwise.</returns>
        public static bool operator ==(LineSegment left, LineSegment right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified instances for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are not equal; false otherwise.</returns>
        public static bool operator !=(LineSegment left, LineSegment right) {
            return !left.Equals(right);
        }

        private static string listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        /// <summary>
        /// Returns a System.String that represents the current LineSegment.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return String.Format("({0}{2} {1}{2})", P1, P2, listSeparator);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
            unchecked {
                return ((P1.GetHashCode()) * 397) ^ P2.GetHashCode();
            }
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            if( !( obj is LineSegment ) ) {
                return false;
            }

            return Equals((LineSegment) obj);
        }

        /// <summary>Indicates whether the current line is equal to another line.</summary>
        /// <param name="other">A line to compare with this line.</param>
        /// <returns>true if the current line is equal to the line parameter; otherwise, false.</returns>
        public bool Equals(LineSegment other) {
            return
                P1 == other.P1 &&
                P2 == other.P2;
        }
    }
}
