using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders;

/// <summary>
///     Adjusts slider anchors while preserving curve shape as closely as possible.
/// </summary>
public static class SliderPathUtil
{
    /// <summary>Moves a typed slider path's end to the requested length.</summary>
    /// <param name="controlPoints">The ordered path control points.</param>
    /// <param name="newLength">The requested length.</param>
    /// <returns>Independent control points preserving all completed segment types.</returns>
    public static List<PathControlPoint> MoveAnchorsToLength(IReadOnlyList<PathControlPoint> controlPoints, double newLength)
    {
        var path = new SliderPath(controlPoints.ToArray());
        return MoveAnchorsToLength(path, path.Distance, newLength);
    }

    /// <summary>Moves a typed slider path's end using a caller-supplied full length.</summary>
    /// <param name="controlPoints">The ordered path control points.</param>
    /// <param name="fullLength">The length before the move.</param>
    /// <param name="newLength">The requested length.</param>
    /// <returns>Independent control points preserving all completed segment types.</returns>
    public static List<PathControlPoint> MoveAnchorsToLength(IReadOnlyList<PathControlPoint> controlPoints, double fullLength, double newLength)
    {
        return MoveAnchorsToLength(new SliderPath(controlPoints.ToArray()), fullLength, newLength);
    }

    /// <summary>Moves a typed slider path's end to a fraction of its full length.</summary>
    /// <param name="controlPoints">The ordered path control points.</param>
    /// <param name="completion">The fraction of the full path to retain.</param>
    /// <returns>Independent control points preserving all completed segment types.</returns>
    public static List<PathControlPoint> MoveAnchorsToCompletion(IReadOnlyList<PathControlPoint> controlPoints, double completion)
    {
        var path = new SliderPath(controlPoints.ToArray());
        return MoveAnchorsToLength(path, path.Distance, completion * path.Distance);
    }

    /// <summary>Moves the end of a typed slider path while keeping complete segments unchanged.</summary>
    /// <param name="sliderPath">The source path.</param>
    /// <param name="fullLength">The length before the move.</param>
    /// <param name="newLength">The requested length.</param>
    /// <returns>Independent control points with segment types.</returns>
    public static List<PathControlPoint> MoveAnchorsToLength(SliderPath sliderPath, double fullLength, double newLength)
    {
        var source = sliderPath.PathControlPoints;
        var result = source.Select(point => point.Copy()).ToList();
        if (source.Count < 2 || Precision.AlmostEquals(newLength, fullLength, 0.01)) return result;

        var targetPath = new SliderPath(source.ToArray(), newLength);
        if (newLength > fullLength)
        {
            Vector2 endpoint = targetPath.PositionAt(1);
            if (source[0].Type == PathType.Linear && source.Skip(1).All(point => !point.Type.HasValue))
                result[^1].Position = endpoint;
            else
            {
                result[^1].Type = PathType.Linear;
                result.Add(new PathControlPoint(endpoint));
            }

            return result;
        }

        result.Clear();
        double remainingLength = newLength;
        foreach (var segment in GetTypedSegments(source))
        {
            var positions = segment.Points.Select(point => point.Position).ToArray();
            double segmentLength = new SliderPath(segment.Type, positions).Distance;
            if (remainingLength >= segmentLength - 0.01)
            {
                AppendSegment(result, segment.Points);
                remainingLength -= segmentLength;
                if (remainingLength <= 0.01) break;
                continue;
            }

            var cutPath = new SliderPath(segment.Type, positions, Math.Max(0, remainingLength));
            var cutPoints = MoveSingleSegmentAnchorsToLength(cutPath, segmentLength, remainingLength);
            AppendSegment(result, cutPoints);
            break;
        }

        return result;
    }

