using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a line with infinite length using three double-precision floating-point numbers in the equation AX + BY = C.</summary>
    /// <remarks>
    /// The Line structure is suitable for interoperation with unmanaged code requiring three consecutive doubles.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Line :IEquatable<Line> {
        /// <summary>
        /// The A component of the Line.
        /// </summary>
        public double A;

        /// <summary>
        /// The B component of the Line.
        /// </summary>
        public double B;

        /// <summary>
        /// The C component of the Line.
        /// </summary>
        public double C;

        /// <summary>
        /// Constructs a new Line.
        /// </summary>
        /// <param name="a">The a component of the Line.</param>
        /// <param name="b">The b component of the Line.</param>
        /// <param name="c">The c component of the Line.</param>
        public Line(double a, double b, double c)
        {
            A = a;
            B = b;
            C = c;
        }

        /// <summary>
        /// Constructs a new Line as Y = AX + B.
        /// </summary>
        /// <param name="a">The a component of the Line.</param>
        /// <param name="b">The b component of the Line.</param>
        public Line(double a, double b)
        {
            A = -a;
            B = 1;
            C = b;
        }

        /// <summary>
        /// Constructs a new Line using two points.
        /// </summary>
        /// <param name="vec1">The first point on the Line.</param>
        /// <param name="vec2">The second point on the Line.</param>
        public Line(Vector2 vec1, Vector2 vec2)
        {
            if (Precision.AlmostEquals(vec1.X, vec2.X)) {
                A = 1;
                B = 0;
                C = vec1.X;
            }
            else {
                A = -1 * (vec2.Y - vec1.Y) / (vec2.X - vec2.X);
                B = 1;
                C = vec1.Y + A * vec1.X;
            }
        }

        /// <summary>
        /// Constructs a new Line using a point and an angle.
        /// </summary>
        /// <param name="vec1">The point on the Line.</param>
        /// <param name="angle">The angle of the Line.</param>
        public Line(Vector2 vec1, double angle)
        {
            if(Precision.AlmostEquals(Math.Abs(angle), 0.5 * Math.PI)) {
                A = 1;
                B = 0;
                C = vec1.X;
            }
            else {
                A = -1 * Math.Tan(angle);
                B = 1;
                C = vec1.Y + A * vec1.X;
            }
        }

        /// <summary>
        /// Defines a Line that is the X-axis.
        /// </summary>
        public static readonly Line AxisX = new Line(0, 1, 0);

        /// <summary>
        /// Defines a Line that is the Y-axis.
        /// </summary>
        public static readonly Line AxisY = new Line(0, 1);

        /// <summary>
        /// Calculate the intersection of two lines
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The intersection the two inputs</returns>
        public static Vector2 Intersection(Line left, Line right)
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
        public static void Intersection(ref Line left, ref Line right, out Vector2 result)
        {
            double d1 = 1 / right.A;
            if (left.B == left.A * right.B * d1)
                result = Vector2.NaN;
            else{
                double y = (left.C - left.A * right.C * d1) / (left.B - left.A * right.B * d1);
                double x = right.C * d1 - right.B * y * d1;
                result = new Vector2(x, y);
            }
        }

        /// <summary>
        /// Compares the specified instances for equality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are equal; false otherwise.</returns>
        public static bool operator ==(Line left, Line right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified instances for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are not equal; false otherwise.</returns>
        public static bool operator !=(Line left, Line right) {
            return !left.Equals(right);
        }

        private static string listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        /// <summary>
        /// Returns a System.String that represents the current Line.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return String.Format("({0}{3} {1}{3} {2})", A, B, C, listSeparator);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
            unchecked {
                return (((A.GetHashCode() * 397 ) ^ B.GetHashCode()) * 397) ^ C.GetHashCode();
            }
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            if( !( obj is Line ) ) {
                return false;
            }

            return Equals((Line) obj);
        }

        /// <summary>Indicates whether the current line is equal to another line.</summary>
        /// <param name="other">A line to compare with this line.</param>
        /// <returns>true if the current line is equal to the line parameter; otherwise, false.</returns>
        public bool Equals(Line other) {
            return
                A == other.A &&
                B == other.B &&
                C == other.C;
        }
    }
}
