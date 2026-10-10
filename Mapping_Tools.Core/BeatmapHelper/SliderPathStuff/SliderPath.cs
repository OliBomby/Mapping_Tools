// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;

/// <summary>
///     Lazily approximates an osu! slider curve and applies its optional serialized pixel-length constraint.
/// </summary>
public struct SliderPath : IEquatable<SliderPath>
{
    /// <summary>
    ///     The user-set distance of the path. If non-null, <see cref="Distance" /> will match this value,
    ///     and the path will be shortened/lengthened to match this length.
    /// </summary>
    public readonly double? ExpectedDistance;

    /// <summary>
    ///     The type of the first path segment.
    /// </summary>
    public readonly PathType Type;

    private PathControlPoint[] controlPoints;

    private List<Vector2> calculatedPath;
    private List<double> cumulativeLength;
    private List<int> segmentStarts;

    private bool isInitialised;

    /// <summary>
    ///     Creates a new <see cref="SliderPath" />.
    /// </summary>
    /// <param name="type">The type of path.</param>
    /// <param name="controlPoints">The control points of the path.</param>
    /// <param name="expectedDistance">
    ///     A user-set distance of the path that may be shorter or longer than the true distance between all
    ///     <paramref name="controlPoints" />. The path will be shortened/lengthened to match this length.
    ///     If null, the path will use the true distance between all <paramref name="controlPoints" />.
    /// </param>
    public SliderPath(PathType type, Vector2[] controlPoints, double? expectedDistance = null)
        : this(controlPoints.Select((point, index) => new PathControlPoint(point, index == 0 ? type : null)).ToArray(), expectedDistance)
    {
    }

    /// <summary>Creates a path with independently typed segments.</summary>
    /// <param name="controlPoints">The ordered path control points.</param>
    /// <param name="expectedDistance">Optional target length.</param>
    public SliderPath(PathControlPoint[] controlPoints, double? expectedDistance = null)
    {
        this.controlPoints = controlPoints.Select(point => point.Copy()).ToArray();
        calculatedPath = [];
        cumulativeLength = [];
        segmentStarts = [];
        isInitialised = false;

        Type = controlPoints.FirstOrDefault()?.Type ?? PathType.Linear;
        ExpectedDistance = expectedDistance;

        EnsureInitialised();
    }

    /// <summary>
    ///     The control points of the path.
    /// </summary>
    public List<Vector2> ControlPoints
    {
        get
        {
            EnsureInitialised();
            return controlPoints.Select(point => point.Position).ToList();
        }
    }

    /// <summary>The typed control points of the path.</summary>
    public IReadOnlyList<PathControlPoint> PathControlPoints
    {
        get
        {
            EnsureInitialised();
            return controlPoints.Select(point => point.Copy()).ToArray();
        }
    }

    /// <summary>
    ///     The distance of the path after lengthening/shortening to account for <see cref="ExpectedDistance" />.
    /// </summary>
    public double Distance
    {
        get
        {
            EnsureInitialised();
            return cumulativeLength.Count == 0 ? 0 : cumulativeLength[^1];
        }
    }

    /// <summary>
    ///     Gets the polyline approximation after truncation or extension to <see cref="ExpectedDistance" />.
    /// </summary>
    public IReadOnlyList<Vector2> CalculatedPath
    {
        get
        {
            EnsureInitialised();
            return calculatedPath;
        }
    }

    /// <summary>
    ///     Gets cumulative polyline distance for each corresponding <see cref="CalculatedPath" /> point.
    /// </summary>
    public IReadOnlyList<double> CumulativeLength
    {
        get
        {
            EnsureInitialised();
            return cumulativeLength;
        }
    }

    /// <summary>
    ///     Gets polyline indexes at which red-anchor-delimited curve segments begin.
    /// </summary>
    public IReadOnlyList<int> SegmentStarts
    {
        get
        {
            EnsureInitialised();
            return segmentStarts;
        }
    }