    private static IEnumerable<(List<PathControlPoint> Points, PathType Type)> GetTypedSegments(IReadOnlyList<PathControlPoint> points)
    {
        int start = 0;
        PathType activeType = points[0].Type ?? PathType.Linear;
        for (int i = 0; i < points.Count; i++)
        {
            bool typedBoundary = i > start && points[i].Type.HasValue;
            if (i != points.Count - 1 && !typedBoundary) continue;

            yield return (points.Skip(start).Take(i - start + 1).ToList(), activeType);

            if (typedBoundary) activeType = points[i].Type!.Value;
            start = i;
        }
    }

    private static void AppendSegment(List<PathControlPoint> result, IReadOnlyList<PathControlPoint> segment)
    {
        if (result.Count == 0)
        {
            result.AddRange(segment.Select(point => point.Copy()));
            return;
        }

        result[^1].Type = segment[0].Type;
        result.AddRange(segment.Skip(1).Select(point => point.Copy()));
    }

    private static List<PathControlPoint> MoveSingleSegmentAnchorsToLength(SliderPath sliderPath, double fullLength, double newLength)
    {
        var newAnchors = new List<Vector2>();
        var pathType = sliderPath.Type;
        PathType newPathType;
        var anchors = sliderPath.ControlPoints;

        if (Precision.AlmostEquals(newLength, fullLength, 0.01))
        {
            newAnchors.AddRange(anchors);
            newPathType = pathType;
            return newAnchors.Select((position, index) => new PathControlPoint(position, index == 0 ? newPathType : null)).ToList();
        }

        switch (sliderPath.Type)
            {
                case PathType.Catmull:
                case PathType.Bezier:
                    // Convert in case the path type is catmull
                    var convert = BezierConverter.ConvertToBezier(sliderPath);
                    return MoveBezierSegmentsToLength(ChopAnchors(convert), newLength);
                case PathType.BSpline:
                    var splineSegments = PathApproximator.GetBSplineBezierSegments(anchors, 4)
                        .Select(points => new BezierSubdivision(points.ToList()));
                    return MoveBezierSegmentsToLength(splineSegments, newLength);
                case PathType.PerfectCurve:
                    newPathType = PathType.PerfectCurve;
                    newAnchors.AddRange(anchors);
                    newAnchors[1] = sliderPath.PositionAt(0.5);
                    newAnchors[2] = sliderPath.PositionAt(1);
                    break;
                default:
                    newPathType = pathType;
                    if (anchors.Count > 2)
                    {
                        // Find the section of the linear slider which contains the slider end
                        double totalLength = 0;
                        foreach (var bezierSubdivision in ChopAnchorsLinear(anchors))
                        {
                            newAnchors.Add(bezierSubdivision.Points[0]);
                            double length = bezierSubdivision.Length();

                            if (Precision.AlmostBigger(totalLength + length, newLength)) break;

                            totalLength += length;
                        }

                        newAnchors.Add(sliderPath.PositionAt(1));
                    }
                    else
                    {
                        newAnchors.AddRange(anchors);
                        newAnchors[^1] = sliderPath.PositionAt(1);
                    }

                    break;
        }

        return newAnchors.Select((position, index) => new PathControlPoint(position, index == 0 ? newPathType : null)).ToList();
    }

    private static List<PathControlPoint> MoveBezierSegmentsToLength(IEnumerable<BezierSubdivision> subdivisions, double newLength)
    {
        var typedAnchors = new List<PathControlPoint>();
        BezierSubdivision? subdivision = null;
        double totalLength = 0;

        foreach (var bezierSubdivision in subdivisions)
        {
            subdivision = bezierSubdivision;
            double length = bezierSubdivision.SubdividedApproximationLength();

            if (Precision.AlmostBigger(totalLength + length, newLength)) break;

            totalLength += length;
            AppendBezierSubdivision(typedAnchors, bezierSubdivision.Points);
        }

        if (subdivision is null) return typedAnchors;

        double t = subdivision.LengthToT(newLength - totalLength);
        subdivision.ScaleRight(t);
        AppendBezierSubdivision(typedAnchors, subdivision.Points);
        return typedAnchors;
    }

