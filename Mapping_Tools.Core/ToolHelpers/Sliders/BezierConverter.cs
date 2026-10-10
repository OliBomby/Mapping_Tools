using System.Globalization;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders;

/// <summary>
///     Converts slider segments of all types to bezier type.
/// </summary>
public static class BezierConverter
{
    private static readonly List<CircleBezierPreset> circlePresets =
    [
        new(0.4993379862754501,
            GetPoints("1.0:0.0|1.0:0.2549893626632736|0.8778997558480327:0.47884446188920726")),

        new(1.7579419829169447,
            GetPoints("1.0:0.0|1.0:0.6263026|0.42931178:1.0990661|-0.18605515:0.9825393")),

        new(3.1385246920140215,
            GetPoints("1.0:0.0|1.0:0.87084764|0.002304826:1.5033062|-0.9973236:0.8739115|-0.9999953:0.0030679568")),

        new(5.69720464620727,
            GetPoints(
                "1.0:0.0|1.0:1.4137783|-1.4305235:2.0779421|-2.3410065:-0.94017583|0.05132711:-1.7309346|0.8331702:-0.5530167")),

        new(2 * Math.PI,
            GetPoints(
                "1.0:0.0|1.0:1.2447058|-0.8526471:2.118367|-2.6211002:7.854936e-06|-0.8526448:-2.118357|1.0:-1.2447058|1.0:-2.4492937e-16")),
    ];

    private static List<Vector2> GetPoints(string str)
    {
        string[] strPoints = str.Split('|');
        var points = new List<Vector2>(strPoints.Length);
        foreach (string strPoint in strPoints)
        {
            string[] strCoords = strPoint.Split(':');
            points.Add(new Vector2(float.Parse(strCoords[0], CultureInfo.InvariantCulture),
                float.Parse(strCoords[1], CultureInfo.InvariantCulture)));
        }

        return points;
    }

    /// <summary>
    ///     Converts sliderpath to a bezier sliderpath with the same shape.
    /// </summary>
    /// <param name="sliderPath">The path to convert.</param>
    /// <returns>A Bézier path retaining the source expected distance and typed segment boundaries.</returns>
    public static SliderPath ConvertToBezier(SliderPath sliderPath)
    {
        var result = ConvertToBezierAnchors(sliderPath.PathControlPoints);
        return new SliderPath(result.ToArray(), sliderPath.ExpectedDistance);
    }

    /// <summary>Converts every typed path segment to typed Bézier control points.</summary>
    /// <param name="controlPoints">The source control points.</param>
    /// <returns>Control points in the same coordinate system with explicit Bézier segment boundaries.</returns>
    public static List<PathControlPoint> ConvertToBezierAnchors(IReadOnlyList<PathControlPoint> controlPoints)
    {
        var result = new List<PathControlPoint>();
        int start = 0;
        PathType type = controlPoints.FirstOrDefault()?.Type ?? PathType.Linear;
        for (int index = 0; index < controlPoints.Count; index++)
        {
            bool boundary = index > start && controlPoints[index].Type.HasValue;
            if (index != controlPoints.Count - 1 && !boundary) continue;

            var positions = controlPoints.Skip(start).Take(index - start + 1)
                .Select(point => point.Position).ToList();
            var converted = ConvertToBezierAnchors(positions, type);
            if (result.Count == 0) result.AddRange(converted);
            else
            {
                result[^1].Type = PathType.Bezier;
                if (converted.Count == 1)
                    result.Add(new PathControlPoint(converted[0].Position, PathType.Bezier));
                else
                    result.AddRange(converted.Skip(1));
            }

            if (boundary) type = controlPoints[index].Type!.Value;
            start = index;
        }

        return result;
    }

    /// <summary>Converts one path segment to typed Bézier control points.</summary>
    /// <param name="anchors">The positions in the source segment.</param>
    /// <param name="type">The source segment type.</param>
    /// <returns>Bézier control points with explicit boundaries.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The path type is unknown.</exception>
    public static List<PathControlPoint> ConvertToBezierAnchors(List<Vector2> anchors, PathType type)
    {
        if (anchors.Count < 2)
            return anchors.Select(point => new PathControlPoint(point, PathType.Bezier)).ToList();

        var converted = type switch
        {
            PathType.Linear => ConvertLinearToBezierAnchors(anchors),
            PathType.PerfectCurve => anchors.Count == 3 ? ConvertCircleToBezierAnchors(anchors) : ToTypedBezier(anchors),
            PathType.Catmull => ConvertCatmullToBezierAnchors(anchors),
            PathType.Bezier => ToTypedBezier(anchors),
            PathType.BSpline => ConvertBSplineToBezierAnchors(anchors),
            _ => throw new ArgumentOutOfRangeException(),
        };

        converted[0].Position = anchors[0];
        converted[^1].Position = anchors[^1];
        return converted;
    }

