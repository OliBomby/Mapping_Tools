using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a CircleArc as could be defined by 3 points.</summary>
    public struct CircleArc :IEquatable<CircleArc> {
        /// <summary>
        /// The centre of the CircleArc.
        /// </summary>
        public Vector2 Centre;

        /// <summary>
        /// The radius of the CircleArc.
        /// </summary>
        public double Radius;

        /// <summary>
        /// The angle on which the CircleArc starts.
        /// </summary>
        public double ThetaStart;

        /// <summary>
        /// The length of the CircleArc in radians.
        /// </summary>
        public double ThetaRange;

        /// <summary>
        /// The direction of the CircleArc.
        /// </summary>
        public double Dir;

        /// <summary>
        /// Whether the CircleArc got initialized correctly.
        /// </summary>
        public bool Stable;

        /// <summary>
        /// Constructs a new CircleArc.
        /// </summary>
        /// <param name="centre">The centre of the CircleArc.</param>
        /// <param name="radius">The radius of the CircleArc.</param>
        /// <param name="thetaStart">The angle on which the CircleArc starts.</param>
        /// <param name="thetaRange">The length of the CircleArc in radians.</param>
        /// <param name="dir">The direction of the CircleArc.</param>
        public CircleArc(Vector2 centre, double radius, double thetaStart, double thetaRange, double dir)
        {
            Centre = centre;
            Radius = radius;
            ThetaStart = thetaStart;
            ThetaRange = thetaRange;
            Dir = dir;
            Stable = true;
        }

        /// <summary>
        /// Constructs a new CircleArc using three points.
        /// </summary>
        /// <param name="points">List containing the points.</param>
        public CircleArc(List<Vector2> points)
        {
            Vector2 a = points[0];
            Vector2 b = points[1];
            Vector2 c = points[2];

            double aSq = (b - c).LengthSquared;
            double bSq = (a - c).LengthSquared;
            double cSq = (a - b).LengthSquared;

            // If we have a degenerate triangle where a side-length is almost zero, then give up and fall
            // back to a more numerically stable method.
            if (Precision.AlmostEquals(aSq, 0) || Precision.AlmostEquals(bSq, 0) || Precision.AlmostEquals(cSq, 0))
            {
                Stable = false;
                Centre = Vector2.Zero;
                Radius = 0;
                ThetaStart = 0;
                ThetaRange = 0;
                Dir = 0;
                return;
            }

            double s = aSq * (bSq + cSq - aSq);
            double t = bSq * (aSq + cSq - bSq);
            double u = cSq * (aSq + bSq - cSq);

            double sum = s + t + u;

            // If we have a degenerate triangle with an almost-zero size, then give up and fall
            // back to a more numerically stable method.
            if (Precision.AlmostEquals(sum, 0))
            {
                Stable = false;
                Centre = Vector2.Zero;
                Radius = 0;
                ThetaStart = 0;
                ThetaRange = 0;
                Dir = 0;
                return;
            }

            Vector2 centre = (s * a + t * b + u * c) / sum;
            Vector2 dA = a - centre;
            Vector2 dC = c - centre;

            double r = dA.Length;

            double thetaStart = Math.Atan2(dA.Y, dA.X);
            double thetaEnd = Math.Atan2(dC.Y, dC.X);

            while (thetaEnd < thetaStart)
                thetaEnd += 2 * Math.PI;

            double dir = 1;
            double thetaRange = thetaEnd - thetaStart;

            // Decide in which direction to draw the circle, depending on which side of
            // AC B lies.
            Vector2 orthoAtoC = c - a;
            orthoAtoC = new Vector2(orthoAtoC.Y, -orthoAtoC.X);
            if (Vector2.Dot(orthoAtoC, b - a) < 0)
            {
                dir = -dir;
                thetaRange = 2 * Math.PI - thetaRange;
            }

            ThetaStart = thetaStart;
            ThetaRange = thetaRange;
            Radius = r;
            Centre = centre;
            Dir = dir;
            Stable = true;
        }

        /// <summary>
        /// Defines a CircleArc that is a unit circle.
        /// </summary>
        public static readonly CircleArc UnitCircle = new CircleArc(Vector2.Zero, 1, 0, 2 * Math.PI, 1);

        /// <summary>
        /// Gets the rotator matrix for the CircleArc.
        /// </summary>
        public Matrix2 Rotator
        {
            get
            {
                return new Matrix2(new Vector2(Math.Cos(ThetaStart), (-Math.Sin(ThetaStart) * Dir)),
                                   new Vector2(Math.Sin(ThetaStart), (Math.Cos(ThetaStart) * Dir))) * Radius;
            }
        }

        /// <summary>
        /// Calculates points among the CircleArc.
        /// </summary>
        /// <param name="amountPoints">Number of points to calculate.</param>
        public List<Vector2> Interpolate(int amountPoints) {
            List<Vector2> output = new List<Vector2>(amountPoints);

            for (int i = 0; i < amountPoints; ++i) {
                double fract = (double)i / (amountPoints - 1);
                double theta = ThetaStart + Dir * fract * ThetaRange;
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
        public static bool operator ==(CircleArc left, CircleArc right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified instances for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are not equal; false otherwise.</returns>
        public static bool operator !=(CircleArc left, CircleArc right) {
            return !left.Equals(right);
        }

        private static readonly string listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        /// <summary>
        /// Returns a System.string that represents the current CircleArc.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format("({1}{0} {2}{0} {3}{0} {4}{0} {5}{0} {6})", listSeparator, Centre, Radius, ThetaStart, ThetaRange, Dir, Stable);
        }
        

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            if( !( obj is CircleArc ) ) {
                return false;
            }

            return Equals((CircleArc) obj);
        }

        /// <summary>Indicates whether the current CircleArc is equal to another CircleArc.</summary>
        /// <param name="other">A CircleArc to compare with this CircleArc.</param>
        /// <returns>true if the current CircleArc is equal to the CircleArc parameter; otherwise, false.</returns>
        public bool Equals(CircleArc other) {
            return
                Centre == other.Centre &&
                Radius == other.Radius &&
                ThetaStart == other.ThetaStart &&
                ThetaRange == other.ThetaRange &&
                Dir == other.Dir &&
                Stable == other.Stable;
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
            hashCode = hashCode * -1521134295 + ThetaStart.GetHashCode();
            hashCode = hashCode * -1521134295 + ThetaRange.GetHashCode();
            hashCode = hashCode * -1521134295 + Dir.GetHashCode();
            hashCode = hashCode * -1521134295 + Stable.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Matrix2>.Default.GetHashCode(Rotator);
            return hashCode;
        }
    }
}