    private static void AppendBezierSubdivision(List<PathControlPoint> result, IReadOnlyList<Vector2> positions)
    {
        if (result.Count == 0)
        {
            result.Add(new PathControlPoint(positions[0], PathType.Bezier));
        }
        else
        {
            result[^1].Type = PathType.Bezier;
        }

        foreach (var position in positions.Skip(1)) result.Add(new PathControlPoint(position));
    }

    /// <summary>
    ///     Calculates the completion values of all the red anchors along the path.
    /// </summary>
    /// <param name="sliderPath">The path whose typed segment boundaries are inspected.</param>
    /// <returns>Normalized path completions for red-anchor boundaries.</returns>
    public static IEnumerable<double> GetRedAnchorCompletions(SliderPath sliderPath)
    {
        double totalLength = 0;
        var segments = GetTypedSegments(sliderPath.PathControlPoints).ToList();
        for (int index = 0; index < segments.Count - 1; index++)
        {
            var segment = segments[index];
            totalLength += new SliderPath(segment.Type,
                segment.Points.Select(point => point.Position).ToArray()).Distance;
            yield return totalLength / sliderPath.Distance;
        }
    }

    /// <summary>
    ///     Splits a slider into independent Bézier subdivisions at path-type-specific boundaries.
    /// </summary>
    /// <param name="sliderPath">The slider path.</param>
    /// <returns>Segments suitable for independent length manipulation.</returns>
    public static IEnumerable<BezierSubdivision> ChopAnchors(SliderPath sliderPath)
    {
        foreach (var segment in GetTypedSegments(sliderPath.PathControlPoints))
        {
            var positions = segment.Points.Select(point => point.Position).ToList();
            if (segment.Type is PathType.Catmull or PathType.Linear)
            {
                foreach (var subdivision in ChopAnchorsLinear(positions)) yield return subdivision;
            }
            else
            {
                yield return new BezierSubdivision(positions);
            }
        }
    }

    /// <summary>
    ///     Converts every consecutive pair of linear anchors into a first-order Bézier segment.
    /// </summary>
    /// <param name="anchors">Polyline vertices in source order.</param>
    /// <returns>One segment for each polyline edge.</returns>
    public static IEnumerable<BezierSubdivision> ChopAnchorsLinear(List<Vector2> anchors)
    {
        for (int i = 1; i < anchors.Count; i++)
        {
            var subdivision = new BezierSubdivision([anchors[i - 1], anchors[i]]);
            yield return subdivision;
        }
    }

    /// <summary>
    ///     Measures point-to-label fit using paired squared Euclidean error.
    /// </summary>
    /// <param name="points">Reconstructed sample points to compare with labels.</param>
    /// <param name="labels">The labels.</param>
    /// <returns>The mean squared positional error over the paired points.</returns>
    public static double CalculateLoss(IReadOnlyCollection<Vector2> points, IReadOnlyList<Vector2> labels)
    {
        int n = points.Count;
        double totalLoss = 0;

        foreach (var point in points)
        {
            double minLoss = double.PositiveInfinity;

            for (int i = 0; i < labels.Count - 1; i++)
            {
                var p1 = labels[i];
                var p2 = labels[i + 1];

                double loss = MinimumDistance(p1, p2, point);

                if (loss < minLoss) minLoss = loss;
            }

            totalLoss += minLoss;
        }

        return totalLoss / n;
    }

    private static double MinimumDistance(Vector2 v, Vector2 w, Vector2 p)
    {
        // Return minimum distance between line segment vw and point p
        double l2 = Vector2.DistanceSquared(v, w); // i.e. |w-v|^2 -  avoid a sqrt
        if (l2 == 0.0) return Vector2.Distance(p, v); // v == w case
        // Consider the line extending the segment, parameterized as v + t (w - v).
        // We find projection of point p onto the line.
        // It falls where t = [(p-v) . (w-v)] / |w-v|^2
        // We clamp t from [0,1] to handle points outside the segment vw.
        double t = Math.Max(0, Math.Min(1, Vector2.Dot(p - v, w - v) / l2));
        var projection = v + t * (w - v); // Projection falls on the segment
        return Vector2.Distance(p, projection);
    }
}