    /// <summary>
    ///     Computes the slider path until a given progress that ranges from 0 (beginning of the slider)
    ///     to 1 (end of the slider) and stores the generated path in the given list.
    /// </summary>
    /// <param name="path">The list to be filled with the computed path.</param>
    /// <param name="p0">Start progress. Ranges from 0 (beginning of the slider) to 1 (end of the slider).</param>
    /// <param name="p1">End progress. Ranges from 0 (beginning of the slider) to 1 (end of the slider).</param>
    public void GetPathToProgress(List<Vector2> path, double p0, double p1)
    {
        EnsureInitialised();

        double d0 = ProgressToDistance(p0);
        double d1 = ProgressToDistance(p1);

        path.Clear();

        int i = 0;
        for (; i < calculatedPath.Count && cumulativeLength[i] < d0; ++i)
        {
        }

        path.Add(InterpolateVertices(i, d0));

        for (; i < calculatedPath.Count && cumulativeLength[i] <= d1; ++i)
            path.Add(calculatedPath[i]);

        path.Add(InterpolateVertices(i, d1));
    }

    /// <summary>
    ///     Computes the position on the slider at a given progress that ranges from 0 (beginning of the path)
    ///     to 1 (end of the path).
    /// </summary>
    /// <param name="progress">Ranges from 0 (beginning of the path) to 1 (end of the path).</param>
    /// <returns></returns>
    public Vector2 PositionAt(double progress)
    {
        EnsureInitialised();

        double d = ProgressToDistance(progress);
        return InterpolateVertices(IndexOfDistance(d), d);
    }

    /// <summary>
    ///     Computes the position of the sliderball on the slider at a given ms
    ///     that ranges from 0 (beginning of the path) to timeLength (end of the path).
    /// </summary>
    /// <param name="ms">Ranges from 0 (beginning of the path) to timeLength (end of the path).</param>
    /// <param name="timeLength"> Indicates the ms duration of the slider, using the slider velocity.</param>
    /// <returns></returns>
    public Vector2 SliderballPositionAt(int ms, int timeLength)
    {
        EnsureInitialised();
        return PositionAt(timeLength <= 0 ? 0 : (double)ms / timeLength);
    }

    /// <summary>Calculates osu!stable's nominal tail judgement timestamp.</summary>
    /// <param name="startTime">The integer slider start timestamp in milliseconds.</param>
    /// <param name="velocity">Slider velocity in pixels per second.</param>
    /// <param name="spanCount">The number of traversals of the path.</param>
    /// <returns>The truncated final scoring time minus 36 ms, bounded by the integer midpoint.</returns>
    /// <remarks>
    /// Uses the calculated polyline, float segment lengths, and accumulated segment durations.
    /// This is stable's fixed tail tick, not lazer's continuous tracking and tail leniency.
    /// Actual gameplay evaluates tracking on the update which reaches this timestamp.
    /// </remarks>
    public int GetLegacyTrueSliderEndTime(int startTime, double velocity, int spanCount)
    {
        double endTime = startTime;
        double distance = 0;
        foreach (var segment in GetLegacySliderSegments(startTime, velocity, spanCount))
        {
            endTime = segment.EndTime;
            distance += segment.Distance;
        }

        if (distance <= 0) return startTime;

        // Stable calculates EndTime and the last scoring timestamp separately.
        int duration = (int)endTime - startTime;
        int scoringTime = (int)(startTime + (float)distance / velocity * 1000.0);
        return Math.Max(startTime + duration / 2, scoringTime - 36);
    }

