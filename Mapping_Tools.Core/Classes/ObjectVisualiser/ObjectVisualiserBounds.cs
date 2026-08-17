using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.ObjectVisualiser;

/// <summary>Describes an axis-aligned rectangle in osu! playfield coordinates.</summary>
public readonly record struct ObjectVisualiserBounds
{
    /// <summary>Creates a non-negative finite rectangle.</summary>
    /// <param name="left">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="width">The rectangle width.</param>
    /// <param name="height">The rectangle height.</param>
    public ObjectVisualiserBounds(double left, double top, double width, double height)
    {
        if (!double.IsFinite(left) || !double.IsFinite(top) || !double.IsFinite(width) || !double.IsFinite(height) ||
            width < 0 || height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    /// <summary>Gets the left coordinate.</summary>
    public double Left { get; }

    /// <summary>Gets the top coordinate.</summary>
    public double Top { get; }

    /// <summary>Gets the rectangle width.</summary>
    public double Width { get; }

    /// <summary>Gets the rectangle height.</summary>
    public double Height { get; }

    /// <summary>Gets the right coordinate.</summary>
    public double Right => Left + Width;

    /// <summary>Gets the bottom coordinate.</summary>
    public double Bottom => Top + Height;

    /// <summary>Gets the rectangle center.</summary>
    public Vector2 Center => new(Left + Width / 2, Top + Height / 2);

    /// <summary>Gets an empty bounds value at the origin.</summary>
    public static ObjectVisualiserBounds Empty => new(0, 0, 0, 0);

    /// <summary>Creates bounds around a non-empty set of points.</summary>
    /// <param name="points">The points to enclose.</param>
    /// <returns>The smallest axis-aligned rectangle containing all points.</returns>
    public static ObjectVisualiserBounds FromPoints(IEnumerable<Vector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        using IEnumerator<Vector2> enumerator = points.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return Empty;
        }

        Vector2 first = enumerator.Current;
        double left = first.X;
        double right = first.X;
        double top = first.Y;
        double bottom = first.Y;

        while (enumerator.MoveNext())
        {
            Vector2 point = enumerator.Current;
            left = Math.Min(left, point.X);
            right = Math.Max(right, point.X);
            top = Math.Min(top, point.Y);
            bottom = Math.Max(bottom, point.Y);
        }

        return new ObjectVisualiserBounds(left, top, right - left, bottom - top);
    }

    /// <summary>Expands the bounds in both directions without changing its center.</summary>
    /// <param name="horizontal">The amount added to each horizontal side.</param>
    /// <param name="vertical">The amount added to each vertical side.</param>
    /// <returns>The expanded bounds.</returns>
    public ObjectVisualiserBounds Inflate(double horizontal, double vertical)
    {
        if (!double.IsFinite(horizontal) || !double.IsFinite(vertical) || horizontal < 0 || vertical < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(horizontal));
        }

        return new ObjectVisualiserBounds(
            Left - horizontal,
            Top - vertical,
            Width + 2 * horizontal,
            Height + 2 * vertical);
    }

    /// <summary>Returns whether a point lies inside the inclusive rectangle.</summary>
    /// <param name="point">The point to inspect.</param>
    /// <returns><see langword="true"/> when the point is inside the bounds.</returns>
    public bool Contains(Vector2 point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    /// <summary>Returns the smallest bounds containing this rectangle and another rectangle.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The union of both rectangles.</returns>
    public ObjectVisualiserBounds Union(ObjectVisualiserBounds other)
    {
        double left = Math.Min(Left, other.Left);
        double top = Math.Min(Top, other.Top);
        double right = Math.Max(Right, other.Right);
        double bottom = Math.Max(Bottom, other.Bottom);
        return new ObjectVisualiserBounds(left, top, right - left, bottom - top);
    }
}
