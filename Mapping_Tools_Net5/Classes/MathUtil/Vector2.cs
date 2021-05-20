/*
Copyright (c) 2006 - 2008 The Open Toolkit library.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>Represents a 2D vector using two double-precision floating-point numbers.</summary>
    /// <remarks>
    /// The Vector2 structure is suitable for interoperation with unmanaged code requiring two consecutive doubles.
    /// </remarks>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2 :IEquatable<Vector2> {
        /// <summary>
        /// The X component of the Vector2.
        /// </summary>
        public double X;

        /// <summary>
        /// The Y component of the Vector2.
        /// </summary>
        public double Y;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="value">The value that will initialize this instance.</param>
        public Vector2(double value) {
            X = value;
            Y = value;
        }

        /// <summary>
        /// Constructs a new Vector2.
        /// </summary>
        /// <param name="x">The x coordinate of the net Vector2.</param>
        /// <param name="y">The y coordinate of the net Vector2.</param>
        public Vector2(double x, double y) {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Constructs a new Vector2.
        /// </summary>
        /// <param name="x">The x coordinate of the net Vector2.</param>
        /// <param name="y">The y coordinate of the net Vector2.</param>
        public Vector2(System.Windows.Point p) {
            X = p.X;
            Y = p.Y;
        }

        /// <summary>
        /// Gets or sets the value at the index of the Vector.
        /// </summary>
        public double this[int index] {
            get {
                if( index == 0 ) {
                    return X;
                }
                else if( index == 1 ) {
                    return Y;
                }
                throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
            }
            set {
                if( index == 0 ) {
                    X = value;
                }
                else if( index == 1 ) {
                    Y = value;
                }
                else {
                    throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
                }
            }
        }

        /// <summary>
        /// Gets the length (magnitude) of the vector.
        /// </summary>
        /// <see cref="LengthFast"/>
        /// <seealso cref="LengthSquared"/>
        [JsonIgnore]
        public double Length {
            get {
                return Math.Sqrt(X * X + Y * Y);
            }
        }

        /// <summary>
        /// Gets an approximation of the vector length (magnitude).
        /// </summary>
        /// <remarks>
        /// This property uses an approximation of the square root function to calculate vector magnitude, with
        /// an upper error bound of 0.001.
        /// </remarks>
        /// <see cref="Length"/>
        /// <seealso cref="LengthSquared"/>
        [JsonIgnore]
        public double LengthFast {
            get {
                return 1.0f / MathHelper.InverseSqrtFast(X * X + Y * Y);
            }
        }

        /// <summary>
        /// Gets the square of the vector length (magnitude).
        /// </summary>
        /// <remarks>
        /// This property avoids the costly square root operation required by the Length property. This makes it more suitable
        /// for comparisons.
        /// </remarks>
        /// <see cref="Length"/>
        /// <seealso cref="LengthFast"/>
        [JsonIgnore]
        public double LengthSquared {
            get {
                return X * X + Y * Y;
            }
        }

        /// <summary>
        /// Gets the angle (direction) of the vector.
        /// </summary>
        [JsonIgnore]
        public double Theta => Y < 0 ? -Math.Acos(X / Math.Sqrt(X * X + Y * Y)) : Math.Acos(X / Math.Sqrt(X * X + Y * Y));

        /// <summary>
        /// Gets the perpendicular vector on the right side of this vector.
        /// </summary>
        [JsonIgnore]
        public Vector2 PerpendicularRight {
            get {
                return new Vector2(Y, -X);
            }
        }

        /// <summary>
        /// Gets the perpendicular vector on the left side of this vector.
        /// </summary>
        [JsonIgnore]
        public Vector2 PerpendicularLeft {
            get {
                return new Vector2(-Y, X);
            }
        }

        /// <summary>
        /// Returns a copy of the Vector2 scaled to unit length.
        /// </summary>
        /// <returns></returns>
        public Vector2 Normalized() {
            Vector2 v = this;
            v.Normalize();
            return v;
        }
        /// <summary>
        /// Scales the Vector2 to unit length.
        /// </summary>
        public void Normalize() {
            double scale = 1.0f / this.Length;
            X *= scale;
            Y *= scale;
        }

        /// <summary>
        /// Scales the Vector2 to approximately unit length.
        /// </summary>
        public void NormalizeFast() {
            double scale = MathHelper.InverseSqrtFast(X * X + Y * Y);
            X *= scale;
            Y *= scale;
        }

        /// <summary>
        /// Rounds the coordinates to integer values.
        /// </summary>
        public void Round() {
            X = Math.Round(X);
            Y = Math.Round(Y);
        }

        /// <summary>
        /// Rounds the coordinates to integer values.
        /// </summary>
        public Vector2 Rounded() {
            return new Vector2(Math.Round(X), Math.Round(Y));
        }

        /// <summary>
        /// Ceils the coordinates to integer values.
        /// </summary>
        public Vector2 Ceiled() {
            return new Vector2(Math.Ceiling(X), Math.Ceiling(Y));
        }

        /// <summary>
        /// Defines a unit-length Vector2 that points towards the X-axis.
        /// </summary>
        public static readonly Vector2 UnitX = new Vector2(1, 0);

        /// <summary>
        /// Defines a unit-length Vector2 that points towards the Y-axis.
        /// </summary>
        public static readonly Vector2 UnitY = new Vector2(0, 1);

        /// <summary>
        /// Defines a zero-length Vector2.
        /// </summary>
        public static readonly Vector2 Zero = new Vector2(0, 0);

        /// <summary>
        /// Defines an instance with all components set to 1.
        /// </summary>
        public static readonly Vector2 One = new Vector2(1, 1);

        /// <summary>
        /// Defines an instance with all components set to NaN.
        /// </summary>
        public static readonly Vector2 NaN = new Vector2(double.NaN, double.NaN);

        /// <summary>
        /// Defines the size of the Vector2 struct in bytes.
        /// </summary>
        public static readonly int SizeInBytes = Marshal.SizeOf(new Vector2());

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <returns>Result of operation.</returns>
        public static Vector2 Add(Vector2 a, Vector2 b) {
            Add(ref a, ref b, out a);
            return a;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        /// <param name="result">Result of operation.</param>
        public static void Add(ref Vector2 a, ref Vector2 b, out Vector2 result) {
            result.X = a.X + b.X;
            result.Y = a.Y + b.Y;
        }

        /// <summary>
        /// Subtract one Vector from another
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <returns>Result of subtraction</returns>
        public static Vector2 Subtract(Vector2 a, Vector2 b) {
            Subtract(ref a, ref b, out a);
            return a;
        }

        /// <summary>
        /// Subtract one Vector from another
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <param name="result">Result of subtraction</param>
        public static void Subtract(ref Vector2 a, ref Vector2 b, out Vector2 result) {
            result.X = a.X - b.X;
            result.Y = a.Y - b.Y;
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        public static Vector2 Multiply(Vector2 vector, double scale) {
            Multiply(ref vector, scale, out vector);
            return vector;
        }

        /// <summary>
        /// Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        public static void Multiply(ref Vector2 vector, double scale, out Vector2 result) {
            result.X = vector.X * scale;
            result.Y = vector.Y * scale;
        }

        /// <summary>
        /// Multiplies a vector by the components a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        public static Vector2 Multiply(Vector2 vector, Vector2 scale) {
            Multiply(ref vector, ref scale, out vector);
            return vector;
        }

        /// <summary>
        /// Multiplies a vector by the components of a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        public static void Multiply(ref Vector2 vector, ref Vector2 scale, out Vector2 result) {
            result.X = vector.X * scale.X;
            result.Y = vector.Y * scale.Y;
        }

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        public static Vector2 Divide(Vector2 vector, double scale) {
            Divide(ref vector, scale, out vector);
            return vector;
        }

        /// <summary>
        /// Divides a vector by a scalar.
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        public static void Divide(ref Vector2 vector, double scale, out Vector2 result) {
            result.X = vector.X / scale;
            result.Y = vector.Y / scale;
        }

        /// <summary>
        /// Divides a vector by the components of a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of the operation.</returns>
        public static Vector2 Divide(Vector2 vector, Vector2 scale) {
            Divide(ref vector, ref scale, out vector);
            return vector;
        }

        /// <summary>
        /// Divide a vector by the components of a vector (scale).
        /// </summary>
        /// <param name="vector">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <param name="result">Result of the operation.</param>
        public static void Divide(ref Vector2 vector, ref Vector2 scale, out Vector2 result) {
            result.X = vector.X / scale.X;
            result.Y = vector.Y / scale.Y;
        }

        /// <summary>
        /// Returns a vector created from the smallest of the corresponding components of the given vectors.
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <returns>The component-wise minimum</returns>
        public static Vector2 ComponentMin(Vector2 a, Vector2 b) {
            a.X = a.X < b.X ? a.X : b.X;
            a.Y = a.Y < b.Y ? a.Y : b.Y;
            return a;
        }

        /// <summary>
        /// Returns a vector created from the smallest of the corresponding components of the given vectors.
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <param name="result">The component-wise minimum</param>
        public static void ComponentMin(ref Vector2 a, ref Vector2 b, out Vector2 result) {
            result.X = a.X < b.X ? a.X : b.X;
            result.Y = a.Y < b.Y ? a.Y : b.Y;
        }

        /// <summary>
        /// Returns a vector created from the largest of the corresponding components of the given vectors.
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <returns>The component-wise maximum</returns>
        public static Vector2 ComponentMax(Vector2 a, Vector2 b) {
            a.X = a.X > b.X ? a.X : b.X;
            a.Y = a.Y > b.Y ? a.Y : b.Y;
            return a;
        }

        /// <summary>
        /// Returns a vector created from the largest of the corresponding components of the given vectors.
        /// </summary>
        /// <param name="a">First operand</param>
        /// <param name="b">Second operand</param>
        /// <param name="result">The component-wise maximum</param>
        public static void ComponentMax(ref Vector2 a, ref Vector2 b, out Vector2 result) {
            result.X = a.X > b.X ? a.X : b.X;
            result.Y = a.Y > b.Y ? a.Y : b.Y;
        }

        /// <summary>
        /// Returns the Vector2 with the minimum magnitude. If the magnitudes are equal, the second vector
        /// is selected.
        /// </summary>
        /// <param name="left">Left operand</param>
        /// <param name="right">Right operand</param>
        /// <returns>The minimum Vector2</returns>
        public static Vector2 MagnitudeMin(Vector2 left, Vector2 right) {
            return left.LengthSquared < right.LengthSquared ? left : right;
        }

        /// <summary>
        /// Returns the Vector2 with the minimum magnitude. If the magnitudes are equal, the second vector
        /// is selected.
        /// </summary>
        /// <param name="left">Left operand</param>
        /// <param name="right">Right operand</param>
        /// <param name="result">The magnitude-wise minimum</param>
        /// <returns>The minimum Vector2</returns>
        public static void MagnitudeMin(ref Vector2 left, ref Vector2 right, out Vector2 result) {
            result = left.LengthSquared < right.LengthSquared ? left : right;
        }

        /// <summary>
        /// Returns the Vector2 with the maximum magnitude. If the magnitudes are equal, the first vector
        /// is selected.
        /// </summary>
        /// <param name="left">Left operand</param>
        /// <param name="right">Right operand</param>
        /// <returns>The maximum Vector2</returns>
        public static Vector2 MagnitudeMax(Vector2 left, Vector2 right) {
            return left.LengthSquared >= right.LengthSquared ? left : right;
        }

        /// <summary>
        /// Returns the Vector2 with the maximum magnitude. If the magnitudes are equal, the first vector
        /// is selected.
        /// </summary>
        /// <param name="left">Left operand</param>
        /// <param name="right">Right operand</param>
        /// <param name="result">The magnitude-wise maximum</param>
        /// <returns>The maximum Vector2</returns>
        public static void MagnitudeMax(ref Vector2 left, ref Vector2 right, out Vector2 result) {
            result = left.LengthSquared >= right.LengthSquared ? left : right;
        }

        /// <summary>
        /// Returns the Vector3 with the minimum magnitude
        /// </summary>
        /// <param name="left">Left operand</param>
        /// <param name="right">Right operand</param>
        /// <returns>The minimum Vector3</returns>
        [Obsolete("Use MagnitudeMin() instead.")]
        public static Vector2 Min(Vector2 left, Vector2 right) {
            return left.LengthSquared < right.LengthSquared ? left : right;
        }

        /// <summary>
        /// Returns the Vector3 with the minimum magnitude
        /// </summary>
        /// <param name="left">Left operand</param>
        /// <param name="right">Right operand</param>
        /// <returns>The minimum Vector3</returns>
        [Obsolete("Use MagnitudeMax() instead.")]
        public static Vector2 Max(Vector2 left, Vector2 right) {
            return left.LengthSquared >= right.LengthSquared ? left : right;
        }

        /// <summary>
        /// Clamp a vector to the given minimum and maximum vectors
        /// </summary>
        /// <param name="vec">Input vector</param>
        /// <param name="min">Minimum vector</param>
        /// <param name="max">Maximum vector</param>
        /// <returns>The clamped vector</returns>
        public static Vector2 Clamp(Vector2 vec, Vector2 min, Vector2 max) {
            vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
            vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
            return vec;
        }

        /// <summary>
        /// Clamp a vector to the given minimum and maximum vectors
        /// </summary>
        /// <param name="vec">Input vector</param>
        /// <param name="min">Minimum vector</param>
        /// <param name="max">Maximum vector</param>
        /// <param name="result">The clamped vector</param>
        public static void Clamp(ref Vector2 vec, ref Vector2 min, ref Vector2 max, out Vector2 result) {
            result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
            result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
        }

        /// <summary>
        /// Compute the euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <returns>The distance</returns>
        public static double Distance(Vector2 vec1, Vector2 vec2) {
            Distance(ref vec1, ref vec2, out double result);
            return result;
        }

        /// <summary>
        /// Compute the euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="result">The distance</param>
        public static void Distance(ref Vector2 vec1, ref Vector2 vec2, out double result) {
            result = Math.Sqrt(( vec2.X - vec1.X ) * ( vec2.X - vec1.X ) + ( vec2.Y - vec1.Y ) * ( vec2.Y - vec1.Y ));
        }

        /// <summary>
        /// Compute the euclidean distance between a vector and a line.
        /// </summary>
        /// <param name="vec1">The vector</param>
        /// <param name="line">The line</param>
        /// <returns>The distance</returns>
        public static double Distance(Vector2 vec1, Line2 line)
        {
            Distance(ref vec1, ref line, out double result);
            return result;
        }

        /// <summary>
        /// Compute the euclidean distance between a vector and a line.
        /// </summary>
        /// <param name="vec1">The vector</param>
        /// <param name="line">The line</param>
        /// <param name="result">The distance</param>
        public static void Distance(ref Vector2 vec1, ref Line2 line, out double result) {
            result = Line2.Distance(line, vec1);
        }

        /// <summary>
        /// Compute the squared euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <returns>The squared distance</returns>
        public static double DistanceSquared(Vector2 vec1, Vector2 vec2) {
            DistanceSquared(ref vec1, ref vec2, out double result);
            return result;
        }

        /// <summary>
        /// Compute the squared euclidean distance between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="result">The squared distance</param>
        public static void DistanceSquared(ref Vector2 vec1, ref Vector2 vec2, out double result) {
            result = ( vec2.X - vec1.X ) * ( vec2.X - vec1.X ) + ( vec2.Y - vec1.Y ) * ( vec2.Y - vec1.Y );
        }

        /// <summary>
        /// Compute the angle between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <returns>The angle</returns>
        public static double Angle(Vector2 vec1, Vector2 vec2)
        {
            Angle(ref vec1, ref vec2, out double result);
            return result;
        }

        /// <summary>
        /// Compute angle between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="result">The angle</param>
        public static void Angle(ref Vector2 vec1, ref Vector2 vec2, out double result)
        {
            result = Math.Acos(Dot(vec1, vec2) / (vec1.Length * vec2.Length));
        }

        /// <summary>
        /// Compute the angle between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <returns>The angle</returns>
        public static double Angle(Vector2 vec1)
        {
            Angle(ref vec1, out double result);
            return result;
        }

        /// <summary>
        /// Compute angle between two vectors.
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="result">The angle</param>
        public static void Angle(ref Vector2 vec1, out double result)
        {
            result = Math.Acos(vec1.X / vec1.Length);
        }

        /// <summary>
        /// Scale a vector to unit length
        /// </summary>
        /// <param name="vec">The input vector</param>
        /// <returns>The normalized vector</returns>
        public static Vector2 Normalize(Vector2 vec) {
            double scale = 1.0f / vec.Length;
            vec.X *= scale;
            vec.Y *= scale;
            return vec;
        }

        /// <summary>
        /// Scale a vector to unit length
        /// </summary>
        /// <param name="vec">The input vector</param>
        /// <param name="result">The normalized vector</param>
        public static void Normalize(ref Vector2 vec, out Vector2 result) {
            double scale = 1.0f / vec.Length;
            result.X = vec.X * scale;
            result.Y = vec.Y * scale;
        }

        /// <summary>
        /// Scale a vector to approximately unit length
        /// </summary>
        /// <param name="vec">The input vector</param>
        /// <returns>The normalized vector</returns>
        public static Vector2 NormalizeFast(Vector2 vec) {
            double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y);
            vec.X *= scale;
            vec.Y *= scale;
            return vec;
        }

        /// <summary>
        /// Scale a vector to approximately unit length
        /// </summary>
        /// <param name="vec">The input vector</param>
        /// <param name="result">The normalized vector</param>
        public static void NormalizeFast(ref Vector2 vec, out Vector2 result) {
            double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y);
            result.X = vec.X * scale;
            result.Y = vec.Y * scale;
        }

        /// <summary>
        /// Calculate the dot (scalar) product of two vectors
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The dot product of the two inputs</returns>
        public static double Dot(Vector2 left, Vector2 right) {
            return left.X * right.X + left.Y * right.Y;
        }

        /// <summary>
        /// Calculate the dot (scalar) product of two vectors
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <param name="result">The dot product of the two inputs</param>
        public static void Dot(ref Vector2 left, ref Vector2 right, out double result) {
            result = left.X * right.X + left.Y * right.Y;
        }

        /// <summary>
        /// Calculate the perpendicular dot (scalar) product of two vectors
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The perpendicular dot product of the two inputs</returns>
        public static double PerpDot(Vector2 left, Vector2 right) {
            return left.X * right.Y - left.Y * right.X;
        }

        /// <summary>
        /// Calculate the perpendicular dot (scalar) product of two vectors
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <param name="result">The perpendicular dot product of the two inputs</param>
        public static void PerpDot(ref Vector2 left, ref Vector2 right, out double result) {
            result = left.X * right.Y - left.Y * right.X;
        }

        /// <summary>
        /// Calculate the product of two vectors as if they are complex numbers
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The complex product the two inputs</returns>
        public static Vector2 ComplexProduct(Vector2 left, Vector2 right) {
            return new Vector2(left.X * right.X - left.Y * right.Y, left.X * right.Y + left.Y * right.X);
        }

        /// <summary>
        /// Calculate the product of two vectors as if they are complex numbers
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <param name="result">The complex product of the two inputs</param>
        public static void ComplexProduct(ref Vector2 left, ref Vector2 right, out Vector2 result) {
            result = new Vector2(left.X * right.X - left.Y * right.Y, left.X * right.Y + left.Y * right.X);
        }

        /// <summary>
        /// Calculate the quotient of two vectors as if they are complex numbers
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The complex quotient the two inputs</returns>
        public static Vector2 ComplexQuotient(Vector2 left, Vector2 right) {
            return new Vector2(right.X * left.X + right.Y * left.Y, right.X * left.Y - right.Y * left.X) / right.LengthSquared;
        }

        /// <summary>
        /// Calculate the quotient of two vectors as if they are complex numbers
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <param name="result">The complex quotient of the two inputs</param>
        public static void ComplexQuotient(ref Vector2 left, ref Vector2 right, out Vector2 result) {
            result = new Vector2(left.X * right.X + left.Y * right.Y, left.X * right.Y - left.Y * right.X) / left.LengthSquared;
        }

        /// <summary>
        /// Calculate the mirror projection of the given vector and line
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The mirror projection of the input</returns>
        public static Vector2 Mirror(Vector2 left, Line2 right) {
            Mirror(ref left, ref right, out Vector2 result);
            return result;
        }

        /// <summary>
        /// Calculate the mirror projection of the given vector and line
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The mirror projection of the input</returns>
        public static void Mirror(ref Vector2 left, ref Line2 right, out Vector2 result) {
            var nearest = Line2.NearestPoint(right, left);
            result = left + 2 * (nearest - left);
        }

        /// <summary>
        /// Project given vector onto a line
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The closest point on the line</returns>
        public static Vector2 Snap(Vector2 left, Line2 right) {
            Snap(ref left, ref right, out Vector2 result);
            return result;
        }

        /// <summary>
        /// Project given vector onto a line
        /// </summary>
        /// <param name="left">First operand</param>
        /// <param name="right">Second operand</param>
        /// <returns>The closest point on the line</returns>
        public static void Snap(ref Vector2 left, ref Line2 right, out Vector2 result) {
            result = Line2.NearestPoint(right, left);
        }

        /// <summary>
        /// Returns a new Vector that is the linear blend of the 2 given Vectors
        /// </summary>
        /// <param name="a">First input vector</param>
        /// <param name="b">Second input vector</param>
        /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
        /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
        public static Vector2 Lerp(Vector2 a, Vector2 b, double blend) {
            a.X = blend * ( b.X - a.X ) + a.X;
            a.Y = blend * ( b.Y - a.Y ) + a.Y;
            return a;
        }

        /// <summary>
        /// Returns a new Vector that is the linear blend of the 2 given Vectors
        /// </summary>
        /// <param name="a">First input vector</param>
        /// <param name="b">Second input vector</param>
        /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
        /// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
        public static void Lerp(ref Vector2 a, ref Vector2 b, double blend, out Vector2 result) {
            result.X = blend * ( b.X - a.X ) + a.X;
            result.Y = blend * ( b.Y - a.Y ) + a.Y;
        }

        /// <summary>
        /// Interpolate 3 Vectors using Barycentric coordinates
        /// </summary>
        /// <param name="a">First input Vector</param>
        /// <param name="b">Second input Vector</param>
        /// <param name="c">Third input Vector</param>
        /// <param name="u">First Barycentric Coordinate</param>
        /// <param name="v">Second Barycentric Coordinate</param>
        /// <returns>a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</returns>
        public static Vector2 BaryCentric(Vector2 a, Vector2 b, Vector2 c, double u, double v) {
            return a + u * ( b - a ) + v * ( c - a );
        }

        /// <summary>Interpolate 3 Vectors using Barycentric coordinates</summary>
        /// <param name="a">First input Vector.</param>
        /// <param name="b">Second input Vector.</param>
        /// <param name="c">Third input Vector.</param>
        /// <param name="u">First Barycentric Coordinate.</param>
        /// <param name="v">Second Barycentric Coordinate.</param>
        /// <param name="result">Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</param>
        public static void BaryCentric(ref Vector2 a, ref Vector2 b, ref Vector2 c, double u, double v, out Vector2 result) {
            result = a; // copy

            Vector2 temp = b; // copy
            Subtract(ref temp, ref a, out temp);
            Multiply(ref temp, u, out temp);
            Add(ref result, ref temp, out result);

            temp = c; // copy
            Subtract(ref temp, ref a, out temp);
            Multiply(ref temp, v, out temp);
            Add(ref result, ref temp, out result);
        }

        /// <summary>
        /// Rotate a vector by an angle.
        /// </summary>
        /// <param name="vec">The vector to rotate.</param>
        /// <param name="angle">The angle in radians to rotate the vector by.</param>
        /// <returns>The result of the operation.</returns>
        public static Vector2 Rotate(Vector2 vec, double angle) {
            Rotate(ref vec, ref angle, out Vector2 result);
            return result;
        }

        /// <summary>
        /// Rotate a vector by an angle.
        /// </summary>
        /// <param name="vec">The vector to rotate.</param>
        /// <param name="angle">The angle in radians to rotate the vector by.</param>
        /// <param name="result">The result of the operation.</param>
        public static void Rotate(ref Vector2 vec, ref double angle, out Vector2 result) {
            result.X = vec.X * Math.Cos(angle) - vec.Y * Math.Sin(angle);
            result.Y = vec.X * Math.Sin(angle) + vec.Y * Math.Cos(angle);
        }

        /// <summary>
        /// Transforms a vector by a quaternion rotation.
        /// </summary>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="quat">The quaternion to rotate the vector by.</param>
        /// <returns>The result of the operation.</returns>
        public static Vector2 Transform(Vector2 vec, Quaternion quat) {
            Transform(ref vec, ref quat, out Vector2 result);
            return result;
        }

        /// <summary>
        /// Transforms a vector by a quaternion rotation.
        /// </summary>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="quat">The quaternion to rotate the vector by.</param>
        /// <param name="result">The result of the operation.</param>
        public static void Transform(ref Vector2 vec, ref Quaternion quat, out Vector2 result) {
            Quaternion v = new Quaternion(vec.X, vec.Y, 0, 0);
            Quaternion.Invert(ref quat, out Quaternion i);
            Quaternion.Multiply(ref quat, ref v, out Quaternion t);
            Quaternion.Multiply(ref t, ref i, out v);

            result.X = v.X;
            result.Y = v.Y;
        }

        /// <summary>
        /// Gets or sets an osuTK.Vector2 with the Y and X components of this instance.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public Vector2 Yx { get { return new Vector2(Y, X); } set { Y = value.X; X = value.Y; } }

        /// <summary>
        /// Adds the specified instances.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Result of addition.</returns>
        public static Vector2 operator +(Vector2 left, Vector2 right) {
            left.X += right.X;
            left.Y += right.Y;
            return left;
        }

        /// <summary>
        /// Subtracts the specified instances.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Result of subtraction.</returns>
        public static Vector2 operator -(Vector2 left, Vector2 right) {
            left.X -= right.X;
            left.Y -= right.Y;
            return left;
        }

        /// <summary>
        /// Negates the specified instance.
        /// </summary>
        /// <param name="vec">Operand.</param>
        /// <returns>Result of negation.</returns>
        public static Vector2 operator -(Vector2 vec) {
            vec.X = -vec.X;
            vec.Y = -vec.Y;
            return vec;
        }

        /// <summary>
        /// Multiplies the specified instance by a scalar.
        /// </summary>
        /// <param name="vec">Left operand.</param>
        /// <param name="scale">Right operand.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector2 operator *(Vector2 vec, double scale) {
            vec.X *= scale;
            vec.Y *= scale;
            return vec;
        }

        /// <summary>
        /// Multiplies the specified instance by a scalar.
        /// </summary>
        /// <param name="scale">Left operand.</param>
        /// <param name="vec">Right operand.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector2 operator *(double scale, Vector2 vec) {
            vec.X *= scale;
            vec.Y *= scale;
            return vec;
        }

        /// <summary>
        /// Component-wise multiplication between the specified instance by a scale vector.
        /// </summary>
        /// <param name="scale">Left operand.</param>
        /// <param name="vec">Right operand.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector2 operator *(Vector2 vec, Vector2 scale) {
            vec.X *= scale.X;
            vec.Y *= scale.Y;
            return vec;
        }

        /// <summary>
        /// Divides the specified instance by a scalar.
        /// </summary>
        /// <param name="vec">Left operand</param>
        /// <param name="scale">Right operand</param>
        /// <returns>Result of the division.</returns>
        public static Vector2 operator /(Vector2 vec, double scale) {
            vec.X /= scale;
            vec.Y /= scale;
            return vec;
        }

        /// <summary>
        /// Compares the specified instances for equality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are equal; false otherwise.</returns>
        public static bool operator ==(Vector2 left, Vector2 right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares the specified instances for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if both instances are not equal; false otherwise.</returns>
        public static bool operator !=(Vector2 left, Vector2 right) {
            return !left.Equals(right);
        }

        private static readonly string listSeparator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        /// <summary>
        /// Returns a System.string that represents the current Vector2.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Format("({0}{2} {1})", X, Y, listSeparator);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
            unchecked {
                return (X.GetHashCode() * 397 ) ^ Y.GetHashCode();
            }
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj) {
            if( !( obj is Vector2 ) ) {
                return false;
            }

            return Equals((Vector2) obj);
        }

        /// <summary>Indicates whether the current vector is equal to another vector.</summary>
        /// <param name="other">A vector to compare with this vector.</param>
        /// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
        public bool Equals(Vector2 other) {
            return
                X == other.X &&
                Y == other.Y;
        }
    }
}
