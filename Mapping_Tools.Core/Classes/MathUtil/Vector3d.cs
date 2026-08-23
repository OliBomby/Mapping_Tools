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

/// <summary>
///     Represents a 3D vector using three double-precision floating-point numbers.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector3D : IEquatable<Vector3D>
{
    /// <summary>
    ///     The X component of the Vector3.
    /// </summary>
    public double X;

    /// <summary>
    ///     The Y component of the Vector3.
    /// </summary>
    public double Y;

    /// <summary>
    ///     The Z component of the Vector3.
    /// </summary>
    public double Z;

    /// <summary>
    ///     Constructs a new instance.
    /// </summary>
    /// <param name="value">The value that will initialize this instance.</param>
    public Vector3D(double value)
    {
        X = value;
        Y = value;
        Z = value;
    }

    /// <summary>
    ///     Constructs a new Vector3.
    /// </summary>
    /// <param name="x">The x component of the Vector3.</param>
    /// <param name="y">The y component of the Vector3.</param>
    /// <param name="z">The z component of the Vector3.</param>
    public Vector3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    ///     Constructs a new instance from the given Vector2d.
    /// </summary>
    /// <param name="v">The Vector2d to copy components from.</param>
    public Vector3D(Vector2D v)
    {
        X = v.X;
        Y = v.Y;
        Z = 0.0f;
    }

    /// <summary>
    ///     Constructs a new instance from the given Vector3d.
    /// </summary>
    /// <param name="v">The Vector3d to copy components from.</param>
    public Vector3D(Vector3D v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }

    /// <summary>
    ///     Constructs a new instance from the given Vector4d.
    /// </summary>
    /// <param name="v">The Vector4d to copy components from.</param>
    public Vector3D(Vector4D v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
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

            if (index == 2) return Z;
            throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
        }
        set
        {
            if (index == 0)
                X = value;
            else if (index == 1)
                Y = value;
            else if (index == 2)
                Z = value;
            else
                throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
        }
    }

    /// <summary>
    ///     Gets the length (magnitude) of the vector.
    /// </summary>
    /// <see cref="LengthFast" />
    /// <seealso cref="LengthSquared" />
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    ///     Gets an approximation of the vector length (magnitude).
    /// </summary>
    /// <remarks>
    ///     This property uses an approximation of the square root function to calculate vector magnitude, with
    ///     an upper error bound of 0.001.
    /// </remarks>
    /// <see cref="Length" />
    /// <seealso cref="LengthSquared" />
    public double LengthFast => 1.0 / MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z);

    /// <summary>
    ///     Gets the square of the vector length (magnitude).
    /// </summary>
    /// <remarks>
    ///     This property avoids the costly square root operation required by the Length property. This makes it more suitable
    ///     for comparisons.
    /// </remarks>
    /// <see cref="Length" />
    /// <seealso cref="LengthFast" />
    public double LengthSquared => X * X + Y * Y + Z * Z;

    /// <summary>
    ///     Returns a copy of the Vector3d scaled to unit length.
    /// </summary>
    /// <returns></returns>
    public Vector3D Normalized()
    {
        var v = this;
        v.Normalize();
        return v;
    }

    /// <summary>
    ///     Scales the Vector3d to unit length.
    /// </summary>
    public void Normalize()
    {
        double scale = 1.0 / Length;
        X *= scale;
        Y *= scale;
        Z *= scale;
    }

    /// <summary>
    ///     Scales the Vector3d to approximately unit length.
    /// </summary>
    public void NormalizeFast()
    {
        double scale = MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z);
        X *= scale;
        Y *= scale;
        Z *= scale;
    }

    /// <summary>
    ///     Defines a unit-length Vector3d that points towards the X-axis.
    /// </summary>
    public static readonly Vector3D UnitX = new(1, 0, 0);

    /// <summary>
    ///     Defines a unit-length Vector3d that points towards the Y-axis.
    /// </summary>
    public static readonly Vector3D UnitY = new(0, 1, 0);

    /// <summary>
    ///     Defines a unit-length Vector3d that points towards the Z-axis.
    /// </summary>
    public static readonly Vector3D UnitZ = new(0, 0, 1);

    /// <summary>
    ///     Defines a zero-length Vector3.
    /// </summary>
    public static readonly Vector3D Zero = new(0, 0, 0);

    /// <summary>
    ///     Defines an instance with all components set to 1.
    /// </summary>
    public static readonly Vector3D One = new(1, 1, 1);

    /// <summary>
    ///     Defines the size of the Vector3d struct in bytes.
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf(new Vector3D());

    /// <summary>
    ///     Adds two vectors.
    /// </summary>
    /// <param name="a">Left operand.</param>
    /// <param name="b">Right operand.</param>
    /// <returns>Result of operation.</returns>
    public static Vector3D Add(Vector3D a, Vector3D b)
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
    public static void Add(ref Vector3D a, ref Vector3D b, out Vector3D result)
    {
        result.X = a.X + b.X;
        result.Y = a.Y + b.Y;
        result.Z = a.Z + b.Z;
    }

    /// <summary>
    ///     Subtract one Vector from another
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>Result of subtraction</returns>
    public static Vector3D Subtract(Vector3D a, Vector3D b)
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
    public static void Subtract(ref Vector3D a, ref Vector3D b, out Vector3D result)
    {
        result.X = a.X - b.X;
        result.Y = a.Y - b.Y;
        result.Z = a.Z - b.Z;
    }

    /// <summary>
    ///     Multiplies a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector3D Multiply(Vector3D vector, double scale)
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
    public static void Multiply(ref Vector3D vector, double scale, out Vector3D result)
    {
        result.X = vector.X * scale;
        result.Y = vector.Y * scale;
        result.Z = vector.Z * scale;
    }

    /// <summary>
    ///     Multiplies a vector by the components a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector3D Multiply(Vector3D vector, Vector3D scale)
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
    public static void Multiply(ref Vector3D vector, ref Vector3D scale, out Vector3D result)
    {
        result.X = vector.X * scale.X;
        result.Y = vector.Y * scale.Y;
        result.Z = vector.Z * scale.Z;
    }

    /// <summary>
    ///     Divides a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector3D Divide(Vector3D vector, double scale)
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
    public static void Divide(ref Vector3D vector, double scale, out Vector3D result)
    {
        result.X = vector.X / scale;
        result.Y = vector.Y / scale;
        result.Z = vector.Z / scale;
    }

    /// <summary>
    ///     Divides a vector by the components of a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector3D Divide(Vector3D vector, Vector3D scale)
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
    public static void Divide(ref Vector3D vector, ref Vector3D scale, out Vector3D result)
    {
        result.X = vector.X / scale.X;
        result.Y = vector.Y / scale.Y;
        result.Z = vector.Z / scale.Z;
    }

    /// <summary>
    ///     Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise minimum</returns>
    public static Vector3D ComponentMin(Vector3D a, Vector3D b)
    {
        a.X = a.X < b.X ? a.X : b.X;
        a.Y = a.Y < b.Y ? a.Y : b.Y;
        a.Z = a.Z < b.Z ? a.Z : b.Z;
        return a;
    }

    /// <summary>
    ///     Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise minimum</param>
    public static void ComponentMin(ref Vector3D a, ref Vector3D b, out Vector3D result)
    {
        result.X = a.X < b.X ? a.X : b.X;
        result.Y = a.Y < b.Y ? a.Y : b.Y;
        result.Z = a.Z < b.Z ? a.Z : b.Z;
    }

    /// <summary>
    ///     Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise maximum</returns>
    public static Vector3D ComponentMax(Vector3D a, Vector3D b)
    {
        a.X = a.X > b.X ? a.X : b.X;
        a.Y = a.Y > b.Y ? a.Y : b.Y;
        a.Z = a.Z > b.Z ? a.Z : b.Z;
        return a;
    }

    /// <summary>
    ///     Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise maximum</param>
    public static void ComponentMax(ref Vector3D a, ref Vector3D b, out Vector3D result)
    {
        result.X = a.X > b.X ? a.X : b.X;
        result.Y = a.Y > b.Y ? a.Y : b.Y;
        result.Z = a.Z > b.Z ? a.Z : b.Z;
    }

    /// <summary>
    ///     Returns the Vector3d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector3d</returns>
    public static Vector3D MagnitudeMin(Vector3D left, Vector3D right)
    {
        return left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector3d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <param name="result">The magnitude-wise minimum</param>
    /// <returns>The minimum Vector3d</returns>
    public static void MagnitudeMin(ref Vector3D left, ref Vector3D right, out Vector3D result)
    {
        result = left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector3d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector3d</returns>
    public static Vector3D MagnitudeMax(Vector3D left, Vector3D right)
    {
        return left.LengthSquared >= right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector3d with the maximum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <param name="result">The magnitude-wise maximum</param>
    /// <returns>The maximum Vector3d</returns>
    public static void MagnitudeMax(ref Vector3D left, ref Vector3D right, out Vector3D result)
    {
        result = left.LengthSquared >= right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector3d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector3</returns>
    [Obsolete("Use MagnitudeMin() instead.")]
    public static Vector3D Min(Vector3D left, Vector3D right)
    {
        return left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector3d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector3</returns>
    [Obsolete("Use MagnitudeMax() instead.")]
    public static Vector3D Max(Vector3D left, Vector3D right)
    {
        return left.LengthSquared >= right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Clamp a vector to the given minimum and maximum vectors
    /// </summary>
    /// <param name="vec">Input vector</param>
    /// <param name="min">Minimum vector</param>
    /// <param name="max">Maximum vector</param>
    /// <returns>The clamped vector</returns>
    public static Vector3D Clamp(Vector3D vec, Vector3D min, Vector3D max)
    {
        vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
        vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
        vec.Z = vec.Z < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
        return vec;
    }

    /// <summary>
    ///     Clamp a vector to the given minimum and maximum vectors
    /// </summary>
    /// <param name="vec">Input vector</param>
    /// <param name="min">Minimum vector</param>
    /// <param name="max">Maximum vector</param>
    /// <param name="result">The clamped vector</param>
    public static void Clamp(ref Vector3D vec, ref Vector3D min, ref Vector3D max, out Vector3D result)
    {
        result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
        result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
        result.Z = vec.Z < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
    }

    /// <summary>
    ///     Compute the euclidean distance between two vectors.
    /// </summary>
    /// <param name="vec1">The first vector</param>
    /// <param name="vec2">The second vector</param>
    /// <returns>The distance</returns>
    public static double Distance(Vector3D vec1, Vector3D vec2)
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
    public static void Distance(ref Vector3D vec1, ref Vector3D vec2, out double result)
    {
        result = Math.Sqrt((vec2.X - vec1.X) * (vec2.X - vec1.X) + (vec2.Y - vec1.Y) * (vec2.Y - vec1.Y) + (vec2.Z - vec1.Z) * (vec2.Z - vec1.Z));
    }

    /// <summary>
    ///     Compute the squared euclidean distance between two vectors.
    /// </summary>
    /// <param name="vec1">The first vector</param>
    /// <param name="vec2">The second vector</param>
    /// <returns>The squared distance</returns>
    public static double DistanceSquared(Vector3D vec1, Vector3D vec2)
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
    public static void DistanceSquared(ref Vector3D vec1, ref Vector3D vec2, out double result)
    {
        result = (vec2.X - vec1.X) * (vec2.X - vec1.X) + (vec2.Y - vec1.Y) * (vec2.Y - vec1.Y) + (vec2.Z - vec1.Z) * (vec2.Z - vec1.Z);
    }

    /// <summary>
    ///     Scale a vector to unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <returns>The normalized vector</returns>
    public static Vector3D Normalize(Vector3D vec)
    {
        double scale = 1.0 / vec.Length;
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    /// <summary>
    ///     Scale a vector to unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <param name="result">The normalized vector</param>
    public static void Normalize(ref Vector3D vec, out Vector3D result)
    {
        double scale = 1.0 / vec.Length;
        result.X = vec.X * scale;
        result.Y = vec.Y * scale;
        result.Z = vec.Z * scale;
    }

    /// <summary>
    ///     Scale a vector to approximately unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <returns>The normalized vector</returns>
    public static Vector3D NormalizeFast(Vector3D vec)
    {
        double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    /// <summary>
    ///     Scale a vector to approximately unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <param name="result">The normalized vector</param>
    public static void NormalizeFast(ref Vector3D vec, out Vector3D result)
    {
        double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        result.X = vec.X * scale;
        result.Y = vec.Y * scale;
        result.Z = vec.Z * scale;
    }

    /// <summary>
    ///     Calculate the dot (scalar) product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <returns>The dot product of the two inputs</returns>
    public static double Dot(Vector3D left, Vector3D right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    /// <summary>
    ///     Calculate the dot (scalar) product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <param name="result">The dot product of the two inputs</param>
    public static void Dot(ref Vector3D left, ref Vector3D right, out double result)
    {
        result = left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    /// <summary>
    ///     Caclulate the cross (vector) product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <returns>The cross product of the two inputs</returns>
    public static Vector3D Cross(Vector3D left, Vector3D right)
    {
        Cross(ref left, ref right, out var result);
        return result;
    }

    /// <summary>
    ///     Caclulate the cross (vector) product of two vectors
    /// </summary>
    /// <remarks>
    ///     It is incorrect to call this method passing the same variable for
    ///     <paramref name="result" /> as for <paramref name="left" /> or
    ///     <paramref name="right" />.
    /// </remarks>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <returns>The cross product of the two inputs</returns>
    /// <param name="result">The cross product of the two inputs</param>
    public static void Cross(ref Vector3D left, ref Vector3D right, out Vector3D result)
    {
        result.X = left.Y * right.Z - left.Z * right.Y;
        result.Y = left.Z * right.X - left.X * right.Z;
        result.Z = left.X * right.Y - left.Y * right.X;
    }

    /// <summary>
    ///     Returns a new Vector that is the linear blend of the 2 given Vectors
    /// </summary>
    /// <param name="a">First input vector</param>
    /// <param name="b">Second input vector</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
    public static Vector3D Lerp(Vector3D a, Vector3D b, double blend)
    {
        a.X = blend * (b.X - a.X) + a.X;
        a.Y = blend * (b.Y - a.Y) + a.Y;
        a.Z = blend * (b.Z - a.Z) + a.Z;
        return a;
    }

    /// <summary>
    ///     Returns a new Vector that is the linear blend of the 2 given Vectors
    /// </summary>
    /// <param name="a">First input vector</param>
    /// <param name="b">Second input vector</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
    public static void Lerp(ref Vector3D a, ref Vector3D b, double blend, out Vector3D result)
    {
        result.X = blend * (b.X - a.X) + a.X;
        result.Y = blend * (b.Y - a.Y) + a.Y;
        result.Z = blend * (b.Z - a.Z) + a.Z;
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
    public static Vector3D BaryCentric(Vector3D a, Vector3D b, Vector3D c, double u, double v)
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
    public static void BaryCentric(ref Vector3D a, ref Vector3D b, ref Vector3D c, double u, double v, out Vector3D result)
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
    ///     Transform a direction vector by the given Matrix
    ///     Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
    /// </summary>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <returns>The transformed vector</returns>
    public static Vector3D TransformVector(Vector3D vec, Matrix4D mat)
    {
        TransformVector(ref vec, ref mat, out var result);
        return result;
    }

    /// <summary>
    ///     Transform a direction vector by the given Matrix
    ///     Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
    /// </summary>
    /// <remarks>
    ///     It is incorrect to call this method passing the same variable for
    ///     <paramref name="result" /> as for <paramref name="vec" />.
    /// </remarks>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <param name="result">The transformed vector</param>
    public static void TransformVector(ref Vector3D vec, ref Matrix4D mat, out Vector3D result)
    {
        result.X = vec.X * mat.Row0.X + vec.Y * mat.Row1.X + vec.Z * mat.Row2.X;

        result.Y = vec.X * mat.Row0.Y + vec.Y * mat.Row1.Y + vec.Z * mat.Row2.Y;

        result.Z = vec.X * mat.Row0.Z + vec.Y * mat.Row1.Z + vec.Z * mat.Row2.Z;
    }

    /// <summary>Transform a Normal by the given Matrix</summary>
    /// <remarks>
    ///     This calculates the inverse of the given matrix, use TransformNormalInverse if you
    ///     already have the inverse to avoid this extra calculation
    /// </remarks>
    /// <param name="norm">The normal to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <returns>The transformed normal</returns>
    public static Vector3D TransformNormal(Vector3D norm, Matrix4D mat)
    {
        mat.Invert();
        return TransformNormalInverse(norm, mat);
    }

    /// <summary>Transform a Normal by the given Matrix</summary>
    /// <remarks>
    ///     This calculates the inverse of the given matrix, use TransformNormalInverse if you
    ///     already have the inverse to avoid this extra calculation
    /// </remarks>
    /// <param name="norm">The normal to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <param name="result">The transformed normal</param>
    public static void TransformNormal(ref Vector3D norm, ref Matrix4D mat, out Vector3D result)
    {
        var Inverse = Matrix4D.Invert(mat);
        TransformNormalInverse(ref norm, ref Inverse, out result);
    }

    /// <summary>Transform a Normal by the (transpose of the) given Matrix</summary>
    /// <remarks>
    ///     This version doesn't calculate the inverse matrix.
    ///     Use this version if you already have the inverse of the desired transform to hand
    /// </remarks>
    /// <param name="norm">The normal to transform</param>
    /// <param name="invMat">The inverse of the desired transformation</param>
    /// <returns>The transformed normal</returns>
    public static Vector3D TransformNormalInverse(Vector3D norm, Matrix4D invMat)
    {
        TransformNormalInverse(ref norm, ref invMat, out var result);
        return result;
    }

    /// <summary>Transform a Normal by the (transpose of the) given Matrix</summary>
    /// <remarks>
    ///     This version doesn't calculate the inverse matrix.
    ///     Use this version if you already have the inverse of the desired transform to hand
    /// </remarks>
    /// <param name="norm">The normal to transform</param>
    /// <param name="invMat">The inverse of the desired transformation</param>
    /// <param name="result">The transformed normal</param>
    public static void TransformNormalInverse(ref Vector3D norm, ref Matrix4D invMat, out Vector3D result)
    {
        result.X = norm.X * invMat.Row0.X + norm.Y * invMat.Row0.Y + norm.Z * invMat.Row0.Z;

        result.Y = norm.X * invMat.Row1.X + norm.Y * invMat.Row1.Y + norm.Z * invMat.Row1.Z;

        result.Z = norm.X * invMat.Row2.X + norm.Y * invMat.Row2.Y + norm.Z * invMat.Row2.Z;
    }

    /// <summary>Transform a Position by the given Matrix</summary>
    /// <param name="pos">The position to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <returns>The transformed position</returns>
    public static Vector3D TransformPosition(Vector3D pos, Matrix4D mat)
    {
        TransformPosition(ref pos, ref mat, out var result);
        return result;
    }

    /// <summary>Transform a Position by the given Matrix</summary>
    /// <param name="pos">The position to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <param name="result">The transformed position</param>
    public static void TransformPosition(ref Vector3D pos, ref Matrix4D mat, out Vector3D result)
    {
        result.X = pos.X * mat.Row0.X + pos.Y * mat.Row1.X + pos.Z * mat.Row2.X + mat.Row3.X;

        result.Y = pos.X * mat.Row0.Y + pos.Y * mat.Row1.Y + pos.Z * mat.Row2.Y + mat.Row3.Y;

        result.Z = pos.X * mat.Row0.Z + pos.Y * mat.Row1.Z + pos.Z * mat.Row2.Z + mat.Row3.Z;
    }

    /// <summary>Transform a Vector by the given Matrix</summary>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <returns>The transformed vector</returns>
    public static Vector3D Transform(Vector3D vec, Matrix4D mat)
    {
        Transform(ref vec, ref mat, out var result);
        return result;
    }

    /// <summary>Transform a Vector by the given Matrix</summary>
    /// <remarks>
    ///     It is incorrect to call this method passing the same variable for
    ///     <paramref name="result" /> as for <paramref name="left" /> or
    ///     <paramref name="right" />.
    /// </remarks>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <param name="result">The transformed vector</param>
    public static void Transform(ref Vector3D vec, ref Matrix4D mat, out Vector3D result)
    {
        var v4 = new Vector4D(vec.X, vec.Y, vec.Z, 1.0);
        Vector4D.Transform(ref v4, ref mat, out v4);
        result.X = v4.X;
        result.Y = v4.Y;
        result.Z = v4.Z;
    }

    /// <summary>
    ///     Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector3D Transform(Vector3D vec, Quaterniond quat)
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
    public static void Transform(ref Vector3D vec, ref Quaterniond quat, out Vector3D result)
    {
        // Since vec.W == 0, we can optimize quat * vec * quat^-1 as follows:
        // vec + 2.0 * cross(quat.xyz, cross(quat.xyz, vec) + quat.w * vec)
        var xyz = quat.Xyz;
        Cross(ref xyz, ref vec, out var temp);
        Multiply(ref vec, quat.W, out var temp2);
        Add(ref temp, ref temp2, out temp);
        Cross(ref xyz, ref temp, out temp);
        Multiply(ref temp, 2, out temp);
        Add(ref vec, ref temp, out result);
    }

    /// <summary>
    ///     Transform a Vector3d by the given Matrix, and project the resulting Vector4 back to a Vector3
    /// </summary>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <returns>The transformed vector</returns>
    public static Vector3D TransformPerspective(Vector3D vec, Matrix4D mat)
    {
        TransformPerspective(ref vec, ref mat, out var result);
        return result;
    }

    /// <summary>Transform a Vector3d by the given Matrix, and project the resulting Vector4d back to a Vector3d</summary>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <param name="result">The transformed vector</param>
    public static void TransformPerspective(ref Vector3D vec, ref Matrix4D mat, out Vector3D result)
    {
        var v = new Vector4D(vec.X, vec.Y, vec.Z, 1);
        Vector4D.Transform(ref v, ref mat, out v);
        result.X = v.X / v.W;
        result.Y = v.Y / v.W;
        result.Z = v.Z / v.W;
    }

    /// <summary>
    ///     Calculates the angle (in radians) between two vectors.
    /// </summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <returns>Angle (in radians) between the vectors.</returns>
    /// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
    public static double CalculateAngle(Vector3D first, Vector3D second)
    {
        CalculateAngle(ref first, ref second, out double result);
        return result;
    }

    /// <summary>Calculates the angle (in radians) between two vectors.</summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <param name="result">Angle (in radians) between the vectors.</param>
    /// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
    public static void CalculateAngle(ref Vector3D first, ref Vector3D second, out double result)
    {
        Dot(ref first, ref second, out double temp);
        result = Math.Acos(MathHelper.Clamp(temp / (first.Length * second.Length), -1.0, 1.0));
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the X and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Xy
    {
        get => new(X, Y);
        set
        {
            X = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the X and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Xz
    {
        get => new(X, Z);
        set
        {
            X = value.X;
            Z = value.Y;
        }
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
    ///     Gets or sets an osuTK.Vector2d with the Y and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Yz
    {
        get => new(Y, Z);
        set
        {
            Y = value.X;
            Z = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the Z and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Zx
    {
        get => new(Z, X);
        set
        {
            Z = value.X;
            X = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the Z and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Zy
    {
        get => new(Z, Y);
        set
        {
            Z = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the X, Z, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Xzy
    {
        get => new(X, Z, Y);
        set
        {
            X = value.X;
            Z = value.Y;
            Y = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Y, X, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Yxz
    {
        get => new(Y, X, Z);
        set
        {
            Y = value.X;
            X = value.Y;
            Z = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Y, Z, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Yzx
    {
        get => new(Y, Z, X);
        set
        {
            Y = value.X;
            Z = value.Y;
            X = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Z, X, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Zxy
    {
        get => new(Z, X, Y);
        set
        {
            Z = value.X;
            X = value.Y;
            Y = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Z, Y, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Zyx
    {
        get => new(Z, Y, X);
        set
        {
            Z = value.X;
            Y = value.Y;
            X = value.Z;
        }
    }

    /// <summary>
    ///     Adds two instances.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector3D operator +(Vector3D left, Vector3D right)
    {
        left.X += right.X;
        left.Y += right.Y;
        left.Z += right.Z;
        return left;
    }

    /// <summary>
    ///     Subtracts two instances.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector3D operator -(Vector3D left, Vector3D right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        left.Z -= right.Z;
        return left;
    }

    /// <summary>
    ///     Negates an instance.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector3D operator -(Vector3D vec)
    {
        vec.X = -vec.X;
        vec.Y = -vec.Y;
        vec.Z = -vec.Z;
        return vec;
    }

    /// <summary>
    ///     Multiplies an instance by a scalar.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <param name="scale">The scalar.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector3D operator *(Vector3D vec, double scale)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    /// <summary>
    ///     Multiplies an instance by a scalar.
    /// </summary>
    /// <param name="scale">The scalar.</param>
    /// <param name="vec">The instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector3D operator *(double scale, Vector3D vec)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        return vec;
    }

    /// <summary>
    ///     Component-wise multiplication between the specified instance by a scale vector.
    /// </summary>
    /// <param name="scale">Left operand.</param>
    /// <param name="vec">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    public static Vector3D operator *(Vector3D vec, Vector3D scale)
    {
        vec.X *= scale.X;
        vec.Y *= scale.Y;
        vec.Z *= scale.Z;
        return vec;
    }

    /// <summary>
    ///     Divides an instance by a scalar.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <param name="scale">The scalar.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector3D operator /(Vector3D vec, double scale)
    {
        vec.X /= scale;
        vec.Y /= scale;
        vec.Z /= scale;
        return vec;
    }

    /// <summary>
    ///     Compares two instances for equality.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True, if left equals right; false otherwise.</returns>
    public static bool operator ==(Vector3D left, Vector3D right) => left.Equals(right);

    /// <summary>
    ///     Compares two instances for inequality.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True, if left does not equa lright; false otherwise.</returns>
    public static bool operator !=(Vector3D left, Vector3D right) => !left.Equals(right);

    /// <summary>Converts osuTK.Vector3 to osuTK.Vector3d.</summary>
    /// <param name="v3">The Vector3 to convert.</param>
    /// <returns>The resulting Vector3d.</returns>
    public static explicit operator Vector3D(Vector3 v3) => new(v3.X, v3.Y, v3.Z);

    /// <summary>Converts osuTK.Vector3d to osuTK.Vector3.</summary>
    /// <param name="v3D">The Vector3d to convert.</param>
    /// <returns>The resulting Vector3.</returns>
    public static explicit operator Vector3(Vector3D v3D) => new(v3D.X, v3D.Y, v3D.Z);

    private static readonly string listSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

    /// <summary>
    ///     Returns a System.string that represents the current Vector3.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("({0}{3} {1}{3} {2})", X, Y, Z, listSeparator);
    }

    /// <summary>
    ///     Returns the hashcode for this instance.
    /// </summary>
    /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = X.GetHashCode();
            hashCode = hashCode * 397 ^ Y.GetHashCode();
            hashCode = hashCode * 397 ^ Z.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    ///     Indicates whether this instance and a specified object are equal.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>True if the instances are equal; false otherwise.</returns>
    public override bool Equals(object obj)
    {
        if (!(obj is Vector3D)) return false;

        return Equals((Vector3D)obj);
    }

    /// <summary>Indicates whether the current vector is equal to another vector.</summary>
    /// <param name="other">A vector to compare with this vector.</param>
    /// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
    public bool Equals(Vector3D other)
    {
        return
            X == other.X && Y == other.Y && Z == other.Z;
    }
}
