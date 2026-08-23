// Copyright (c) Open Toolkit library.
// This file is subject to the terms and conditions defined in
// file 'License.txt', which is part of this source code package.

using System.Globalization;
using System.Runtime.InteropServices;

namespace Mapping_Tools.Core.Classes.MathUtil;

/// <summary>
///     Defines a 2d box (rectangle).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Box2D : IEquatable<Box2D>
{
    /// <summary>
    ///     The left boundary of the structure.
    /// </summary>
    public double Left;

    /// <summary>
    ///     The right boundary of the structure.
    /// </summary>
    public double Right;

    /// <summary>
    ///     The top boundary of the structure.
    /// </summary>
    public double Top;

    /// <summary>
    ///     The bottom boundary of the structure.
    /// </summary>
    public double Bottom;

    /// <summary>
    ///     Constructs a new Box2d with the specified dimensions.
    /// </summary>
    /// <param name="topLeft">An osuTK.Vector2d describing the top-left corner of the Box2d.</param>
    /// <param name="bottomRight">An osuTK.Vector2d describing the bottom-right corner of the Box2d.</param>
    public Box2D(Vector2D topLeft, Vector2D bottomRight)
    {
        Left = topLeft.X;
        Top = topLeft.Y;
        Right = bottomRight.X;
        Bottom = bottomRight.Y;
    }

    /// <summary>
    ///     Constructs a new Box2d with the specified dimensions.
    /// </summary>
    /// <param name="left">The position of the left boundary.</param>
    /// <param name="top">The position of the top boundary.</param>
    /// <param name="right">The position of the right boundary.</param>
    /// <param name="bottom">The position of the bottom boundary.</param>
    public Box2D(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    /// <summary>
    ///     Creates a new Box2d with the specified dimensions.
    /// </summary>
    /// <param name="top">The position of the top boundary.</param>
    /// <param name="left">The position of the left boundary.</param>
    /// <param name="right">The position of the right boundary.</param>
    /// <param name="bottom">The position of the bottom boundary.</param>
    /// <returns>A new osuTK.Box2d with the specfied dimensions.</returns>
    public static Box2D FromTlrb(double top, double left, double right, double bottom)
    {
        return new Box2D(left, top, right, bottom);
    }

    /// <summary>
    ///     Creates a new Box2d with the specified dimensions.
    /// </summary>
    /// <param name="top">The position of the top boundary.</param>
    /// <param name="left">The position of the left boundary.</param>
    /// <param name="width">The width of the box.</param>
    /// <param name="height">The height of the box.</param>
    /// <returns>A new osuTK.Box2d with the specfied dimensions.</returns>
    public static Box2D FromDimensions(double left, double top, double width, double height)
    {
        return new Box2D(left, top, left + width, top + height);
    }

    /// <summary>
    ///     Creates a new Box2d with the specified dimensions.
    /// </summary>
    /// <param name="position">The position of the top left corner.</param>
    /// <param name="size">The size of the box.</param>
    /// <returns>A new osuTK.Box2d with the specfied dimensions.</returns>
    public static Box2D FromDimensions(Vector2D position, Vector2D size)
    {
        return FromDimensions(position.X, position.Y, size.X, size.Y);
    }

    /// <summary>
    ///     Gets a double describing the width of the Box2d structure.
    /// </summary>
    public double Width => Math.Abs(Right - Left);

    /// <summary>
    ///     Gets a double describing the height of the Box2d structure.
    /// </summary>
    public double Height => Math.Abs(Bottom - Top);

    /// <summary>
    ///     Returns whether the box contains the specified point on the closed region described by this Box2.
    /// </summary>
    /// <param name="point">The point to query.</param>
    /// <returns>Whether this box contains the point.</returns>
    public bool Contains(Vector2D point)
    {
        return Contains(point, true);
    }

    /// <summary>
    ///     Returns whether the box contains the specified point.
    /// </summary>
    /// <param name="point">The point to query.</param>
    /// <param name="closedRegion">Whether to include the box boundary in the test region.</param>
    /// <returns>Whether this box contains the point.</returns>
    public bool Contains(Vector2D point, bool closedRegion)
    {
        bool xOK = closedRegion == Left <= Right ? point.X >= Left != point.X > Right : point.X > Left != point.X >= Right;

        bool yOK = closedRegion == Top <= Bottom ? point.Y >= Top != point.Y > Bottom : point.Y > Top != point.Y >= Bottom;

        return xOK && yOK;
    }

    /// <summary>
    ///     Returns a Box2d translated by the given amount.
    /// </summary>
    public Box2D Translated(Vector2D point)
    {
        return new Box2D(Left + point.X, Top + point.Y, Right + point.X, Bottom + point.Y);
    }

    /// <summary>
    ///     Translates this Box2d by the given amount.
    /// </summary>
    public void Translate(Vector2D point)
    {
        Left += point.X;
        Right += point.X;
        Top += point.Y;
        Bottom += point.Y;
    }

    /// <summary>
    ///     Equality comparator.
    /// </summary>
    public static bool operator ==(Box2D left, Box2D right) =>
        left.Bottom == right.Bottom && left.Top == right.Top && left.Left == right.Left && left.Right == right.Right;

    /// <summary>
    ///     Inequality comparator.
    /// </summary>
    public static bool operator !=(Box2D left, Box2D right) => !(left == right);

    /// <summary>
    ///     Functional equality comparator.
    /// </summary>
    public bool Equals(Box2D other)
    {
        return this == other;
    }

    /// <summary>
    ///     Implements Object.Equals.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is Box2D && Equals((Box2D)obj);
    }

    /// <summary>
    ///     Gets the hash code for this Box2d.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Left.GetHashCode();
            hashCode = hashCode * 397 ^ Right.GetHashCode();
            hashCode = hashCode * 397 ^ Top.GetHashCode();
            hashCode = hashCode * 397 ^ Bottom.GetHashCode();
            return hashCode;
        }
    }

    private static readonly string listSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

    /// <summary>
    ///     Returns a <see cref="string" /> describing the current instance.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return string.Format("({0}{4} {1}) - ({2}{4} {3})", Left, Top, Right, Bottom, listSeparator);
    }
}