    private static List<PathControlPoint> ToTypedBezier(IReadOnlyList<Vector2> anchors) =>
        anchors.Select((position, index) => new PathControlPoint(position, index == 0 ? PathType.Bezier : null)).ToList();

    /// <summary>
    ///     Converts a perfect-curve path to Bézier segments; non-perfect paths are returned unchanged.
    /// </summary>
    /// <param name="perfectPath"></param>
    /// <returns></returns>
    public static SliderPath ConvertCircleToBezier(SliderPath perfectPath)
    {
        if (perfectPath.Type != PathType.PerfectCurve) return perfectPath;

        return ConvertToBezier(perfectPath);
    }

    /// <summary>
    ///     Approximates a stable circular arc with one or more preset Bézier segments.
    /// </summary>
    /// <param name="ca"></param>
    /// <returns></returns>
    public static SliderPath ConvertCircleToBezier(CircleArc ca)
    {
        var anchors = ConvertCircleToBezierAnchors(ca);
        return new SliderPath(anchors.ToArray());
    }

    /// <summary>
    ///     Creates a Bézier path from three perfect-curve anchors.
    /// </summary>
    /// <param name="perfectAnchors"></param>
    /// <returns></returns>
    public static SliderPath ConvertCircleToBezier(List<Vector2> perfectAnchors)
    {
        return ConvertToBezier(new SliderPath(PathType.PerfectCurve, perfectAnchors.ToArray()));
    }

    /// <summary>
    ///     Converts three perfect-curve anchors into Bézier control points, preserving unstable input unchanged.
    /// </summary>
    /// <param name="perfectAnchors">The three positions defining the arc.</param>
    /// <returns>Typed Bézier control points, or the original positions marked as Bézier when the arc is unstable.</returns>
    public static List<PathControlPoint> ConvertCircleToBezierAnchors(List<Vector2> perfectAnchors)
    {
        var cs = new CircleArc(perfectAnchors);
        if (!cs.Stable)
            return ToTypedBezier(perfectAnchors);

        var converted = ConvertCircleToBezierAnchors(cs);
        converted[0].Position = perfectAnchors[0];
        converted[^1].Position = perfectAnchors[^1];
        return converted;
    }

    /// <summary>
    ///     Maps a stable circle arc onto the smallest preset whose angular tolerance covers its sweep.
    /// </summary>
    /// <param name="cs">The stable circular arc to approximate.</param>
    /// <returns>Typed Bézier control points approximating the arc.</returns>
    public static List<PathControlPoint> ConvertCircleToBezierAnchors(CircleArc cs)
    {
        var preset = circlePresets.Last();
        foreach (var CBP in circlePresets)
            if (CBP.MaxAngle >= cs.ThetaRange)
            {
                preset = CBP;
                break;
            }

        var arc = preset.Points.Copy();
        double arcLength = preset.MaxAngle;

        // Converge on arcLength of thetaRange
        int n = arc.Count - 1;
        double tf = cs.ThetaRange / arcLength;
        while (Math.Abs(tf - 1) > 0.0000001)
        {
            for (int j = 0; j < n; j++)
            for (int i = n; i > j; i--)
                arc[i] = arc[i] * tf + arc[i - 1] * (1 - tf);

            arcLength = Math.Atan2(arc.Last()[1], arc.Last()[0]);
            if (arcLength < 0) arcLength += 2 * Math.PI;

            tf = cs.ThetaRange / arcLength;
        }

        // Adjust rotation, radius, and position
        var rotator = cs.Rotator;
        for (int i = 0; i < arc.Count; i++) arc[i] = Matrix2.Mult(rotator, arc[i]) + cs.Centre;

        return ToTypedBezier(arc);
    }