    /// <summary>Calculates the slider ball position used by osu!stable's cursor tracking check.</summary>
    /// <param name="time">The integer timestamp to sample, in milliseconds.</param>
    /// <param name="startTime">The integer slider start timestamp in milliseconds.</param>
    /// <param name="velocity">Slider velocity in pixels per second.</param>
    /// <param name="spanCount">The number of traversals of the path.</param>
    /// <returns>The absolute position on the calculated path.</returns>
    /// <remarks>
    /// Truncates each polyline segment's start and end timestamps, then interpolates using floats.
    /// At shared boundaries the first matching segment wins; zero-duration segments use their endpoint.
    /// At the exact start of an invisible slider this can differ from the rendered ball, which uses the start.
    /// Before the slider starts, returns its start; after it ends, extrapolates its final movement as stable does.
    /// Lazer uses continuous path progress instead. This also differs from stable's PositionAtTime helper.
    /// </remarks>
    public Vector2 GetLegacySliderBallPositionAtTime(int time, int startTime, double velocity, int spanCount)
    {
        Vector2 position = PositionAt(0);
        if (time < startTime) return position;

        (Vector2 Start, Vector2 End, double StartTime, double EndTime, float Distance)? lastSegment = null;
        foreach (var segment in GetLegacySliderSegments(startTime, velocity, spanCount))
        {
            int segmentStart = (int)segment.StartTime;
            int segmentEnd = (int)segment.EndTime;
            if (segmentStart <= time && segmentEnd >= time)
                return InterpolateLegacySliderSegment(segment.Start, segment.End, segmentStart, segmentEnd, time);

            lastSegment = segment;
        }

        // Stable keeps using the final movement when a judgement update arrives after it ends.
        return lastSegment is { } last
            ? InterpolateLegacySliderSegment(last.Start, last.End, (int)last.StartTime, (int)last.EndTime, time)
            : position;
    }

    private static Vector2 InterpolateLegacySliderSegment(Vector2 start, Vector2 end, int startTime, int endTime, int time)
    {
        if (startTime == endTime) return end;

        float progress = (float)(1.0 - (double)(float)(endTime - time) / (float)(endTime - startTime));
        return new Vector2(
            (float)start.X + ((float)end.X - (float)start.X) * progress,
            (float)start.Y + ((float)end.Y - (float)start.Y) * progress);
    }

    private IEnumerable<(Vector2 Start, Vector2 End, double StartTime, double EndTime, float Distance)>
        GetLegacySliderSegments(int startTime, double velocity, int spanCount)
    {
        EnsureInitialised();
        if (!double.IsFinite(velocity) || velocity <= 0 || spanCount <= 0 || calculatedPath.Count < 2) yield break;

        var lines = GetLegacyPathSegments();
        double[] lengths = new double[lines.Length + 1];
        for (int i = 0; i < lines.Length; i++)
            lengths[i + 1] = lengths[i] + lines[i].Distance;

        double currentTime = startTime;
        for (int span = 0; span < spanCount; span++)
        {
            bool reversed = span % 2 == 1;
            for (int step = 0; step < lines.Length; step++)
            {
                int index = reversed ? lines.Length - step - 1 : step;
                var start = reversed ? lines[index].End : lines[index].Start;
                var end = reversed ? lines[index].Start : lines[index].End;
                float distance = (float)(lengths[index + 1] - lengths[index]);
                // Stable's 32-bit runtime keeps this multiplication at extended precision.
                // Do not round the product to float before dividing by velocity.
                double endTime = currentTime + 1000.0 * distance / velocity;

                yield return (start, end, currentTime, endTime, distance);
                currentTime = endTime;
            }
        }
    }

    private (Vector2 Start, Vector2 End, float Distance)[] GetLegacyPathSegments()
    {
        // Stable keeps duplicate linear anchors. A duplicate tail prevents extension and is
        // essential to Invisiblator's end snapping; the general path approximation removes it.
        bool linear = Type == PathType.Linear && controlPoints.All(point => point.Type is null or PathType.Linear);
        var points = linear
            ? controlPoints.Select(point => new System.Numerics.Vector2(
                (float)Math.Round(point.Position.X), (float)Math.Round(point.Position.Y))).ToArray()
            : calculatedPath.Select(point => new System.Numerics.Vector2((float)point.X, (float)point.Y)).ToArray();
        List<(System.Numerics.Vector2 Start, System.Numerics.Vector2 End)> lines = [];
        for (int i = 1; i < points.Length; i++)
        {
            // Stable's linear subdivision computes the endpoint even with just one subdivision.
            // At Invisiblator coordinates, subtracting then adding floats can change that endpoint.
            var end = linear ? points[i - 1] + (points[i] - points[i - 1]) : points[i];
            lines.Add((points[i - 1], end));
        }

        double total = 0;
        foreach (var line in lines)
            total += LegacyLineLength(line.End - line.Start);

        double excess = total - (linear ? ExpectedDistance ?? total : total);
        while (linear && ExpectedDistance.HasValue && lines.Count > 0)
        {
            var last = lines[^1];
            var delta = last.End - last.Start;
            if (delta.Length() > excess + 0.0001)
            {
                if (delta != System.Numerics.Vector2.Zero)
                    lines[^1] = (last.Start, last.Start + System.Numerics.Vector2.Normalize(delta) * (LegacyLineLength(delta) - (float)excess));
                break;
            }

            excess -= delta.Length();
            lines.RemoveAt(lines.Count - 1);
        }

        return lines.Select(line => (new Vector2(line.Start.X, line.Start.Y), new Vector2(line.End.X, line.End.Y),
            LegacyLineLength(line.End - line.Start))).ToArray();
    }

