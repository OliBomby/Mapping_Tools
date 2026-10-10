using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;

/// <summary>
///     Carries a sampled path position together with reconstruction geometry and stable path ordering.
/// </summary>
public struct PathPoint : IComparable<PathPoint>
{
    /// <summary>
    ///     The current, possibly edited position.
    /// </summary>
    public Vector2 Pos;

    /// <summary>
    ///     The original sampled position used to measure reconstruction error.
    /// </summary>
    public Vector2 OgPos;

    /// <summary>
    ///     Incoming tangent angle in radians, or NaN at an undefined boundary.
    /// </summary>
    public double PreAngle;

    /// <summary>
    ///     Outgoing tangent angle in radians, or NaN at an undefined boundary.
    /// </summary>
    public double PostAngle;

    /// <summary>
    ///     Distance along the sampled path from its first point.
    /// </summary>
    public double CumulativeLength;

    /// <summary>
    ///     Used to define distance between points which are on the same position. [0,1]
    /// </summary>
    public double T;

    /// <summary>
    ///     If true, indicates that this point is not continuous in local curvature.
    /// </summary>
    public bool Red;

    /// <summary>
    ///     Creates a point whose original and current positions are identical.
    /// </summary>
    /// <param name="pos">The pos.</param>
    /// <param name="preAngle">The pre angle.</param>
    /// <param name="postAngle">The post angle.</param>
    /// <param name="cumulativeLength">The cumulative length.</param>
    /// <param name="t">The ordering parameter for points at equal cumulative distance.</param>
    /// <param name="red">The red.</param>
    public PathPoint(Vector2 pos, double preAngle = 0, double postAngle = 0, double cumulativeLength = 0,
        double t = double.NaN, bool red = false) : this(pos, pos, preAngle, postAngle, cumulativeLength, t, red)
    {
    }

    /// <summary>
    ///     Creates a point with separate edited and original positions.
    /// </summary>
    /// <param name="pos">The pos.</param>
    /// <param name="ogPos">The og pos.</param>
    /// <param name="preAngle">The pre angle.</param>
    /// <param name="postAngle">The post angle.</param>
    /// <param name="cumulativeLength">The cumulative length.</param>
    /// <param name="t">The ordering parameter for points at equal cumulative distance.</param>
    /// <param name="red">The red.</param>
    public PathPoint(Vector2 pos, Vector2 ogPos, double preAngle = 0, double postAngle = 0, double cumulativeLength = 0, double t = double.NaN, bool red = false)
    {
        Pos = pos;
        OgPos = ogPos;
        PreAngle = preAngle;
        PostAngle = postAngle;
        CumulativeLength = cumulativeLength;
        T = t;
        Red = red;
    }

    /// <summary>
    ///     Gets the available tangent or the shortest-angle midpoint between incoming and outgoing tangents.
    /// </summary>
    public double AvgAngle => double.IsNaN(PreAngle) ? PostAngle :
        double.IsNaN(PostAngle) ? PreAngle : MathHelper.LerpAngle(PreAngle, PostAngle, 0.5);

    /// <summary>
    ///     Returns a copy with a new same-position ordering parameter.
    /// </summary>
    /// <param name="t">The replacement same-position ordering parameter.</param>
    /// <returns>A point differing only in <see cref="T" />.</returns>
    public PathPoint SetT(double t)
    {
        return new PathPoint(Pos, OgPos, PreAngle, PostAngle, CumulativeLength, t, Red);
    }

    /// <summary>
    ///     Returns a copy with updated curvature-discontinuity state.
    /// </summary>
    /// <param name="red">The red.</param>
    /// <returns>A point differing only in <see cref="Red" />.</returns>
    public PathPoint SetRed(bool red)
    {
        return new PathPoint(Pos, OgPos, PreAngle, PostAngle, CumulativeLength, T, red);
    }

    /// <summary>
    ///     Adds the specified instances.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Result of addition.</returns>
    public static PathPoint operator +(PathPoint left, PathPoint right)
    {
        left.Pos += right.Pos;
        left.OgPos += right.OgPos;
        left.PreAngle += right.PreAngle;
        left.PostAngle += right.PostAngle;
        left.CumulativeLength += right.CumulativeLength;
        left.Red |= right.Red;
        return left;
    }

    /// <summary>
    ///     Subtracts the specified instances.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Result of subtraction.</returns>
    public static PathPoint operator -(PathPoint left, PathPoint right)
    {
        left.Pos -= right.Pos;
        left.OgPos -= right.OgPos;
        left.PreAngle -= right.PreAngle;
        left.PostAngle -= right.PostAngle;
        left.CumulativeLength -= right.CumulativeLength;
        left.Red &= right.Red;
        return left;
    }

