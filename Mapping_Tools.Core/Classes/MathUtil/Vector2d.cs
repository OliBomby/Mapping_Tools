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

using System.Globalization;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace Mapping_Tools.Core.Classes.MathUtil;

/// <summary>Represents a 2D vector using two double-precision floating-point numbers.</summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector2D : IEquatable<Vector2D>
{
    /// <summary>The X coordinate of this instance.</summary>
    public double X;

    /// <summary>The Y coordinate of this instance.</summary>
    public double Y;

    /// <summary>
    ///     Defines a unit-length Vector2d that points towards the X-axis.
    /// </summary>
    public static readonly Vector2D UnitX = new(1, 0);

    /// <summary>
    ///     Defines a unit-length Vector2d that points towards the Y-axis.
    /// </summary>
    public static readonly Vector2D UnitY = new(0, 1);

    /// <summary>
    ///     Defines a zero-length Vector2d.
    /// </summary>
    public static readonly Vector2D Zero = new(0, 0);

    /// <summary>
    ///     Defines an instance with all components set to 1.
    /// </summary>
    public static readonly Vector2D One = new(1, 1);

    /// <summary>
    ///     Defines the size of the Vector2d struct in bytes.
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf(new Vector2D());

    /// <summary>
    ///     Constructs a new instance.
    /// </summary>
    /// <param name="value">The value that will initialize this instance.</param>
    public Vector2D(double value)
    {
        X = value;
        Y = value;
    }

    /// <summary>Constructs left vector with the given coordinates.</summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    ///     Gets or sets the value at the index of the Vector.
    /// </summary>
    public double this[int index]
    {
        get
        {
            if (index == 0) return X;

            if (index == 1) return Y;
            throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
        }
        set
        {
            if (index == 0)
                X = value;
            else if (index == 1)
                Y = value;
            else
                throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
        }
    }

    /// <summary>
    ///     Gets the length (magnitude) of the vector.
    /// </summary>
    /// <seealso cref="LengthSquared" />
    public double Length => Math.Sqrt(X * X + Y * Y);

    /// <summary>
    ///     Gets the square of the vector length (magnitude).
    /// </summary>
    /// <remarks>
    ///     This property avoids the costly square root operation required by the Length property. This makes it more suitable
    ///     for comparisons.
    /// </remarks>
    /// <see cref="Length" />
    public double LengthSquared => X * X + Y * Y;

    /// <summary>
    ///     Gets the perpendicular vector on the right side of this vector.
    /// </summary>
    public Vector2D PerpendicularRight => new(Y, -X);

    /// <summary>
    ///     Gets the perpendicular vector on the left side of this vector.
    /// </summary>
    public Vector2D PerpendicularLeft => new(-Y, X);

    /// <summary>
    ///     Returns a copy of the Vector2d scaled to unit length.
    /// </summary>
    /// <returns></returns>
    public Vector2D Normalized()
    {
        var v = this;
        v.Normalize();
        return v;
    }

    /// <summary>
    ///     Scales the Vector2 to unit length.
    /// </summary>
    public void Normalize()
    {
        double scale = 1.0 / Length;
        X *= scale;
        Y *= scale;
    }

    /// <summary>
    ///     Adds two vectors.
    /// </summary>
    /// <param name="a">Left operand.</param>
    /// <param name="b">Right operand.</param>
    /// <returns>Result of operation.</returns>
    public static Vector2D Add(Vector2D a, Vector2D b)
    {
        Add(ref a, ref b, out a);
        return a;
    }

    /// <summary>
    ///     Adds two vectors.
    /// </summary>
    /// <param name="a">Left operand.</param>
    /// <param name="b">Right operand.</param>
    /// <param name="result">Result of operation.</param>
    public static void Add(ref Vector2D a, ref Vector2D b, out Vector2D result)
    {
        result.X = a.X + b.X;
        result.Y = a.Y + b.Y;
    }

    /// <summary>
    ///     Subtract one Vector from another
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>Result of subtraction</returns>
    public static Vector2D Subtract(Vector2D a, Vector2D b)
    {
        Subtract(ref a, ref b, out a);
        return a;
    }

    /// <summary>
    ///     Subtract one Vector from another
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">Result of subtraction</param>
    public static void Subtract(ref Vector2D a, ref Vector2D b, out Vector2D result)
    {
        result.X = a.X - b.X;
        result.Y = a.Y - b.Y;
    }

    /// <summary>
    ///     Multiplies a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector2D Multiply(Vector2D vector, double scale)
    {
        Multiply(ref vector, scale, out vector);
        return vector;
    }

    /// <summary>
    ///     Multiplies a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Multiply(ref Vector2D vector, double scale, out Vector2D result)
    {
        result.X = vector.X * scale;
        result.Y = vector.Y * scale;
    }

    /// <summary>
    ///     Multiplies a vector by the components a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector2D Multiply(Vector2D vector, Vector2D scale)
    {
        Multiply(ref vector, ref scale, out vector);
        return vector;
    }

    /// <summary>
    ///     Multiplies a vector by the components of a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Multiply(ref Vector2D vector, ref Vector2D scale, out Vector2D result)
    {
        result.X = vector.X * scale.X;
        result.Y = vector.Y * scale.Y;
    }

    /// <summary>
    ///     Divides a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector2D Divide(Vector2D vector, double scale)
    {
        Divide(ref vector, scale, out vector);
        return vector;
    }

    /// <summary>
    ///     Divides a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Divide(ref Vector2D vector, double scale, out Vector2D result)
    {
        result.X = vector.X / scale;
        result.Y = vector.Y / scale;
    }

    /// <summary>
    ///     Divides a vector by the components of a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector2D Divide(Vector2D vector, Vector2D scale)
    {
        Divide(ref vector, ref scale, out vector);
        return vector;
    }

    /// <summary>
    ///     Divide a vector by the components of a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Divide(ref Vector2D vector, ref Vector2D scale, out Vector2D result)
    {
        result.X = vector.X / scale.X;
        result.Y = vector.Y / scale.Y;
    }

    /// <summary>
    ///     Calculate the component-wise minimum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise minimum</returns>
    [Obsolete("Use ComponentMin() instead.")]
    public static Vector2D Min(Vector2D a, Vector2D b)
    {
        a.X = a.X < b.X ? a.X : b.X;
        a.Y = a.Y < b.Y ? a.Y : b.Y;
        return a;
    }

    /// <summary>
    ///     Calculate the component-wise minimum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise minimum</param>
    [Obsolete("Use ComponentMin() instead.")]
    public static void Min(ref Vector2D a, ref Vector2D b, out Vector2D result)
    {
        result.X = a.X < b.X ? a.X : b.X;
        result.Y = a.Y < b.Y ? a.Y : b.Y;
    }

    /// <summary>
    ///     Calculate the component-wise maximum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise maximum</returns>
    [Obsolete("Use ComponentMax() instead.")]
    public static Vector2D Max(Vector2D a, Vector2D b)
    {
        a.X = a.X > b.X ? a.X : b.X;
        a.Y = a.Y > b.Y ? a.Y : b.Y;
        return a;
    }

    /// <summary>
    ///     Calculate the component-wise maximum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise maximum</param>
    [Obsolete("Use ComponentMax() instead.")]
    public static void Max(ref Vector2D a, ref Vector2D b, out Vector2D result)
    {
        result.X = a.X > b.X ? a.X : b.X;
        result.Y = a.Y > b.Y ? a.Y : b.Y;
    }

    /// <summary>
    ///     Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise minimum</returns>
    public static Vector2D ComponentMin(Vector2D a, Vector2D b)
    {
        a.X = a.X < b.X ? a.X : b.X;
        a.Y = a.Y < b.Y ? a.Y : b.Y;
        return a;
    }

    /// <summary>
    ///     Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise minimum</param>
    public static void ComponentMin(ref Vector2D a, ref Vector2D b, out Vector2D result)
    {
        result.X = a.X < b.X ? a.X : b.X;
        result.Y = a.Y < b.Y ? a.Y : b.Y;
    }

    /// <summary>
    ///     Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise maximum</returns>
    public static Vector2D ComponentMax(Vector2D a, Vector2D b)
    {
        a.X = a.X > b.X ? a.X : b.X;
        a.Y = a.Y > b.Y ? a.Y : b.Y;
        return a;
    }

    /// <summary>
    ///     Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise maximum</param>
    public static void ComponentMax(ref Vector2D a, ref Vector2D b, out Vector2D result)
    {
        result.X = a.X > b.X ? a.X : b.X;
        result.Y = a.Y > b.Y ? a.Y : b.Y;
    }

    /// <summary>
    ///     Returns the Vector2d with the minimum magnitude. If the magnitudes are equal, the second vector
    ///     is selected.
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector2d</returns>
    public static Vector2D MagnitudeMin(Vector2D left, Vector2D right)
    {
        return left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector2d with the minimum magnitude. If the magnitudes are equal, the second vector
    ///     is selected.
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <param name="result">The magnitude-wise minimum</param>
    /// <returns>The minimum Vector2d</returns>
    public static void MagnitudeMin(ref Vector2D left, ref Vector2D right, out Vector2D result)
    {
        result = left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector2d with the minimum magnitude. If the magnitudes are equal, the first vector
    ///     is selected.
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector2d</returns>
    public static Vector2D MagnitudeMax(Vector2D left, Vector2D right)
    {
        return left.LengthSquared >= right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector2d with the maximum magnitude. If the magnitudes are equal, the first vector
    ///     is selected.
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <param name="result">The magnitude-wise maximum</param>
    /// <returns>The maximum Vector2d</returns>
    public static void MagnitudeMax(ref Vector2D left, ref Vector2D right, out Vector2D result)
    {
        result = left.LengthSquared >= right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Clamp a vector to the given minimum and maximum vectors
    /// </summary>
    /// <param name="vec">Input vector</param>
    /// <param name="min">Minimum vector</param>
    /// <param name="max">Maximum vector</param>
    /// <returns>The clamped vector</returns>
    public static Vector2D Clamp(Vector2D vec, Vector2D min, Vector2D max)
    {
        vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
        vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
        return vec;
    }

    /// <summary>
    ///     Clamp a vector to the given minimum and maximum vectors
    /// </summary>
    /// <param name="vec">Input vector</param>
    /// <param name="min">Minimum vector</param>
    /// <param name="max">Maximum vector</param>
    /// <param name="result">The clamped vector</param>
    public static void Clamp(ref Vector2D vec, ref Vector2D min, ref Vector2D max, out Vector2D result)
    {
        result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
        result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
    }

    /// <summary>
    ///     Compute the euclidean distance between two vectors.
    /// </summary>
    /// <param name="vec1">The first vector</param>
    /// <param name="vec2">The second vector</param>
    /// <returns>The distance</returns>
    public static double Distance(Vector2D vec1, Vector2D vec2)
    {
        Distance(ref vec1, ref vec2, out double result);
        return result;
    }

    /// <summary>
    ///     Compute the euclidean distance between two vectors.
    /// </summary>
    /// <param name="vec1">The first vector</param>
    /// <param name="vec2">The second vector</param>
    /// <param name="result">The distance</param>
    public static void Distance(ref Vector2D vec1, ref Vector2D vec2, out double result)
    {
        result = Math.Sqrt((vec2.X - vec1.X) * (vec2.X - vec1.X) + (vec2.Y - vec1.Y) * (vec2.Y - vec1.Y));
    }

    /// <summary>
    ///     Compute the squared euclidean distance between two vectors.
    /// </summary>
    /// <param name="vec1">The first vector</param>
    /// <param name="vec2">The second vector</param>
    /// <returns>The squared distance</returns>
    public static double DistanceSquared(Vector2D vec1, Vector2D vec2)
    {
        DistanceSquared(ref vec1, ref vec2, out double result);
        return result;
    }

    /// <summary>
    ///     Compute the squared euclidean distance between two vectors.
    /// </summary>
    /// <param name="vec1">The first vector</param>
    /// <param name="vec2">The second vector</param>
    /// <param name="result">The squared distance</param>
    public static void DistanceSquared(ref Vector2D vec1, ref Vector2D vec2, out double result)
    {
        result = (vec2.X - vec1.X) * (vec2.X - vec1.X) + (vec2.Y - vec1.Y) * (vec2.Y - vec1.Y);
    }

    /// <summary>
    ///     Scale a vector to unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <returns>The normalized vector</returns>
    public static Vector2D Normalize(Vector2D vec)
    {
        double scale = 1.0 / vec.Length;
        vec.X *= scale;
        vec.Y *= scale;
        return vec;
    }

    /// <summary>
    ///     Scale a vector to unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <param name="result">The normalized vector</param>
    public static void Normalize(ref Vector2D vec, out Vector2D result)
    {
        double scale = 1.0 / vec.Length;
        result.X = vec.X * scale;
        result.Y = vec.Y * scale;
    }

    /// <summary>
    ///     Scale a vector to approximately unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <returns>The normalized vector</returns>
    public static Vector2D NormalizeFast(Vector2D vec)
    {
        double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y);
        vec.X *= scale;
        vec.Y *= scale;
        return vec;
    }

    /// <summary>
    ///     Scale a vector to approximately unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <param name="result">The normalized vector</param>
    public static void NormalizeFast(ref Vector2D vec, out Vector2D result)
    {
        double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y);
        result.X = vec.X * scale;
        result.Y = vec.Y * scale;
    }

    /// <summary>
    ///     Calculate the dot (scalar) product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <returns>The dot product of the two inputs</returns>
    public static double Dot(Vector2D left, Vector2D right)
    {
        return left.X * right.X + left.Y * right.Y;
    }

    /// <summary>
    ///     Calculate the dot (scalar) product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <param name="result">The dot product of the two inputs</param>
    public static void Dot(ref Vector2D left, ref Vector2D right, out double result)
    {
        result = left.X * right.X + left.Y * right.Y;
    }

    /// <summary>
    ///     Returns a new Vector that is the linear blend of the 2 given Vectors
    /// </summary>
    /// <param name="a">First input vector</param>
    /// <param name="b">Second input vector</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
    public static Vector2D Lerp(Vector2D a, Vector2D b, double blend)
    {
        a.X = blend * (b.X - a.X) + a.X;
        a.Y = blend * (b.Y - a.Y) + a.Y;
        return a;
    }

    /// <summary>
    ///     Returns a new Vector that is the linear blend of the 2 given Vectors
    /// </summary>
    /// <param name="a">First input vector</param>
    /// <param name="b">Second input vector</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
    public static void Lerp(ref Vector2D a, ref Vector2D b, double blend, out Vector2D result)
    {
        result.X = blend * (b.X - a.X) + a.X;
        result.Y = blend * (b.Y - a.Y) + a.Y;
    }

    /// <summary>
    ///     Interpolate 3 Vectors using Barycentric coordinates
    /// </summary>
    /// <param name="a">First input Vector</param>
    /// <param name="b">Second input Vector</param>
    /// <param name="c">Third input Vector</param>
    /// <param name="u">First Barycentric Coordinate</param>
    /// <param name="v">Second Barycentric Coordinate</param>
    /// <returns>a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</returns>
    public static Vector2D BaryCentric(Vector2D a, Vector2D b, Vector2D c, double u, double v)
    {
        return a + u * (b - a) + v * (c - a);
    }

    /// <summary>Interpolate 3 Vectors using Barycentric coordinates</summary>
    /// <param name="a">First input Vector.</param>
    /// <param name="b">Second input Vector.</param>
    /// <param name="c">Third input Vector.</param>
    /// <param name="u">First Barycentric Coordinate.</param>
    /// <param name="v">Second Barycentric Coordinate.</param>
    /// <param name="result">
    ///     Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c
    ///     otherwise
    /// </param>
    public static void BaryCentric(ref Vector2D a, ref Vector2D b, ref Vector2D c, double u, double v, out Vector2D result)
    {
        result = a; // copy

        var temp = b; // copy
        Subtract(ref temp, ref a, out temp);
        Multiply(ref temp, u, out temp);
        Add(ref result, ref temp, out result);

        temp = c; // copy
        Subtract(ref temp, ref a, out temp);
        Multiply(ref temp, v, out temp);
        Add(ref result, ref temp, out result);
    }

    /// <summary>
    ///     Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D Transform(Vector2D vec, Quaterniond quat)
    {
        Transform(ref vec, ref quat, out var result);
        return result;
    }

    /// <summary>
    ///     Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <param name="result">The result of the operation.</param>
    public static void Transform(ref Vector2D vec, ref Quaterniond quat, out Vector2D result)
    {
        var v = new Quaterniond(vec.X, vec.Y, 0, 0);
        Quaterniond.Invert(ref quat, out var i);
        Quaterniond.Multiply(ref quat, ref v, out var t);
        Quaterniond.Multiply(ref t, ref i, out v);

        result.X = v.X;
        result.Y = v.Y;
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the Y and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Yx
    {
        get => new(Y, X);
        set
        {
            Y = value.X;
            X = value.Y;
        }
    }

    /// <summary>
    ///     Adds two instances.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D operator +(Vector2D left, Vector2D right)
    {
        left.X += right.X;
        left.Y += right.Y;
        return left;
    }

    /// <summary>
    ///     Subtracts two instances.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D operator -(Vector2D left, Vector2D right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        return left;
    }

    /// <summary>
    ///     Negates an instance.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D operator -(Vector2D vec)
    {
        vec.X = -vec.X;
        vec.Y = -vec.Y;
        return vec;
    }

    /// <summary>
    ///     Multiplies an instance by a scalar.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <param name="f">The scalar.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D operator *(Vector2D vec, double f)
    {
        vec.X *= f;
        vec.Y *= f;
        return vec;
    }

    /// <summary>
    ///     Multiply an instance by a scalar.
    /// </summary>
    /// <param name="f">The scalar.</param>
    /// <param name="vec">The instance.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D operator *(double f, Vector2D vec)
    {
        vec.X *= f;
        vec.Y *= f;
        return vec;
    }

    /// <summary>
    ///     Component-wise multiplication between the specified instance by a scale vector.
    /// </summary>
    /// <param name="scale">Left operand.</param>
    /// <param name="vec">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    public static Vector2D operator *(Vector2D vec, Vector2D scale)
    {
        vec.X *= scale.X;
        vec.Y *= scale.Y;
        return vec;
    }

    /// <summary>
    ///     Divides an instance by a scalar.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <param name="f">The scalar.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector2D operator /(Vector2D vec, double f)
    {
        vec.X /= f;
        vec.Y /= f;
        return vec;
    }

    /// <summary>
    ///     Compares two instances for equality.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns>True, if both instances are equal; false otherwise.</returns>
    public static bool operator ==(Vector2D left, Vector2D right) => left.Equals(right);

    /// <summary>
    ///     Compares two instances for ienquality.
    /// </summary>
    /// <param name="left">The left instance.</param>
    /// <param name="right">The right instance.</param>
    /// <returns>True, if the instances are not equal; false otherwise.</returns>
    public static bool operator !=(Vector2D left, Vector2D right) => !left.Equals(right);

    /// <summary>Converts osuTK.Vector2 to osuTK.Vector2d.</summary>
    /// <param name="v2">The Vector2 to convert.</param>
    /// <returns>The resulting Vector2d.</returns>
    public static explicit operator Vector2D(Vector2 v2) => new(v2.X, v2.Y);

    /// <summary>Converts osuTK.Vector2d to osuTK.Vector2.</summary>
    /// <param name="v2D">The Vector2d to convert.</param>
    /// <returns>The resulting Vector2.</returns>
    public static explicit operator Vector2(Vector2D v2D) => new(v2D.X, v2D.Y);

    private static readonly string listSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

    /// <summary>
    ///     Returns a System.string that represents the current instance.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("({0}{2} {1})", X, Y, listSeparator);
    }

    /// <summary>
    ///     Returns the hashcode for this instance.
    /// </summary>
    /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return X.GetHashCode() * 397 ^ Y.GetHashCode();
        }
    }

    /// <summary>
    ///     Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>True if the instances are equal; false otherwise.</returns>
    public override bool Equals(object obj)
    {
        if (!(obj is Vector2D)) return false;

        return Equals((Vector2D)obj);
    }

    /// <summary>Indicates whether the current vector is equal to another vector.</summary>
    /// <param name="other">A vector to compare with this vector.</param>
    /// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
    public bool Equals(Vector2D other)
    {
        return
            X == other.X && Y == other.Y;
    }
}
