using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.ToolHelpers.Sliders;

/// <summary>
///     Adjusts slider anchors while preserving curve shape as closely as possible.
/// </summary>
public static class SliderPathUtil
{
    /// <summary>
    ///     Fits anchors to a target path length after measuring the source curve.
    /// </summary>
    /// <param name="anchors">The source slider control points including its start.</param>
    /// <param name="pathType">The path type.</param>
    /// <param name="newLength">The new length.</param>
    /// <param name="newPathType">The new path type.</param>
    /// <returns>Adjusted anchors and, through <paramref name="newPathType" />, any required type conversion.</returns>
    public static List<Vector2> MoveAnchorsToLength(List<Vector2> anchors, PathType pathType, double newLength, out PathType newPathType)
    {
        var sliderPath = new SliderPath(pathType, anchors.ToArray(), newLength);
        double fullLength = new SliderPath(pathType, anchors.ToArray()).Distance;

        return MoveAnchorsToLength(sliderPath, fullLength, newLength, out newPathType);
    }

    /// <summary>
    ///     Fits anchors to a target length using a caller-supplied untruncated source length.
    /// </summary>
    /// <param name="anchors">The source slider control points including its start.</param>
    /// <param name="pathType">The path type.</param>
    /// <param name="fullLength">The full length.</param>
    /// <param name="newLength">The new length.</param>
    /// <param name="newPathType">The new path type.</param>
    /// <returns>Adjusted anchors preserving the original path as far as possible.</returns>
    public static List<Vector2> MoveAnchorsToLength(List<Vector2> anchors, PathType pathType, double fullLength, double newLength, out PathType newPathType)
    {
        var sliderPath = new SliderPath(pathType, anchors.ToArray(), newLength);

        return MoveAnchorsToLength(sliderPath, fullLength, newLength, out newPathType);
    }

    /// <summary>
    ///     Fits anchors to a fraction of the source curve's full length.
    /// </summary>
    /// <param name="anchors">The source slider control points including its start.</param>
    /// <param name="pathType">The path type.</param>
    /// <param name="completion">The completion.</param>
    /// <param name="newPathType">The new path type.</param>
    /// <returns>Anchors ending at the requested completion.</returns>
    public static List<Vector2> MoveAnchorsToCompletion(List<Vector2> anchors, PathType pathType, double completion, out PathType newPathType)
    {
        double fullLength = new SliderPath(pathType, anchors.ToArray()).Distance;
        var sliderPath = new SliderPath(pathType, anchors.ToArray(), completion * fullLength);

        return MoveAnchorsToLength(sliderPath, fullLength, sliderPath.Distance, out newPathType);
    }

