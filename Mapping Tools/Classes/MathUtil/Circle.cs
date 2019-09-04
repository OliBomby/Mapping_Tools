using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a Circle as could be defined by a centre point and a radius.</summary>
    public struct Circle :IEquatable<Circle> {
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
        public Circle(Vector2 centre, double radius)
        {
            Centre = centre;
            Radius = radius;
        }

        /// <summary>
        /// Constructs a new Circle using two points.
        /// </summary>
        /// <param name="points">List containing the points.</param>
        public Circle(Vector2 centre, Vector2 outerPoint)
        {
            Centre = centre;
            Radius = Vector2.Distance(centre, outerPoint);
        }

        /// <summary>
        /// Defines a Circle that is a unit circle.
        /// </summary>
        public static readonly Circle UnitCircle = new Circle(Vector2.Zero, 1);

        /*public static bool Intersection(Circle left, Line right, out Vector2[] intersections) {
            Vector2 offset = left.Centre;
            double r = left.Radius;
            double a = right.A;
            double b = right.B;
            double c = -right.C;
            double x0 = -a * c / (a * a + b * b);
            double y0 = -b * c / (a * a + b * b);
            if (c * c > r * r * (a * a + b * b) + EPS) {
                intersections = new Vector2[] { };
                return false;
            }
            else if (abs(c * c - r * r * (a * a + b * b)) < EPS) {
                puts("1 point");
                cout << x0 << ' ' << y0 << '\n';
            }
            else {
                double d = r * r - c * c / (a * a + b * b);
                double mult = Math.Sqrt(d / (a * a + b * b));
                double ax, ay, bx, by;
                ax = x0 + b * mult;
                bx = x0 - b * mult;
                ay = y0 - a * mult;
                by = y0 + a * mult;
                puts("2 points");
                cout << ax << ' ' << ay << '\n' << bx << ' ' << by << '\n';
            }
        }*/

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
            if( !( obj is Circle ) ) {
                return false;
            }

            return Equals((Circle) obj);
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
        public override int GetHashCode()
        {
            var hashCode = 2048149326;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(Centre);
            hashCode = hashCode * -1521134295 + Radius.GetHashCode();
            return hashCode;
        }
    }
}
