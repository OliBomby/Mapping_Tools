using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a Circle as could be defined by a centre point and a radius.</summary>
    public struct Circle : IEquatable<Circle> {
        /// <summary>
        /// The centre of the Circle.
        /// </summary>
        public Vector2 Centre;

        /// <summary>
        /// The radius of the Circle.
        /// </summary>
        public double Radius;

        /// <summary>
        /// Constructs a new Circle.
        /// </summary>
        /// <param name="centre">The centre of the Circle.</param>
        /// <param name="radius">The radius of the Circle.</param>
        public Circle(Vector2 centre, double radius) {
            Centre = centre;
            Radius = radius;
        }

        /// <summary>
        /// Constructs a new Circle using two points.
        /// </summary>
        public Circle(Vector2 centre, Vector2 outerPoint) {
            Centre = centre;
            Radius = Vector2.Distance(centre, outerPoint);
        }

        /// <summary>
        /// Constructs a new Circle from a CircleArc.
        /// </summary>
        /// <param name="arc">The CircleArc to get centre point and radius from.</param>
        public Circle(CircleArc arc) {
            Centre = arc.Centre;
            Radius = arc.Radius;
        }

        /// <summary>
        /// Defines a Circle that is a unit circle.
        /// </summary>
        public static readonly Circle UnitCircle = new Circle(Vector2.Zero, 1);

        /// <summary>
        /// Calculates the intersection(s) between a Circle and a Line.
        /// </summary>
        /// <param name="left">The circle.</param>
        /// <param name="right">The line.</param>
        /// <param name="intersections">The calculated intersection(s).</param>
        /// <returns>Whether there is at least one intersection.</returns>
        public static bool Intersection(Circle left, Line2 right, out Vector2[] intersections) {
            double p1 = right.PositionVector.X;
            double p2 = right.PositionVector.Y;
            double d1 = right.DirectionVector.X;
            double d2 = right.DirectionVector.Y;
            double c1 = left.Centre.X;
            double c2 = left.Centre.Y;
            double r = left.Radius;

            double ds = d1 * d1 + d2 * d2;
            double c = d2 * p1 - d1 * p2 - d2 * c1 + d1 * c2;
            double disc = r * r * ds - c * c;

            if (disc <= -Precision.DOUBLE_EPSILON) {
                intersections = new Vector2[0];
                return false;
            }

            if (Math.Abs(disc) < Precision.DOUBLE_EPSILON) {
                intersections = new Vector2[1] { new Vector2(c * d2 / ds + c1, -c * d1 / ds + c2) };
                return true;
            }

            var root = Math.Sqrt(disc);

            intersections = new Vector2[2] {
                new Vector2((c * d2 - d1 * root) / ds + c1, (-c * d1 - d2 * root) / ds + c2),
                new Vector2((c * d2 + d1 * root) / ds + c1, (-c * d1 + d2 * root) / ds + c2),
            };
            return true;
        }

        /// <summary>
        /// Calculate the intersection(s) of two circles.
        /// </summary>
        /// <param name="left">Circle 1.</param>
        /// <param name="right">Circle 2.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>Whether there is at least one intersection.</returns>
        public static bool Intersection(Circle left, Circle right, out Vector2[] intersections) {
            var d = Vector2.Distance(left.Centre, right.Centre);
            if (d > left.Radius + right.Radius || d <= Math.Abs(left.Radius - right.Radius)) {
                // None or infinite solutions
            }

            var d2 = d * d;
            var a = (left.Radius * left.Radius - right.Radius * right.Radius + d2) / (2 * d);
            var p2 = left.Centre + a * (right.Centre - left.Centre) / d;
            var h = Math.Sqrt(left.Radius * left.Radius - a * a);

            if (Math.Abs(d - (left.Radius + right.Radius)) < Precision.DOUBLE_EPSILON) {
                // One solution
                intersections = new[] {
                    p2
                };
                return true;
            }
            var b = h * (right.Centre - left.Centre) / d;
            intersections = new[] {
                p2 + b.PerpendicularLeft,
                p2 + b.PerpendicularRight
            };
            return true;
        }

        /// <summary>
        /// Calculates points among the Circle.
        /// </summary>
        /// <param name="amountPoints">Number of points to calculate.</param>
        public List<Vector2> Interpolate(int amountPoints) {
            List<Vector2> output = new List<Vector2>(amountPoints);

            for (int i = 0; i < amountPoints; ++i) {
                double fract = (double)i / (amountPoints - 1);
                double theta = 2 * Math.PI * fract;
                Vector2 o = new Vector2(Math.Cos(theta), Math.Sin(theta)) * Radius;
                output.Add(Centre + o);
            }

            return output;
        }

        /// <summary>
        /// Compares the specified instances for equality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are equal; false otherwise.</returns>
        public static bool operator ==(Circle left, Circle right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified instances for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are not equal; false otherwise.</returns>
        public static bool operator !=(Circle left, Circle right) {
            return !left.Equals(right);
        }

        private static readonly string listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        /// <summary>
        /// Returns a System.string that represents the current Circle.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format("({1}{0} {2})", listSeparator, Centre, Radius);
        }


        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            if (!(obj is Circle)) {
                return false;
            }

            return Equals((Circle)obj);
        }

        /// <summary>Indicates whether the current Circle is equal to another Circle.</summary>
        /// <param name="other">A Circle to compare with this Circle.</param>
        /// <returns>true if the current Circle is equal to the Circle parameter; otherwise, false.</returns>
        public bool Equals(Circle other) {
            return
                Centre == other.Centre &&
                Radius == other.Radius;
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
            var hashCode = 2048149326;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(Centre);
            hashCode = hashCode * -1521134295 + Radius.GetHashCode();
            return hashCode;
        }
    }
}
