using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Mapping_Tools.Core.Tools.SliderMerger.Models;

namespace Mapping_Tools.Core.Tools.SliderMerger;

/// <summary>
///     Merges selected circles and sliders while retaining their typed path segments.
/// </summary>
public static class SliderMergerEngine
{
    /// <summary>
    ///     Merges adjacent supported objects in their supplied order.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap whose hit-object list is changed.</param>
    /// <param name="markedObjects">The selected, bookmarked, time-filtered, or complete object sequence.</param>
    /// <param name="options">The connection and geometry settings.</param>
    /// <param name="progress">Optional normalized progress receiver.</param>
    /// <param name="cancellationToken">Cancels between object-pair evaluations.</param>
    /// <returns>The number of source objects incorporated into merged sliders.</returns>
    /// <exception cref="ArgumentException">The leniency or connection mode is invalid.</exception>
    public static int Merge(
        Beatmap beatmap,
        IReadOnlyList<HitObject> markedObjects,
        SliderMergerEngineOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(markedObjects);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);

        var objects = markedObjects.ToList();
        int mergedObjects = 0;
        bool mergedWithPrevious = false;

        for (int index = 0; index < objects.Count - 1; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(index, objects.Count);

            var first = objects[index];
            var second = objects[index + 1];
            var firstConnection = first.IsSlider
                ? options.MergeOnSliderEnd
                    ? first.GetSliderPath().PositionAt(1)
                    : first.GetAbsoluteControlPointPositions().Last()
                : first.Pos;
            double distance = Vector2.Distance(firstConnection, second.Pos);

            if (distance > options.Leniency || !(first.IsSlider || first.IsCircle) || !(second.IsSlider || second.IsCircle))
            {
                mergedWithPrevious = false;
                continue;
            }

            var survivor = (first.IsSlider, second.IsSlider) switch
            {
                (true, true) => MergeSliders(first, second, options),
                (true, false) => MergeSliderAndCircle(first, second, options),
                (false, true) => MergeCircleAndSlider(first, second, options),
                _ => MergeCircles(first, second, options),
            };

            var removed = ReferenceEquals(survivor, first) ? second : first;
            beatmap.HitObjects.Remove(removed);
            objects.Remove(removed);
            index--;

            mergedObjects++;
            if (!mergedWithPrevious) mergedObjects++;

            mergedWithPrevious = true;

            // Preserve the legacy hidden geometry easter egg for existing projects.
            if (Precision.AlmostEquals(options.Leniency, 727))
            {
                var shape = MakePenis(survivor.GetAbsoluteControlPointPositions(), survivor.PixelLength);
                survivor.Pos = shape[0].Position;
                survivor.ControlPoints = shape.Select(point =>
                    new PathControlPoint(point.Position - survivor.Pos, point.Type)).ToList();
                survivor.PixelLength *= 2;
            }
        }

