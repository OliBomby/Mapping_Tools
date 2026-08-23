using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders;

/// <summary>
///     Helper methods for advanced bezier anchor and path approximation manipulation.
///     A <see cref="BezierSubdivision" /> represents a single bezier polynomial.
/// </summary>
// Many of these functions are designed to simulate PathApproximator methods, but
// with added functionality which should be kept separate from PathApproximator.
public class BezierSubdivision
{
    /// <summary>
    ///     The left-to-right node index at <see cref="Level" />.
    /// </summary>
    public int Index; // Index of subdivision

    /// <summary>
    ///     The depth in the binary de Casteljau subdivision tree.
    /// </summary>
    public int Level; // Depth of subdivision

    /// <summary>
    ///     Mutable Bézier control points for this parameter interval.
    /// </summary>
    public List<Vector2> Points; // List of bezier control Points

    /// <summary>
    ///     Creates a Bézier segment associated with a subdivision-tree node.
    /// </summary>
    /// <param name="points">The Bézier control polygon for this interval.</param>
    /// <param name="level">The level.</param>
    /// <param name="index">The node's left-to-right index at the specified depth.</param>
    public BezierSubdivision(List<Vector2> points, int level = 0, int index = 0)
    {
        Points = points;
        Level = level;
        Index = index;
    }

    /// <summary>
    ///     Gets the polynomial degree, one less than the control-point count.
    /// </summary>
    public int Order => Points.Count - 1; // Bezier polynomial order

    /// <summary>
    ///     Copies the control-point list and subdivision coordinates.
    /// </summary>
    /// <returns>An independently mutable segment.</returns>
    public BezierSubdivision Copy()
    {
        return new BezierSubdivision(new List<Vector2>(Points), Level, Index);
    }

    /// <summary>
    ///     Computes the maximum squared second-difference metric used by osu!'s Bézier approximator.
    /// </summary>
    /// <returns>The worst control-polygon deviation; zero denotes a linear polygon.</returns>
    public double Flatness() // Max of the flatness metric
    {
        double worst = 0;
        for (int i = 1; i < Order; i++) worst = Math.Max(worst, (Points[i - 1] - 2 * Points[i] + Points[i + 1]).LengthSquared);
        return Math.Sqrt(worst) / 2;
    }

    /// <summary>
    ///     Tests whether this segment can be approximated without further subdivision.
    /// </summary>
    /// <param name="tolerance">The tolerance.</param>
    /// <returns><see langword="true" /> when the squared flatness falls within the scaled tolerance.</returns>
    public bool Flat(double tolerance = 0.25) // Whether it would satisfy BezierIsFlatEnough
    {
        return Flatness() <= tolerance * tolerance * 4; // Tolerance is squared because the flatness is squared
    }

    /// <summary>
    ///     Sums the Euclidean lengths of consecutive control-polygon edges.
    /// </summary>
    /// <returns>An upper-bound-style length estimate for the Bézier segment.</returns>
    public double Length() // Euclidean length of subdivision segments
    {
        double length = 0;
        for (int i = 0; i < Order; i++)
            length += (Points[i + 1] - Points[i]).Length;
        return length;
    }

    /// <summary>
    ///     Reverses the curve parameterization in place.
    /// </summary>
    public void Reverse() // Reverse the Points
    {
        Points.Reverse();
    }

    /// <summary>
    ///     Restricts the curve in place to the original parameter interval <c>[0,t]</c>.
    /// </summary>
    /// <param name="t">The original curve parameter that becomes the new right endpoint.</param>
    public void ScaleRight(double t) // De Casteljau reparameterization [0,t]
    {
        for (int j = 0; j < Order; j++)
        for (int i = Order; i > j; i--)
            Points[i] = Points[i] * t + Points[i - 1] * (1 - t);
    }

    /// <summary>
    ///     Restricts the curve in place to the original parameter interval <c>[t,1]</c>.
    /// </summary>
    /// <param name="t">The original curve parameter that becomes the new left endpoint.</param>
    public void ScaleLeft(double t) // De Casteljau reparameterization [t,1]
    {
        for (int j = Order; j > 0; j--)
        for (int i = 0; i < j; i++)
            Points[i] = Points[i] * (1 - t) + Points[i + 1] * t;
    }

    /// <summary>
    ///     Reconstructs the adjacent subdivision to the right at the same tree depth.
    /// </summary>
    /// <returns>The sibling-position segment with index increased by one.</returns>
    public BezierSubdivision Next() // Next index at current level
    {
        var next = new BezierSubdivision(new List<Vector2>(Points), Level, Index + 1);
        next.ScaleLeft(2);
        next.Reverse();
        return next;
    }

    /// <summary>
    ///     Reconstructs the adjacent subdivision to the left at the same tree depth.
    /// </summary>
    /// <returns>The sibling-position segment with index decreased by one.</returns>
    public BezierSubdivision Prev() // Previous index at current level
    {
        var next = new BezierSubdivision(new List<Vector2>(Points), Level, Index - 1);
        next.ScaleRight(-1);
        next.Reverse();
        return next;
    }

    /// <summary>
    ///     Reconstructs the parent curve interval by undoing one subdivision step.
    /// </summary>
    /// <returns>The segment at depth <c>Level - 1</c> containing this node.</returns>
    public BezierSubdivision Parent() // Parent subdivision (inverse of BezierSubdivide)
    {
        var parent = new BezierSubdivision(new List<Vector2>(Points), Level - 1, Index >> 1);
        if ((Index & 1) == 0)
            parent.ScaleRight(2);
        else
            parent.ScaleLeft(-1);
        return parent;
    }

