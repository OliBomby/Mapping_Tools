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

namespace Mapping_Tools.Core.MathUtil;

/// <summary>Represents a 4D vector using four double-precision floating-point numbers.</summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Vector4D : IEquatable<Vector4D>
{
    /// <summary>
    ///     The X component of the Vector4d.
    /// </summary>
    public double X;

    /// <summary>
    ///     The Y component of the Vector4d.
    /// </summary>
    public double Y;

    /// <summary>
    ///     The Z component of the Vector4d.
    /// </summary>
    public double Z;

    /// <summary>
    ///     The W component of the Vector4d.
    /// </summary>
    public double W;

    /// <summary>
    ///     Defines a unit-length Vector4d that points towards the X-axis.
    /// </summary>
    public static readonly Vector4D UnitX = new(1, 0, 0, 0);

    /// <summary>
    ///     Defines a unit-length Vector4d that points towards the Y-axis.
    /// </summary>
    public static readonly Vector4D UnitY = new(0, 1, 0, 0);

    /// <summary>
    ///     Defines a unit-length Vector4d that points towards the Z-axis.
    /// </summary>
    public static readonly Vector4D UnitZ = new(0, 0, 1, 0);

    /// <summary>
    ///     Defines a unit-length Vector4d that points towards the W-axis.
    /// </summary>
    public static readonly Vector4D UnitW = new(0, 0, 0, 1);

    /// <summary>
    ///     Defines a zero-length Vector4d.
    /// </summary>
    public static readonly Vector4D Zero = new(0, 0, 0, 0);

    /// <summary>
    ///     Defines an instance with all components set to 1.
    /// </summary>
    public static readonly Vector4D One = new(1, 1, 1, 1);

    /// <summary>
    ///     Defines the size of the Vector4d struct in bytes.
    /// </summary>
    public static readonly int SizeInBytes = Marshal.SizeOf(new Vector4D());

    /// <summary>
    ///     Constructs a new instance.
    /// </summary>
    /// <param name="value">The value that will initialize this instance.</param>
    public Vector4D(double value)
    {
        X = value;
        Y = value;
        Z = value;
        W = value;
    }

    /// <summary>
    ///     Constructs a new Vector4d.
    /// </summary>
    /// <param name="x">The x component of the Vector4d.</param>
    /// <param name="y">The y component of the Vector4d.</param>
    /// <param name="z">The z component of the Vector4d.</param>
    /// <param name="w">The w component of the Vector4d.</param>
    public Vector4D(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    ///     Constructs a new Vector4d from the given Vector2d.
    /// </summary>
    /// <param name="v">The Vector2d to copy components from.</param>
    public Vector4D(Vector2D v)
    {
        X = v.X;
        Y = v.Y;
        Z = 0.0f;
        W = 0.0f;
    }

    /// <summary>
    ///     Constructs a new Vector4d from the given Vector3d.
    ///     The w component is initialized to 0.
    /// </summary>
    /// <param name="v">The Vector3d to copy components from.</param>
    /// <remarks>
    ///     <seealso cref="Vector4D(Mapping_Tools.Core.MathUtil.Vector3D,double)" />
    /// </remarks>
    public Vector4D(Vector3D v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
        W = 0.0f;
    }

    /// <summary>
    ///     Constructs a new Vector4d from the specified Vector3d and w component.
    /// </summary>
    /// <param name="v">The Vector3d to copy components from.</param>
    /// <param name="w">The w component of the new Vector4.</param>
    public Vector4D(Vector3D v, double w)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
        W = w;
    }

    /// <summary>
    ///     Constructs a new Vector4d from the given Vector4d.
    /// </summary>
    /// <param name="v">The Vector4d to copy components from.</param>
    public Vector4D(Vector4D v)
    {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
        W = v.W;
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

            if (index == 3) return W;
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
            else if (index == 3)
                W = value;
            else
                throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
        }
    }

    /// <summary>
    ///     Gets the length (magnitude) of the vector.
    /// </summary>
    /// <see cref="LengthFast" />
    /// <seealso cref="LengthSquared" />
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

    /// <summary>
    ///     Gets an approximation of the vector length (magnitude).
    /// </summary>
    /// <remarks>
    ///     This property uses an approximation of the square root function to calculate vector magnitude, with
    ///     an upper error bound of 0.001.
    /// </remarks>
    /// <see cref="Length" />
    /// <seealso cref="LengthSquared" />
    public double LengthFast => 1.0 / MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z + W * W);

    /// <summary>
    ///     Gets the square of the vector length (magnitude).
    /// </summary>
    /// <remarks>
    ///     This property avoids the costly square root operation required by the Length property. This makes it more suitable
    ///     for comparisons.
    /// </remarks>
    /// <see cref="Length" />
    public double LengthSquared => X * X + Y * Y + Z * Z + W * W;

    /// <summary>
    ///     Returns a copy of the Vector4d scaled to unit length.
    /// </summary>
    public Vector4D Normalized()
    {
        var v = this;
        v.Normalize();
        return v;
    }

    /// <summary>
    ///     Scales the Vector4d to unit length.
    /// </summary>
    public void Normalize()
    {
        double scale = 1.0 / Length;
        X *= scale;
        Y *= scale;
        Z *= scale;
        W *= scale;
    }

    /// <summary>
    ///     Scales the Vector4d to approximately unit length.
    /// </summary>
    public void NormalizeFast()
    {
        double scale = MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z + W * W);
        X *= scale;
        Y *= scale;
        Z *= scale;
        W *= scale;
    }

    /// <summary>
    ///     Adds two vectors.
    /// </summary>
    /// <param name="a">Left operand.</param>
    /// <param name="b">Right operand.</param>
    /// <returns>Result of operation.</returns>
    public static Vector4D Add(Vector4D a, Vector4D b)
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
    public static void Add(ref Vector4D a, ref Vector4D b, out Vector4D result)
    {
        result.X = a.X + b.X;
        result.Y = a.Y + b.Y;
        result.Z = a.Z + b.Z;
        result.W = a.W + b.W;
    }

    /// <summary>
    ///     Subtract one Vector from another
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>Result of subtraction</returns>
    public static Vector4D Subtract(Vector4D a, Vector4D b)
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
    public static void Subtract(ref Vector4D a, ref Vector4D b, out Vector4D result)
    {
        result.X = a.X - b.X;
        result.Y = a.Y - b.Y;
        result.Z = a.Z - b.Z;
        result.W = a.W - b.W;
    }

    /// <summary>
    ///     Multiplies a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector4D Multiply(Vector4D vector, double scale)
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
    public static void Multiply(ref Vector4D vector, double scale, out Vector4D result)
    {
        result.X = vector.X * scale;
        result.Y = vector.Y * scale;
        result.Z = vector.Z * scale;
        result.W = vector.W * scale;
    }

    /// <summary>
    ///     Multiplies a vector by the components a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector4D Multiply(Vector4D vector, Vector4D scale)
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
    public static void Multiply(ref Vector4D vector, ref Vector4D scale, out Vector4D result)
    {
        result.X = vector.X * scale.X;
        result.Y = vector.Y * scale.Y;
        result.Z = vector.Z * scale.Z;
        result.W = vector.W * scale.W;
    }

    /// <summary>
    ///     Divides a vector by a scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector4D Divide(Vector4D vector, double scale)
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
    public static void Divide(ref Vector4D vector, double scale, out Vector4D result)
    {
        result.X = vector.X / scale;
        result.Y = vector.Y / scale;
        result.Z = vector.Z / scale;
        result.W = vector.W / scale;
    }

    /// <summary>
    ///     Divides a vector by the components of a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    public static Vector4D Divide(Vector4D vector, Vector4D scale)
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
    public static void Divide(ref Vector4D vector, ref Vector4D scale, out Vector4D result)
    {
        result.X = vector.X / scale.X;
        result.Y = vector.Y / scale.Y;
        result.Z = vector.Z / scale.Z;
        result.W = vector.W / scale.W;
    }

    /// <summary>
    ///     Calculate the component-wise minimum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise minimum</returns>
    [Obsolete("Use ComponentMin() instead.")]
    public static Vector4D Min(Vector4D a, Vector4D b)
    {
        a.X = a.X < b.X ? a.X : b.X;
        a.Y = a.Y < b.Y ? a.Y : b.Y;
        a.Z = a.Z < b.Z ? a.Z : b.Z;
        a.W = a.W < b.W ? a.W : b.W;
        return a;
    }

    /// <summary>
    ///     Calculate the component-wise minimum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise minimum</param>
    [Obsolete("Use ComponentMin() instead.")]
    public static void Min(ref Vector4D a, ref Vector4D b, out Vector4D result)
    {
        result.X = a.X < b.X ? a.X : b.X;
        result.Y = a.Y < b.Y ? a.Y : b.Y;
        result.Z = a.Z < b.Z ? a.Z : b.Z;
        result.W = a.W < b.W ? a.W : b.W;
    }

    /// <summary>
    ///     Calculate the component-wise maximum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise maximum</returns>
    [Obsolete("Use ComponentMax() instead.")]
    public static Vector4D Max(Vector4D a, Vector4D b)
    {
        a.X = a.X > b.X ? a.X : b.X;
        a.Y = a.Y > b.Y ? a.Y : b.Y;
        a.Z = a.Z > b.Z ? a.Z : b.Z;
        a.W = a.W > b.W ? a.W : b.W;
        return a;
    }

    /// <summary>
    ///     Calculate the component-wise maximum of two vectors
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise maximum</param>
    [Obsolete("Use ComponentMax() instead.")]
    public static void Max(ref Vector4D a, ref Vector4D b, out Vector4D result)
    {
        result.X = a.X > b.X ? a.X : b.X;
        result.Y = a.Y > b.Y ? a.Y : b.Y;
        result.Z = a.Z > b.Z ? a.Z : b.Z;
        result.W = a.W > b.W ? a.W : b.W;
    }

    /// <summary>
    ///     Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise minimum</returns>
    public static Vector4D ComponentMin(Vector4D a, Vector4D b)
    {
        a.X = a.X < b.X ? a.X : b.X;
        a.Y = a.Y < b.Y ? a.Y : b.Y;
        a.Z = a.Z < b.Z ? a.Z : b.Z;
        a.W = a.W < b.W ? a.W : b.W;
        return a;
    }

    /// <summary>
    ///     Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise minimum</param>
    public static void ComponentMin(ref Vector4D a, ref Vector4D b, out Vector4D result)
    {
        result.X = a.X < b.X ? a.X : b.X;
        result.Y = a.Y < b.Y ? a.Y : b.Y;
        result.Z = a.Z < b.Z ? a.Z : b.Z;
        result.W = a.W < b.W ? a.W : b.W;
    }

    /// <summary>
    ///     Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <returns>The component-wise maximum</returns>
    public static Vector4D ComponentMax(Vector4D a, Vector4D b)
    {
        a.X = a.X > b.X ? a.X : b.X;
        a.Y = a.Y > b.Y ? a.Y : b.Y;
        a.Z = a.Z > b.Z ? a.Z : b.Z;
        a.W = a.W > b.W ? a.W : b.W;
        return a;
    }

    /// <summary>
    ///     Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand</param>
    /// <param name="b">Second operand</param>
    /// <param name="result">The component-wise maximum</param>
    public static void ComponentMax(ref Vector4D a, ref Vector4D b, out Vector4D result)
    {
        result.X = a.X > b.X ? a.X : b.X;
        result.Y = a.Y > b.Y ? a.Y : b.Y;
        result.Z = a.Z > b.Z ? a.Z : b.Z;
        result.W = a.W > b.W ? a.W : b.W;
    }

    /// <summary>
    ///     Returns the Vector4d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector4d</returns>
    public static Vector4D MagnitudeMin(Vector4D left, Vector4D right)
    {
        return left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector4d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <param name="result">The magnitude-wise minimum</param>
    /// <returns>The minimum Vector4d</returns>
    public static void MagnitudeMin(ref Vector4D left, ref Vector4D right, out Vector4D result)
    {
        result = left.LengthSquared < right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector4d with the minimum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>The minimum Vector4d</returns>
    public static Vector4D MagnitudeMax(Vector4D left, Vector4D right)
    {
        return left.LengthSquared >= right.LengthSquared ? left : right;
    }

    /// <summary>
    ///     Returns the Vector4d with the maximum magnitude
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <param name="result">The magnitude-wise maximum</param>
    /// <returns>The maximum Vector4d</returns>
    public static void MagnitudeMax(ref Vector4D left, ref Vector4D right, out Vector4D result)
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
    public static Vector4D Clamp(Vector4D vec, Vector4D min, Vector4D max)
    {
        vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
        vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
        vec.Z = vec.X < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
        vec.W = vec.Y < min.W ? min.W : vec.W > max.W ? max.W : vec.W;
        return vec;
    }

    /// <summary>
    ///     Clamp a vector to the given minimum and maximum vectors
    /// </summary>
    /// <param name="vec">Input vector</param>
    /// <param name="min">Minimum vector</param>
    /// <param name="max">Maximum vector</param>
    /// <param name="result">The clamped vector</param>
    public static void Clamp(ref Vector4D vec, ref Vector4D min, ref Vector4D max, out Vector4D result)
    {
        result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
        result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
        result.Z = vec.X < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
        result.W = vec.Y < min.W ? min.W : vec.W > max.W ? max.W : vec.W;
    }

    /// <summary>
    ///     Scale a vector to unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <returns>The normalized vector</returns>
    public static Vector4D Normalize(Vector4D vec)
    {
        double scale = 1.0 / vec.Length;
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        vec.W *= scale;
        return vec;
    }

    /// <summary>
    ///     Scale a vector to unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <param name="result">The normalized vector</param>
    public static void Normalize(ref Vector4D vec, out Vector4D result)
    {
        double scale = 1.0 / vec.Length;
        result.X = vec.X * scale;
        result.Y = vec.Y * scale;
        result.Z = vec.Z * scale;
        result.W = vec.W * scale;
    }

    /// <summary>
    ///     Scale a vector to approximately unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <returns>The normalized vector</returns>
    public static Vector4D NormalizeFast(Vector4D vec)
    {
        double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        vec.W *= scale;
        return vec;
    }

    /// <summary>
    ///     Scale a vector to approximately unit length
    /// </summary>
    /// <param name="vec">The input vector</param>
    /// <param name="result">The normalized vector</param>
    public static void NormalizeFast(ref Vector4D vec, out Vector4D result)
    {
        double scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);
        result.X = vec.X * scale;
        result.Y = vec.Y * scale;
        result.Z = vec.Z * scale;
        result.W = vec.W * scale;
    }

    /// <summary>
    ///     Calculate the dot product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <returns>The dot product of the two inputs</returns>
    public static double Dot(Vector4D left, Vector4D right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
    }

    /// <summary>
    ///     Calculate the dot product of two vectors
    /// </summary>
    /// <param name="left">First operand</param>
    /// <param name="right">Second operand</param>
    /// <param name="result">The dot product of the two inputs</param>
    public static void Dot(ref Vector4D left, ref Vector4D right, out double result)
    {
        result = left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
    }

    /// <summary>
    ///     Returns a new Vector that is the linear blend of the 2 given Vectors
    /// </summary>
    /// <param name="a">First input vector</param>
    /// <param name="b">Second input vector</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
    public static Vector4D Lerp(Vector4D a, Vector4D b, double blend)
    {
        a.X = blend * (b.X - a.X) + a.X;
        a.Y = blend * (b.Y - a.Y) + a.Y;
        a.Z = blend * (b.Z - a.Z) + a.Z;
        a.W = blend * (b.W - a.W) + a.W;
        return a;
    }

    /// <summary>
    ///     Returns a new Vector that is the linear blend of the 2 given Vectors
    /// </summary>
    /// <param name="a">First input vector</param>
    /// <param name="b">Second input vector</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
    public static void Lerp(ref Vector4D a, ref Vector4D b, double blend, out Vector4D result)
    {
        result.X = blend * (b.X - a.X) + a.X;
        result.Y = blend * (b.Y - a.Y) + a.Y;
        result.Z = blend * (b.Z - a.Z) + a.Z;
        result.W = blend * (b.W - a.W) + a.W;
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
    public static Vector4D BaryCentric(Vector4D a, Vector4D b, Vector4D c, double u, double v)
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
    public static void BaryCentric(ref Vector4D a, ref Vector4D b, ref Vector4D c, double u, double v, out Vector4D result)
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

    /// <summary>Transform a Vector by the given Matrix</summary>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <returns>The transformed vector</returns>
    public static Vector4D Transform(Vector4D vec, Matrix4D mat)
    {
        Transform(ref vec, ref mat, out var result);
        return result;
    }

    /// <summary>Transform a Vector by the given Matrix</summary>
    /// <param name="vec">The vector to transform</param>
    /// <param name="mat">The desired transformation</param>
    /// <param name="result">The transformed vector</param>
    public static void Transform(ref Vector4D vec, ref Matrix4D mat, out Vector4D result)
    {
        result = new Vector4D(
            vec.X * mat.Row0.X + vec.Y * mat.Row1.X + vec.Z * mat.Row2.X + vec.W * mat.Row3.X,
            vec.X * mat.Row0.Y + vec.Y * mat.Row1.Y + vec.Z * mat.Row2.Y + vec.W * mat.Row3.Y,
            vec.X * mat.Row0.Z + vec.Y * mat.Row1.Z + vec.Z * mat.Row2.Z + vec.W * mat.Row3.Z,
            vec.X * mat.Row0.W + vec.Y * mat.Row1.W + vec.Z * mat.Row2.W + vec.W * mat.Row3.W);
    }

    /// <summary>
    ///     Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector4D Transform(Vector4D vec, Quaterniond quat)
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
    public static void Transform(ref Vector4D vec, ref Quaterniond quat, out Vector4D result)
    {
        var v = new Quaterniond(vec.X, vec.Y, vec.Z, vec.W);
        Quaterniond.Invert(ref quat, out var i);
        Quaterniond.Multiply(ref quat, ref v, out var t);
        Quaterniond.Multiply(ref t, ref i, out v);

        result.X = v.X;
        result.Y = v.Y;
        result.Z = v.Z;
        result.W = v.W;
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
    ///     Gets or sets an osuTK.Vector2d with the X and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Xw
    {
        get => new(X, W);
        set
        {
            X = value.X;
            W = value.Y;
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
    ///     Gets or sets an osuTK.Vector2d with the Y and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Yw
    {
        get => new(Y, W);
        set
        {
            Y = value.X;
            W = value.Y;
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
    ///     Gets an osuTK.Vector2d with the Z and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Zw
    {
        get => new(Z, W);
        set
        {
            Z = value.X;
            W = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the W and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Wx
    {
        get => new(W, X);
        set
        {
            W = value.X;
            X = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the W and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Wy
    {
        get => new(W, Y);
        set
        {
            W = value.X;
            Y = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector2d with the W and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector2D Wz
    {
        get => new(W, Z);
        set
        {
            W = value.X;
            Z = value.Y;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the X, Y, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Xyz
    {
        get => new(X, Y, Z);
        set
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the X, Y, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Xyw
    {
        get => new(X, Y, W);
        set
        {
            X = value.X;
            Y = value.Y;
            W = value.Z;
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
    ///     Gets or sets an osuTK.Vector3d with the X, Z, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Xzw
    {
        get => new(X, Z, W);
        set
        {
            X = value.X;
            Z = value.Y;
            W = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the X, W, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Xwy
    {
        get => new(X, W, Y);
        set
        {
            X = value.X;
            W = value.Y;
            Y = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the X, W, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Xwz
    {
        get => new(X, W, Z);
        set
        {
            X = value.X;
            W = value.Y;
            Z = value.Z;
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
    ///     Gets or sets an osuTK.Vector3d with the Y, X, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Yxw
    {
        get => new(Y, X, W);
        set
        {
            Y = value.X;
            X = value.Y;
            W = value.Z;
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
    ///     Gets or sets an osuTK.Vector3d with the Y, Z, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Yzw
    {
        get => new(Y, Z, W);
        set
        {
            Y = value.X;
            Z = value.Y;
            W = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Y, W, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Ywx
    {
        get => new(Y, W, X);
        set
        {
            Y = value.X;
            W = value.Y;
            X = value.Z;
        }
    }

    /// <summary>
    ///     Gets an osuTK.Vector3d with the Y, W, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Ywz
    {
        get => new(Y, W, Z);
        set
        {
            Y = value.X;
            W = value.Y;
            Z = value.Z;
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
    ///     Gets or sets an osuTK.Vector3d with the Z, X, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Zxw
    {
        get => new(Z, X, W);
        set
        {
            Z = value.X;
            X = value.Y;
            W = value.Z;
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
    ///     Gets or sets an osuTK.Vector3d with the Z, Y, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Zyw
    {
        get => new(Z, Y, W);
        set
        {
            Z = value.X;
            Y = value.Y;
            W = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Z, W, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Zwx
    {
        get => new(Z, W, X);
        set
        {
            Z = value.X;
            W = value.Y;
            X = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the Z, W, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Zwy
    {
        get => new(Z, W, Y);
        set
        {
            Z = value.X;
            W = value.Y;
            Y = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the W, X, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Wxy
    {
        get => new(W, X, Y);
        set
        {
            W = value.X;
            X = value.Y;
            Y = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the W, X, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Wxz
    {
        get => new(W, X, Z);
        set
        {
            W = value.X;
            X = value.Y;
            Z = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the W, Y, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Wyx
    {
        get => new(W, Y, X);
        set
        {
            W = value.X;
            Y = value.Y;
            X = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the W, Y, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Wyz
    {
        get => new(W, Y, Z);
        set
        {
            W = value.X;
            Y = value.Y;
            Z = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the W, Z, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Wzx
    {
        get => new(W, Z, X);
        set
        {
            W = value.X;
            Z = value.Y;
            X = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector3d with the W, Z, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector3D Wzy
    {
        get => new(W, Z, Y);
        set
        {
            W = value.X;
            Z = value.Y;
            Y = value.Z;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the X, Y, W, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Xywz
    {
        get => new(X, Y, W, Z);
        set
        {
            X = value.X;
            Y = value.Y;
            W = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the X, Z, Y, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Xzyw
    {
        get => new(X, Z, Y, W);
        set
        {
            X = value.X;
            Z = value.Y;
            Y = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the X, Z, W, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Xzwy
    {
        get => new(X, Z, W, Y);
        set
        {
            X = value.X;
            Z = value.Y;
            W = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the X, W, Y, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Xwyz
    {
        get => new(X, W, Y, Z);
        set
        {
            X = value.X;
            W = value.Y;
            Y = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the X, W, Z, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Xwzy
    {
        get => new(X, W, Z, Y);
        set
        {
            X = value.X;
            W = value.Y;
            Z = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Y, X, Z, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Yxzw
    {
        get => new(Y, X, Z, W);
        set
        {
            Y = value.X;
            X = value.Y;
            Z = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Y, X, W, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Yxwz
    {
        get => new(Y, X, W, Z);
        set
        {
            Y = value.X;
            X = value.Y;
            W = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets an osuTK.Vector4d with the Y, Y, Z, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Yyzw
    {
        get => new(Y, Y, Z, W);
        set
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Gets an osuTK.Vector4d with the Y, Y, W, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Yywz
    {
        get => new(Y, Y, W, Z);
        set
        {
            X = value.X;
            Y = value.Y;
            W = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Y, Z, X, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Yzxw
    {
        get => new(Y, Z, X, W);
        set
        {
            Y = value.X;
            Z = value.Y;
            X = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Y, Z, W, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Yzwx
    {
        get => new(Y, Z, W, X);
        set
        {
            Y = value.X;
            Z = value.Y;
            W = value.Z;
            X = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Y, W, X, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Ywxz
    {
        get => new(Y, W, X, Z);
        set
        {
            Y = value.X;
            W = value.Y;
            X = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Y, W, Z, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Ywzx
    {
        get => new(Y, W, Z, X);
        set
        {
            Y = value.X;
            W = value.Y;
            Z = value.Z;
            X = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Z, X, Y, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zxyw
    {
        get => new(Z, X, Y, W);
        set
        {
            Z = value.X;
            X = value.Y;
            Y = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Z, X, W, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zxwy
    {
        get => new(Z, X, W, Y);
        set
        {
            Z = value.X;
            X = value.Y;
            W = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Z, Y, X, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zyxw
    {
        get => new(Z, Y, X, W);
        set
        {
            Z = value.X;
            Y = value.Y;
            X = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Z, Y, W, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zywx
    {
        get => new(Z, Y, W, X);
        set
        {
            Z = value.X;
            Y = value.Y;
            W = value.Z;
            X = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Z, W, X, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zwxy
    {
        get => new(Z, W, X, Y);
        set
        {
            Z = value.X;
            W = value.Y;
            X = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the Z, W, Y, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zwyx
    {
        get => new(Z, W, Y, X);
        set
        {
            Z = value.X;
            W = value.Y;
            Y = value.Z;
            X = value.W;
        }
    }

    /// <summary>
    ///     Gets an osuTK.Vector4d with the Z, W, Z, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Zwzy
    {
        get => new(Z, W, Z, Y);
        set
        {
            X = value.X;
            W = value.Y;
            Z = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the W, X, Y, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wxyz
    {
        get => new(W, X, Y, Z);
        set
        {
            W = value.X;
            X = value.Y;
            Y = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the W, X, Z, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wxzy
    {
        get => new(W, X, Z, Y);
        set
        {
            W = value.X;
            X = value.Y;
            Z = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the W, Y, X, and Z components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wyxz
    {
        get => new(W, Y, X, Z);
        set
        {
            W = value.X;
            Y = value.Y;
            X = value.Z;
            Z = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the W, Y, Z, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wyzx
    {
        get => new(W, Y, Z, X);
        set
        {
            W = value.X;
            Y = value.Y;
            Z = value.Z;
            X = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the W, Z, X, and Y components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wzxy
    {
        get => new(W, Z, X, Y);
        set
        {
            W = value.X;
            Z = value.Y;
            X = value.Z;
            Y = value.W;
        }
    }

    /// <summary>
    ///     Gets or sets an osuTK.Vector4d with the W, Z, Y, and X components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wzyx
    {
        get => new(W, Z, Y, X);
        set
        {
            W = value.X;
            Z = value.Y;
            Y = value.Z;
            X = value.W;
        }
    }

    /// <summary>
    ///     Gets an osuTK.Vector4d with the W, Z, Y, and W components of this instance.
    /// </summary>
    [XmlIgnore]
    public Vector4D Wzyw
    {
        get => new(W, Z, Y, W);
        set
        {
            X = value.X;
            Z = value.Y;
            Y = value.Z;
            W = value.W;
        }
    }

    /// <summary>
    ///     Adds two instances.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector4D operator +(Vector4D left, Vector4D right)
    {
        left.X += right.X;
        left.Y += right.Y;
        left.Z += right.Z;
        left.W += right.W;
        return left;
    }

    /// <summary>
    ///     Subtracts two instances.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector4D operator -(Vector4D left, Vector4D right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        left.Z -= right.Z;
        left.W -= right.W;
        return left;
    }

    /// <summary>
    ///     Negates an instance.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector4D operator -(Vector4D vec)
    {
        vec.X = -vec.X;
        vec.Y = -vec.Y;
        vec.Z = -vec.Z;
        vec.W = -vec.W;
        return vec;
    }

    /// <summary>
    ///     Multiplies an instance by a scalar.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <param name="scale">The scalar.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector4D operator *(Vector4D vec, double scale)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        vec.W *= scale;
        return vec;
    }

    /// <summary>
    ///     Multiplies an instance by a scalar.
    /// </summary>
    /// <param name="scale">The scalar.</param>
    /// <param name="vec">The instance.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector4D operator *(double scale, Vector4D vec)
    {
        vec.X *= scale;
        vec.Y *= scale;
        vec.Z *= scale;
        vec.W *= scale;
        return vec;
    }

    /// <summary>
    ///     Component-wise multiplication between the specified instance by a scale vector.
    /// </summary>
    /// <param name="scale">Left operand.</param>
    /// <param name="vec">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    public static Vector4D operator *(Vector4D vec, Vector4D scale)
    {
        vec.X *= scale.X;
        vec.Y *= scale.Y;
        vec.Z *= scale.Z;
        vec.W *= scale.W;
        return vec;
    }

    /// <summary>
    ///     Divides an instance by a scalar.
    /// </summary>
    /// <param name="vec">The instance.</param>
    /// <param name="scale">The scalar.</param>
    /// <returns>The result of the calculation.</returns>
    public static Vector4D operator /(Vector4D vec, double scale)
    {
        vec.X /= scale;
        vec.Y /= scale;
        vec.Z /= scale;
        vec.W /= scale;
        return vec;
    }

    /// <summary>
    ///     Compares two instances for equality.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True, if left equals right; false otherwise.</returns>
    public static bool operator ==(Vector4D left, Vector4D right) => left.Equals(right);

    /// <summary>
    ///     Compares two instances for inequality.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True, if left does not equa lright; false otherwise.</returns>
    public static bool operator !=(Vector4D left, Vector4D right) => !left.Equals(right);

    /// <summary>
    ///     Returns a pointer to the first element of the specified instance.
    /// </summary>
    /// <param name="v">The instance.</param>
    /// <returns>A pointer to the first element of v.</returns>
    public static unsafe explicit operator double*(Vector4D v) => &v.X;

    /// <summary>
    ///     Returns a pointer to the first element of the specified instance.
    /// </summary>
    /// <param name="v">The instance.</param>
    /// <returns>A pointer to the first element of v.</returns>
    public static explicit operator IntPtr(Vector4D v)
    {
        unsafe
        {
            return (IntPtr)(&v.X);
        }
    }

    /// <summary>Converts osuTK.Vector4 to osuTK.Vector4d.</summary>
    /// <param name="v4">The Vector4 to convert.</param>
    /// <returns>The resulting Vector4d.</returns>
    public static explicit operator Vector4D(Vector4 v4) => new(v4.X, v4.Y, v4.Z, v4.W);

    /// <summary>Converts osuTK.Vector4d to osuTK.Vector4.</summary>
    /// <param name="v4D">The Vector4d to convert.</param>
    /// <returns>The resulting Vector4.</returns>
    public static explicit operator Vector4(Vector4D v4D) => new(v4D.X, v4D.Y, v4D.Z, v4D.W);

    private static readonly string listSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

    /// <summary>
    ///     Returns a System.string that represents the current Vector4d.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("({0}{4} {1}{4} {2}{4} {3})", X, Y, Z, W, listSeparator);
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
            hashCode = hashCode * 397 ^ W.GetHashCode();
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
        if (!(obj is Vector4D)) return false;

        return Equals((Vector4D)obj);
    }

    /// <summary>Indicates whether the current vector is equal to another vector.</summary>
    /// <param name="other">A vector to compare with this vector.</param>
    /// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
    public bool Equals(Vector4D other)
    {
        return
            X == other.X && Y == other.Y && Z == other.Z && W == other.W;
    }
}