        progress?.Report(1);
        return mergedObjects;
    }

    /// <summary>
    ///     Determines whether each typed Bézier segment is a straight edge.
    /// </summary>
    /// <param name="points">The typed Bézier control points.</param>
    /// <returns><see langword="true" /> when every segment has exactly two points.</returns>
    public static bool IsLinearBezier(IReadOnlyList<PathControlPoint> points)
    {
        int start = 0;
        for (int index = 1; index < points.Count; index++)
        {
            if (!points[index].Type.HasValue && index != points.Count - 1) continue;
            if (index - start != 1) return false;
            start = index;
        }

        return true;
    }

    /// <summary>Moves every control point by the supplied delta.</summary>
    /// <param name="points">The mutable control-point list.</param>
    /// <param name="delta">The translation in osu! playfield pixels.</param>
    public static void Move(IList<Vector2> points, Vector2 delta)
    {
        for (int index = 0; index < points.Count; index++) points[index] += delta;
    }

    private static HitObject MergeSliders(
        HitObject first,
        HitObject second,
        SliderMergerEngineOptions options)
    {
        if (options.MergeOnSliderEnd)
        {
            MoveLastAnchorToSliderEnd(first);
        }

        var firstPath = first.ControlPoints;
        var secondPath = second.ControlPoints;
        double extraLength = 0;
        Vector2 join = first.Pos + firstPath[^1].Position;
        bool linear = options.LinearOnLinear && IsEntirelyLinear(first) && IsEntirelyLinear(second);

        switch (options.ConnectionModeSetting)
        {
            case SliderMergerConnectionMode.Move:
                if (!linear) firstPath[^1].Type = secondPath[0].Type;
                break;
            case SliderMergerConnectionMode.Linear:
                extraLength = (join - second.Pos).Length;
                if (!linear) firstPath[^1].Type = PathType.Linear;
                firstPath.Add(new PathControlPoint(second.Pos - first.Pos, linear ? null : secondPath[0].Type));
                break;
            default:
                throw new ArgumentException("Unexpected slider connection mode.", nameof(options));
        }

        Vector2 offset = options.ConnectionModeSetting == SliderMergerConnectionMode.Move ? join - second.Pos : Vector2.Zero;
        foreach (var point in secondPath.Skip(1))
            firstPath.Add(new PathControlPoint(second.Pos + offset + point.Position - first.Pos, linear ? null : point.Type));

        if (linear) RemoveDuplicateAnchors(firstPath);
        first.PixelLength = first.PixelLength + second.PixelLength + extraLength;
        first.Repeat = 1;
        return first;
    }

    private static HitObject MergeSliderAndCircle(
        HitObject first,
        HitObject second,
        SliderMergerEngineOptions options)
    {
        var path = first.ControlPoints;
        bool linear = options.LinearOnLinear && IsEntirelyLinear(first);
        double extraLength = (first.Pos + path[^1].Position - second.Pos).Length;
        if (!linear) path[^1].Type = PathType.Linear;
        path.Add(new PathControlPoint(second.Pos - first.Pos));
        if (linear) RemoveDuplicateAnchors(path);

        first.PixelLength += extraLength;
        first.Repeat = 1;
        return first;
    }

    private static HitObject MergeCircleAndSlider(
        HitObject first,
        HitObject second,
        SliderMergerEngineOptions options)
    {
        var path = second.ControlPoints;
        bool linear = options.LinearOnLinear && IsEntirelyLinear(second);
        Vector2 secondStart = second.Pos;
        var merged = new List<PathControlPoint> { new(Vector2.Zero, PathType.Linear) };
        merged.Add(new PathControlPoint(secondStart - first.Pos, linear ? null : path[0].Type));
        merged.AddRange(path.Skip(1).Select(point => new PathControlPoint(secondStart + point.Position - first.Pos, linear ? null : point.Type)));
        double extraLength = (first.Pos - second.Pos).Length;
        if (linear) RemoveDuplicateAnchors(merged);

        second.Pos = first.Pos;
        second.ControlPoints = merged;
        second.PixelLength += extraLength;
        second.Repeat = 1;
        return second;
    }

    private static HitObject MergeCircles(
        HitObject first,
        HitObject second,
        SliderMergerEngineOptions options)
    {
        if (Precision.DefinitelyBigger(Vector2.Distance(first.Pos, second.Pos), 0))
        {
            first.ControlPoints =
            [
                new PathControlPoint(Vector2.Zero, options.LinearOnLinear ? PathType.Linear : PathType.Bezier),
                new PathControlPoint(second.Pos - first.Pos),
            ];
            first.PixelLength = (first.Pos - second.Pos).Length;
            first.IsCircle = false;
            first.IsSlider = true;
            first.Repeat = 1;
            SetEndpointEdges(first, GetHeadEdges(first), GetTailEdges(second));
        }

        return first;
    }

    private static void SetEndpointEdges(
        HitObject slider,
        EdgeData head,
        EdgeData tail)
    {
        slider.EdgeHitsounds = [head.Hitsound, tail.Hitsound];
        slider.EdgeSampleSets = [head.SampleSet, tail.SampleSet];
        slider.EdgeAdditionSets = [head.AdditionSet, tail.AdditionSet];
    }

    private static EdgeData GetHeadEdges(HitObject hitObject)
    {
        return GetEdge(hitObject, 0);
    }

    private static EdgeData GetTailEdges(HitObject hitObject)
    {
        return hitObject is { IsSlider: true, EdgeHitsounds.Count: > 0 }
            ? GetEdge(hitObject, hitObject.EdgeHitsounds.Count - 1)
            : GetEdge(hitObject, 0);
    }

    private static EdgeData GetEdge(HitObject hitObject, int index)
    {
        if (hitObject is { IsSlider: true, EdgeHitsounds: { Count: > 0 } edgeHitsounds } && index < edgeHitsounds.Count)
        {
            var sampleSet = hitObject.EdgeSampleSets is { Count: > 0 } edgeSampleSets && index < edgeSampleSets.Count
                ? edgeSampleSets[index]
                : SampleSet.None;
            var additionSet = hitObject.EdgeAdditionSets is { Count: > 0 } edgeAdditionSets && index < edgeAdditionSets.Count
                ? edgeAdditionSets[index]
                : SampleSet.None;
            return new EdgeData(edgeHitsounds[index], sampleSet, additionSet);
        }

        return new EdgeData(hitObject.GetHitsounds(), hitObject.SampleSet, hitObject.AdditionSet);
    }

    private static bool IsEntirelyLinear(HitObject slider) =>
        slider.ControlPoints[0].Type == PathType.Linear && slider.ControlPoints.Skip(1).All(point => point.Type is null);

    private static void MoveLastAnchorToSliderEnd(HitObject slider)
    {
        double fullLength = slider.GetSliderPath(true).Distance;
        if (Precision.AlmostEquals(slider.PixelLength, fullLength, 0.01)) return;

        slider.ControlPoints = SliderPathUtil.MoveAnchorsToLength(
            slider.ControlPoints, fullLength, slider.PixelLength);
    }

    private static void RemoveDuplicateAnchors(List<PathControlPoint> points)
    {
        for (int index = 0; index < points.Count - 1; index++)
            if (points[index].Position == points[index + 1].Position)
            {
                points.RemoveAt(index);
                index--;
            }
    }

    /// <summary>Validates the connection mode and distance tolerance.</summary>
    /// <param name="options">The Slider Merger settings to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A mode is undefined or leniency is non-finite or negative.</exception>
    public static void Validate(SliderMergerEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.IsDefined(options.ConnectionModeSetting))
            throw new ArgumentException("Slider Merger contains an unknown mode.", nameof(options));

        if (!double.IsFinite(options.Leniency) || options.Leniency < 0)
            throw new ArgumentException(
                "Slider Merger leniency must be a finite non-negative number.",
                nameof(options));
    }

    private static List<PathControlPoint> MakePenis(List<Vector2> points, double sliderLength)
    {
        List<PathControlPoint> newPoints =
        [
            new(new Vector2(0, 0), PathType.Bezier), new(new Vector2(40, -40)),
            new(new Vector2(0, -70)), new(new Vector2(-40, -40)),
            new(new Vector2(0, 0), PathType.Bezier), new(new Vector2(96, 24)),
            new(new Vector2(168, 0), PathType.Bezier), new(new Vector2(96, -24)),
            new(new Vector2(0, 0), PathType.Bezier), new(new Vector2(-40, 40)),
            new(new Vector2(0, 70)), new(new Vector2(40, 40)), new(new Vector2(0, 0)),
        ];

        double sizeMultiplier = sliderLength / 591 * 2; // 591 is the size of the dick
        double normalAngle = -(points.Last() - points.First()).Theta;
        var matrix = Matrix2.CreateRotation(normalAngle);
        matrix *= sizeMultiplier;
        for (int index = 0; index < newPoints.Count; index++)
            // transform to slider
            newPoints[index].Position = points.First() + Matrix2.Mult(matrix, newPoints[index].Position);

        return newPoints;
    }

    private readonly record struct EdgeData(int Hitsound, SampleSet SampleSet, SampleSet AdditionSet);
}