    /// <summary>
    ///     Splits the segment at <c>t = 0.5</c> using de Casteljau subdivision.
    /// </summary>
    /// <param name="leftChild">The left child.</param>
    /// <param name="rightChild">The right child.</param>
    public void Children(out BezierSubdivision leftChild, out BezierSubdivision rightChild) // Child subdivisions (BezierSubdivide)
    {
        var left = new List<Vector2>(Points);
        var right = new List<Vector2>(Points);
        for (int j = 0; j < Order; j++)
        for (int i = Order; i > j; i--)
        {
            left[i] = (left[i] + left[i - 1]) / 2;
            right[Order - i] = (right[Order - i] + right[Order - i + 1]) / 2;
        }

        leftChild = new BezierSubdivision(left, Level + 1, Index << 1);
        rightChild = new BezierSubdivision(right, Level + 1, Index << 1 | 1);
    }

    /// <summary>
    ///     Replaces non-flat linked-list nodes with their children until every segment meets the tolerance.
    /// </summary>
    /// <param name="subdivisions">The subdivisions.</param>
    /// <param name="tolerance">The tolerance.</param>
    public static void Subdivide(ref LinkedList<BezierSubdivision> subdivisions, double tolerance = 0.25) // Simulate the first part of ApproximateBezier on a linked list
    {
        var current = subdivisions.First;
        while (current != null)
            if (current.Value.Flat(tolerance))
            {
                current = current.Next;
            }
            else
            {
                current.Value.Children(out var left, out var right);
                current.Value = left;
                subdivisions.AddAfter(current, right);
            }
    }

    /// <summary>
    ///     Produces osu!-compatible polyline points for this already-small Bézier segment.
    /// </summary>
    /// <returns>The midpoint-based approximation used after flatness subdivision.</returns>
    public List<Vector2> Approximation() // BezierApproximate (the second part of ApproximateBezier)
    {
        Children(out var left, out var right);
        left.Points.RemoveAt(Order);
        left.Points.AddRange(right.Points);
        var output = new List<Vector2> { left.Points[0] };
        for (int i = 2; i < 2 * Order; i += 2)
            output.Add(0.25 * (left.Points[i - 1] + 2 * left.Points[i] + left.Points[i + 1]));
        output.Add(right.Points[Order]);
        return output;
    }

    /// <summary>
    ///     Measures the polyline returned by <see cref="Approximation" />.
    /// </summary>
    /// <returns>The local approximation length.</returns>
    public double ApproximationLength() // Euclidean length of approximation segments
    {
        var approximation = Approximation();
        double length = 0;
        for (int i = 0; i < Order; i++)
            length += (approximation[i + 1] - approximation[i]).Length;
        return length;
    }

    /// <summary>
    ///     Approximates total curve length using recursive flatness subdivision.
    /// </summary>
    /// <param name="tolerance">The tolerance.</param>
    /// <returns>The sum of all terminal-segment approximation lengths.</returns>
    public double SubdividedApproximationLength(double tolerance = 0.25) // Length of path approximation
    {
        var pathApproximation = new LinkedList<BezierSubdivision>();
        pathApproximation.AddLast(this);
        Subdivide(ref pathApproximation, tolerance);
        return pathApproximation.Sum(o => o.ApproximationLength());
    }

    /// <summary>
    ///     Degree-elevates the Bézier curve without changing its geometric shape.
    /// </summary>
    /// <param name="k">The k.</param>
    public void Increase(int k = 1) // Increase bezier order by k
    {
        for (int j = 0; j < k; j++)
        {
            Points.Add(Points[Order]);
            for (int i = Order - 1; i > 0; i--)
                Points[i] = (Points[i] * (Order - i) + Points[i - 1] * i) / Order;
        }
    }

    /// <summary>
    ///     Finds the parameter whose approximated arc length reaches a requested distance.
    /// </summary>
    /// <param name="length">The desired arc length from the segment start.</param>
    /// <param name="precision">The precision.</param>
    /// <param name="tolerance">The tolerance.</param>
    /// <returns>The approximate parameter; values greater than one extrapolate beyond the curve.</returns>
    public double LengthToT(double length, double precision = 0.1, double tolerance = 0.25) // approximate bezier progress t for a desired path length, t can be > 1
    {
        if (Length() == 0)
            return double.NaN;
        if (length <= 0)
            return 0;

        BezierSubdivision baseSubdivision = null;
        LinkedListNode<BezierSubdivision> current = null;
        double l = 0;
        double lnext = 0;
        while (length > lnext)
        {
            current = current?.Next;
            if (current == null)
            {
                baseSubdivision = baseSubdivision == null ? this : baseSubdivision.Next();
                var pathApproximation = new LinkedList<BezierSubdivision>();
                pathApproximation.AddLast(baseSubdivision);
                Subdivide(ref pathApproximation, tolerance);
                current = pathApproximation.First;
            }

            l = lnext;
            lnext += current.Value.ApproximationLength();
        }

        var curr = current.Value;
        while (curr.ApproximationLength() > precision)
        {
            curr.Children(out var left, out var right);
            lnext = l + left.ApproximationLength();
            if (length > lnext)
            {
                curr = right;
                l = lnext;
            }
            else
            {
                curr = left;
            }
        }

        return (curr.Index + (length - l) / curr.ApproximationLength()) / (1 << curr.Level);
    }
}