    private static float LegacyLineLength(System.Numerics.Vector2 delta)
    {
        return (float)Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y);
    }

    /// <summary>
    ///     Computes the position of the sliderball on the slider at all ms from 0 to timeLength.
    /// </summary>
    /// <param name="timeLength"> Indicates the ms duration of the slider, using the slider velocity.</param>
    /// <returns>
    ///     A Vector2 array such that the index i contains the position of the sliderball at the ith ms.
    /// </returns>
    public Vector2[] SliderballPositions(int timeLength)
    {
        EnsureInitialised();

        var sbPositions = new Vector2[timeLength + 1];
        for (int i = 0; i < timeLength + 1; i++)
            sbPositions[i] = SliderballPositionAt(i, timeLength);

        return sbPositions;
    }

    private void EnsureInitialised()
    {
        if (isInitialised)
            return;
        isInitialised = true;

        controlPoints = controlPoints ?? Array.Empty<PathControlPoint>();
        calculatedPath = new List<Vector2>();
        cumulativeLength = new List<double>();
        segmentStarts = new List<int>();

        CalculatePath();
        CalculateCumulativeLength();
    }

    private List<Vector2> CalculateSubpath(List<Vector2> subControlPoints, PathType type)
    {
        switch (type)
        {
            case PathType.Linear:
                return PathApproximator.ApproximateLinear(subControlPoints);
            case PathType.PerfectCurve:
                //we can only use CircularArc iff we have exactly three control points and no dissection.
                if (subControlPoints.Count != 3 ||
                    controlPoints.Length != 3 && controlPoints.Skip(1).All(point => !point.Type.HasValue))
                    break;

                // Here we have exactly 3 control points. Attempt to fit a circular arc.
                var subpath = PathApproximator.ApproximateCircularArc(subControlPoints);

                // If for some reason a circular arc could not be fit to the 3 given points, fall back to a numerically stable bezier approximation.
                if (subpath.Count == 0)
                    break;

                return subpath;
            case PathType.Catmull:
                return PathApproximator.ApproximateCatmull(subControlPoints);
            case PathType.BSpline:
                return PathApproximator.ApproximateBSpline(subControlPoints, 4);
        }

        return PathApproximator.ApproximateBezier(subControlPoints);
    }

    private void CalculatePath()
    {
        calculatedPath.Clear();

        // Typed control points start independent subpaths at their position.

        int start = 0;
        PathType activeType = Type;

        for (int i = 0; i < controlPoints.Length; ++i)
        {
            bool typedBoundary = i > start && controlPoints[i].Type.HasValue;
            if (i == controlPoints.Length - 1 || typedBoundary)
            {
                var cpSpan = controlPoints.Skip(start).Take(i - start + 1).Select(point => point.Position).ToList();

                // Remember the index of the subpath start
                segmentStarts.Add(calculatedPath.Count);

                foreach (var t in CalculateSubpath(cpSpan, activeType))
                    if (calculatedPath.Count == 0 || calculatedPath.Last() != t)
                        calculatedPath.Add(t);

                if (typedBoundary) activeType = controlPoints[i].Type!.Value;
                start = i;
            }
        }
    }

    private void CalculateCumulativeLength()
    {
        double l = 0;

        cumulativeLength.Clear();
        cumulativeLength.Add(l);

        for (int i = 0; i < calculatedPath.Count - 1; ++i)
        {
            var diff = calculatedPath[i + 1] - calculatedPath[i];
            double d = diff.Length;

            // Shorted slider paths that are too long compared to the expected distance
            if (ExpectedDistance.HasValue && ExpectedDistance - l < d)
            {
                calculatedPath[i + 1] = calculatedPath[i] + diff * (double)((ExpectedDistance - l) / d);
                calculatedPath.RemoveRange(i + 2, calculatedPath.Count - 2 - i);

                l = ExpectedDistance.Value;
                cumulativeLength.Add(l);
                break;
            }

            l += d;
            cumulativeLength.Add(l);
        }

        // Lengthen slider paths that are too short compared to the expected distance
        if (ExpectedDistance.HasValue && l < ExpectedDistance && calculatedPath.Count > 1)
        {
            var diff = calculatedPath[^1] - calculatedPath[^2];
            double d = diff.Length;

            if (d <= 0)
                return;

            calculatedPath[^1] += diff * (double)((ExpectedDistance - l) / d);
            cumulativeLength[calculatedPath.Count - 1] = ExpectedDistance.Value;
        }
    }

    private int IndexOfDistance(double d)
    {
        int i = cumulativeLength.BinarySearch(d);
        if (i < 0)
            i = ~i;

        return i;
    }

    private double ProgressToDistance(double progress)
    {
        return MathHelper.Clamp(progress, 0, 1) * Distance;
    }

    private Vector2 InterpolateVertices(int i, double d)
    {
        if (calculatedPath.Count == 0)
            return Vector2.Zero;

        if (i <= 0)
            return calculatedPath.First();
        if (i >= calculatedPath.Count)
            return calculatedPath.Last();

        var p0 = calculatedPath[i - 1];
        var p1 = calculatedPath[i];

        double d0 = cumulativeLength[i - 1];
        double d1 = cumulativeLength[i];

        // Avoid division by and almost-zero number in case two points are extremely close to each other.
        if (Precision.AlmostEquals(d0, d1))
            return p0;

        double w = (d - d0) / (d1 - d0);
        return p0 + (p1 - p0) * w;
    }

    /// <summary>
    ///     Compares serialized curve identity: path type, requested distance, and ordered control points.
    /// </summary>
    /// <param name="other">The path to compare.</param>
    /// <returns><see langword="true" /> when the inputs that define both paths match.</returns>
    public bool Equals(SliderPath other)
    {
        if (ControlPoints == null && other.ControlPoints != null)
            return false;
        if (other.ControlPoints == null && ControlPoints != null)
            return false;

        return controlPoints.Select(point => (point.Position, point.Type))
                   .SequenceEqual(other.controlPoints.Select(point => (point.Position, point.Type))) &&
               ExpectedDistance.Equals(other.ExpectedDistance);
    }

    /// <summary>
    ///     Determines whether an object is a slider path with equal serialized curve identity.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> for an equal <see cref="SliderPath" />.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        return obj is SliderPath other && Equals(other);
    }

    /// <summary>
    ///     Applies the == operator.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true" /> when the serialized curve inputs match.</returns>
    public static bool operator ==(SliderPath left, SliderPath right) => left.Equals(right);

    /// <summary>
    ///     Applies the != operator.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns><see langword="true" /> when the serialized curve inputs differ.</returns>
    public static bool operator !=(SliderPath left, SliderPath right) => !left.Equals(right);

    /// <summary>
    ///     Combines requested distance, path type, and control-point array identity into a hash.
    /// </summary>
    /// <returns>A hash code for this path value.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(ExpectedDistance);
        hashCode.Add(Type);
        if (controlPoints is not null)
            foreach (var point in controlPoints)
            {
                hashCode.Add(point.Position);
                hashCode.Add(point.Type);
            }

        return hashCode.ToHashCode();
    }
}
