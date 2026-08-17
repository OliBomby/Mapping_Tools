using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.ObjectVisualiser;

/// <summary>Provides reusable polyline geometry for a visualized slider path.</summary>
public sealed class ObjectVisualiserPath
{
    private readonly Vector2[] points;
    private readonly double[] cumulativeLengths;

    /// <summary>Creates a path from one or more finite points.</summary>
    /// <param name="points">The points in drawing order.</param>
    public ObjectVisualiserPath(IEnumerable<Vector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        this.points = points.ToArray();
        if (this.points.Length == 0)
        {
            throw new ArgumentException("A visualizer path needs at least one point.", nameof(points));
        }

        for (var i = 0; i < this.points.Length; i++)
        {
            if (!double.IsFinite(this.points[i].X) || !double.IsFinite(this.points[i].Y))
            {
                throw new ArgumentException("A visualizer path can only contain finite points.", nameof(points));
            }
        }

        cumulativeLengths = new double[this.points.Length];
        for (var i = 1; i < this.points.Length; i++)
        {
            cumulativeLengths[i] = cumulativeLengths[i - 1] + (this.points[i] - this.points[i - 1]).Length;
            if (!double.IsFinite(cumulativeLengths[i]))
            {
                throw new ArgumentException("A visualizer path is too large to measure.", nameof(points));
            }
        }

        Points = Array.AsReadOnly(this.points);
        Bounds = ObjectVisualiserBounds.FromPoints(this.points);
    }

    /// <summary>Gets the immutable points in drawing order.</summary>
    public IReadOnlyList<Vector2> Points { get; }

    /// <summary>Gets the total polyline length in playfield pixels.</summary>
    public double Length => cumulativeLengths[^1];

    /// <summary>Gets the smallest bounds containing the path points.</summary>
    public ObjectVisualiserBounds Bounds { get; }

    /// <summary>Returns the point at normalized distance along the path.</summary>
    /// <param name="progress">A value from zero at the start to one at the end.</param>
    /// <returns>The interpolated path position.</returns>
    public Vector2 PositionAt(double progress)
    {
        if (!double.IsFinite(progress))
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        if (points.Length == 1 || Length == 0)
        {
            return points[0];
        }

        double distance = Math.Clamp(progress, 0, 1) * Length;
        int segment = Array.BinarySearch(cumulativeLengths, distance);
        if (segment >= 0)
        {
            return Points[segment];
        }

        segment = ~segment;
        if (segment <= 0)
        {
            return Points[0];
        }

        if (segment >= points.Length)
        {
            return points[^1];
        }

        double segmentLength = cumulativeLengths[segment] - cumulativeLengths[segment - 1];
        double segmentProgress = segmentLength == 0
            ? 0
            : (distance - cumulativeLengths[segment - 1]) / segmentLength;
        return Vector2.Lerp(points[segment - 1], points[segment], segmentProgress);
    }

    /// <summary>Finds the shortest distance from a point to this polyline.</summary>
    /// <param name="point">The point to inspect.</param>
    /// <returns>The distance in playfield pixels.</returns>
    public double DistanceTo(Vector2 point)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(point));
        }

        double result = double.PositiveInfinity;
        for (var i = 1; i < points.Length; i++)
        {
            Vector2 start = points[i - 1];
            Vector2 end = points[i];
            Vector2 delta = end - start;
            double lengthSquared = delta.LengthSquared;
            double fraction = lengthSquared == 0
                ? 0
                : Math.Clamp((point - start).X * delta.X + (point - start).Y * delta.Y, 0, lengthSquared) / lengthSquared;
            result = Math.Min(result, (point - Vector2.Lerp(start, end, fraction)).Length);
        }

        return points.Length == 1 ? (point - points[0]).Length : result;
    }
}