    /// <summary>
    ///     Converts a Catmull path to joined cubic Bézier segments; other path types are returned unchanged.
    /// </summary>
    /// <param name="catmullPath"></param>
    /// <returns></returns>
    public static SliderPath ConvertCatmullToBezier(SliderPath catmullPath)
    {
        if (catmullPath.Type != PathType.Catmull) return catmullPath;

        return ConvertToBezier(catmullPath);
    }

    private static List<PathControlPoint> ConvertBSplineToBezierAnchors(List<Vector2> bsplineAnchors)
    {
        var bsplinePath = new SliderPath(PathType.BSpline, [.. bsplineAnchors]);
        return ConvertLinearToBezierAnchors(bsplinePath.CalculatedPath.ToList());
    }

    /// <summary>
    ///     Creates a Bézier path from Catmull control points.
    /// </summary>
    /// <param name="catmullAnchors"></param>
    /// <returns></returns>
    public static SliderPath ConvertCatmullToBezier(List<Vector2> catmullAnchors)
    {
        return ConvertToBezier(new SliderPath(PathType.Catmull, catmullAnchors.ToArray()));
    }

    /// <summary>
    ///     Converts each Catmull span into a cubic Bezier segment with a typed boundary at each join.
    /// </summary>
    /// <param name="pts">The Catmull control positions.</param>
    /// <returns>Joined cubic control points with typed Bézier segment starts.</returns>
    public static List<PathControlPoint> ConvertCatmullToBezierAnchors(List<Vector2> pts)
    {
        var cubics = new List<PathControlPoint> { new(pts[0], PathType.Bezier) };
        int iLen = pts.Count;
        for (int i = 0; i < iLen - 1; i++)
        {
            var v1 = i > 0 ? pts[i - 1] : pts[i];
            var v2 = pts[i];
            var v3 = pts[i + 1];
            var v4 = i < iLen - 2 ? pts[i + 2] : v3 + v3 - v2;

            cubics.Add(new PathControlPoint((-v1 + 6 * v2 + v3) / 6));
            cubics.Add(new PathControlPoint((-v4 + 6 * v3 + v2) / 6));
            cubics.Add(new PathControlPoint(v3, i < iLen - 2 ? PathType.Bezier : null));
        }

        return cubics;
    }

    /// <summary>
    ///     Converts a linear path to first-order Bézier segments; other path types are returned unchanged.
    /// </summary>
    /// <param name="linearPath"></param>
    /// <returns></returns>
    public static SliderPath ConvertLinearToBezier(SliderPath linearPath)
    {
        if (linearPath.Type != PathType.Linear) return linearPath;

        return ConvertToBezier(linearPath);
    }

    /// <summary>
    ///     Creates a Bézier path from polyline anchors.
    /// </summary>
    /// <param name="linearAnchors"></param>
    /// <returns></returns>
    public static SliderPath ConvertLinearToBezier(List<Vector2> linearAnchors)
    {
        return ConvertToBezier(new SliderPath(PathType.Linear, linearAnchors.ToArray()));
    }

    /// <summary>
    ///     Marks each interior polyline vertex as the start of the next Bezier segment.
    /// </summary>
    /// <param name="pts">The polyline vertices.</param>
    /// <returns>Linear positions with typed Bézier segment starts at each interior vertex.</returns>
    public static List<PathControlPoint> ConvertLinearToBezierAnchors(List<Vector2> pts)
    {
        return pts.Select((position, index) => new PathControlPoint(position,
            index < pts.Count - 1 ? PathType.Bezier : null)).ToList();
    }

    private struct CircleBezierPreset
    {
        /// <summary>
        ///     The largest circle-arc sweep for which this preset meets the approximation tolerance.
        /// </summary>
        public readonly double MaxAngle;

        /// <summary>
        ///     Unit-circle Bézier control points transformed onto the requested arc.
        /// </summary>
        public readonly List<Vector2> Points;

        /// <summary>
        ///     Associates an angular threshold with a unit-circle control polygon.
        /// </summary>
        /// <param name="maxAngle">The max angle.</param>
        /// <param name="points">Unit-circle Bézier control points for the preset arc.</param>
        public CircleBezierPreset(double maxAngle, List<Vector2> points)
        {
            MaxAngle = maxAngle;
            Points = points;
        }
    }
}