    /// <summary>
    ///     Extends or truncates a prepared slider path by moving and reconstructing its anchors.
    /// </summary>
    /// <param name="sliderPath">The slider path.</param>
    /// <param name="fullLength">The full length.</param>
    /// <param name="newLength">The new length.</param>
    /// <param name="newPathType">The new path type.</param>
    /// <returns>The reconstructed anchors and resulting curve type.</returns>
    public static List<Vector2> MoveAnchorsToLength(SliderPath sliderPath, double fullLength, double newLength, out PathType newPathType)
    {
        var newAnchors = new List<Vector2>();
        var pathType = sliderPath.Type;
        var anchors = sliderPath.ControlPoints;

        if (Precision.AlmostEquals(newLength, fullLength, 0.01))
        {
            newAnchors.AddRange(anchors);
            newPathType = pathType;
            return newAnchors;
        }

        if (newLength > fullLength)
            // Extend linearly
            switch (pathType)
            {
                case PathType.Bezier:
                    newPathType = PathType.Bezier;
                    newAnchors.AddRange(anchors);

                    if (newAnchors.Count > 1 && newAnchors[^2] == newAnchors[^1]) newAnchors[^2] += Vector2.UnitX;

                    newAnchors.Add(anchors[^1]);
                    newAnchors.Add(sliderPath.PositionAt(1));
                    break;
                case PathType.Catmull:
                case PathType.PerfectCurve:
                    // Convert to bezier and then extend
                    newPathType = PathType.Bezier;
                    newAnchors = BezierConverter.ConvertToBezier(sliderPath).ControlPoints;
                    newAnchors.Add(anchors.Last());
                    newAnchors.Add(sliderPath.PositionAt(1));
                    break;
                default:
                    newPathType = pathType;
                    newAnchors.AddRange(anchors);
                    newAnchors[^1] = sliderPath.PositionAt(1);
                    break;
            }
        else
            switch (sliderPath.Type)
            {
                case PathType.Catmull:
                case PathType.Bezier:
                    newPathType = PathType.Bezier;

                    // Convert in case the path type is catmull
                    var convert = BezierConverter.ConvertToBezier(sliderPath).ControlPoints;

                    // Find the last bezier segment and the pixel length at that part
                    BezierSubdivision subdivision = null;
                    double totalLength = 0;

                    foreach (var bezierSubdivision in ChopAnchors(convert))
                    {
                        subdivision = bezierSubdivision;
                        double length = bezierSubdivision.SubdividedApproximationLength();

                        if (Precision.AlmostBigger(totalLength + length, newLength)) break;

                        totalLength += length;
                        newAnchors.AddRange(bezierSubdivision.Points);
                    }

                    if (subdivision == null) break;

                    // Find T for the remaining pixel length
                    double t = subdivision.LengthToT(newLength - totalLength);

                    // ScaleRight the BezierSubdivision so the anchors end at T
                    subdivision.ScaleRight(t);

                    // Add the scaled anchors
                    newAnchors.AddRange(subdivision.Points);
                    break;
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
                        totalLength = 0;
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

        return newAnchors;
    }

    /// <summary>
    ///     Calculates the completion values of all the red anchors along the path.
    /// </summary>
    /// <param name="sliderPath">The path whose repeated-anchor segment boundaries are inspected.</param>
    /// <returns>Normalized path completions for red-anchor boundaries.</returns>
    public static IEnumerable<double> GetRedAnchorCompletions(SliderPath sliderPath)
    {
        int start = 0;
        int end = 0;
        double totalLength = 0;
        var anchors = sliderPath.ControlPoints;

        for (int i = 0; i < anchors.Count; i++)
        {
            end++;

            if (i == anchors.Count - 1 || anchors[i] != anchors[i + 1]) continue;

            var cpSpan = anchors.GetRange(start, end - start);
            var subdivision = new BezierSubdivision(cpSpan);
            totalLength += subdivision.SubdividedApproximationLength();

            yield return totalLength / sliderPath.Distance;

            start = end;
        }
    }

    /// <summary>
    ///     Splits a slider into independent Bézier subdivisions at path-type-specific boundaries.
    /// </summary>
    /// <param name="sliderPath">The slider path.</param>
    /// <returns>Segments suitable for independent length manipulation.</returns>
    public static IEnumerable<BezierSubdivision> ChopAnchors(SliderPath sliderPath)
    {
        switch (sliderPath.Type)
        {
            case PathType.Catmull:
            case PathType.Linear:
                return ChopAnchorsLinear(sliderPath.ControlPoints);
            default:
                return ChopAnchors(sliderPath.ControlPoints);
        }
    }

    /// <summary>
    ///     Splits a Bézier anchor list at consecutive duplicate red anchors.
    /// </summary>
    /// <param name="anchors">Bézier control points containing duplicate red-anchor separators.</param>
    /// <returns>One Bézier segment per red-anchor-delimited section.</returns>
    public static IEnumerable<BezierSubdivision> ChopAnchors(List<Vector2> anchors)
    {
        int start = 0;
        int end = 0;

        for (int i = 0; i < anchors.Count; i++)
        {
            end++;

            if (i != anchors.Count - 1 && anchors[i] != anchors[i + 1] || i == anchors.Count - 2) continue;

            var cpSpan = anchors.GetRange(start, end - start);
            var subdivision = new BezierSubdivision(cpSpan);

            yield return subdivision;

            start = end;
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
            var subdivision = new BezierSubdivision(new List<Vector2> { anchors[i - 1], anchors[i] });
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