    /// <summary>
    ///     Negates the specified instance.
    /// </summary>
    /// <param name="vec">Operand.</param>
    /// <returns>Result of negation.</returns>
    public static PathPoint operator -(PathPoint vec)
    {
        vec.Pos = -vec.Pos;
        vec.OgPos = -vec.OgPos;
        vec.PreAngle += Math.PI;
        vec.PostAngle += Math.PI;
        return vec;
    }

    /// <summary>
    ///     Multiplies the specified instance by a scalar.
    /// </summary>
    /// <param name="vec">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    public static PathPoint operator *(PathPoint vec, double scale)
    {
        vec.Pos *= scale;
        vec.OgPos *= scale;
        vec.CumulativeLength *= scale;
        return vec;
    }

    /// <summary>
    ///     Multiplies the specified instance by a scalar.
    /// </summary>
    /// <param name="scale">Left operand.</param>
    /// <param name="vec">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    public static PathPoint operator *(double scale, PathPoint vec)
    {
        vec.Pos *= scale;
        vec.OgPos *= scale;
        vec.CumulativeLength *= scale;
        return vec;
    }

    /// <summary>
    ///     Divides the specified instance by a scalar.
    /// </summary>
    /// <param name="vec">Left operand</param>
    /// <param name="scale">Right operand</param>
    /// <returns>Result of the division.</returns>
    public static PathPoint operator /(PathPoint vec, double scale)
    {
        vec.Pos /= scale;
        vec.OgPos /= scale;
        vec.CumulativeLength /= scale;
        return vec;
    }

    /// <summary>
    ///     Returns a new PathPoint that is the linear blend of the 2 given PathPoint
    /// </summary>
    /// <param name="a">First input PathPoint</param>
    /// <param name="b">Second input PathPoint</param>
    /// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
    /// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
    public static PathPoint Lerp(PathPoint a, PathPoint b, double blend)
    {
        a.Pos = blend * (b.Pos - a.Pos) + a.Pos;
        a.OgPos = blend * (b.OgPos - a.OgPos) + a.OgPos;
        double angle1 = a.Red && !double.IsNaN(a.PostAngle) ? a.PostAngle : a.AvgAngle;
        double angle2 = b.Red && !double.IsNaN(b.PreAngle) ? b.PreAngle : b.AvgAngle;
        a.PreAngle = MathHelper.LerpAngle(angle1, angle2, blend);
        a.PostAngle = a.PreAngle;
        a.CumulativeLength = blend * (b.CumulativeLength - a.CumulativeLength) + a.CumulativeLength;
        a.T = blend * (b.T - a.T) + a.T;
        a.Red = false;
        return a;
    }

    /// <summary>
    ///     Formats reconstruction geometry for diagnostics.
    /// </summary>
    /// <returns>Current/original positions, tangents, distance, ordering parameter, and red state.</returns>
    public override string ToString()
    {
        return $"{Pos} {OgPos} ({PreAngle}, {PostAngle}) {CumulativeLength} {T} {Red}";
    }

    /// <summary>
    ///     Orders points by cumulative distance and then by <see cref="T" /> at coincident positions.
    /// </summary>
    /// <param name="other">The point to compare.</param>
    /// <returns>A standard sort value.</returns>
    public int CompareTo(PathPoint other)
    {
        int cumulativeLengthComparison = CumulativeLength.CompareTo(other.CumulativeLength);
        return cumulativeLengthComparison != 0 ? cumulativeLengthComparison : T.CompareTo(other.T);
    }

    /// <summary>
    ///     Applies the &lt; operator.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true" /> when the left point precedes the right in path order.</returns>
    public static bool operator <(PathPoint left, PathPoint right) => left.CompareTo(right) < 0;

    /// <summary>
    ///     Applies the &gt; operator.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true" /> when the left point follows the right in path order.</returns>
    public static bool operator >(PathPoint left, PathPoint right) => left.CompareTo(right) > 0;

    /// <summary>
    ///     Applies the &lt;= operator.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true" /> when the left point does not follow the right.</returns>
    public static bool operator <=(PathPoint left, PathPoint right) => left.CompareTo(right) <= 0;

    /// <summary>
    ///     Applies the &gt;= operator.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true" /> when the left point does not precede the right.</returns>
    public static bool operator >=(PathPoint left, PathPoint right) => left.CompareTo(right) >= 0;
}
