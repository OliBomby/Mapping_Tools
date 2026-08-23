using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;

namespace Mapping_Tools.Core.Tools.SliderMerger;

/// <summary>
///     Merges selected circles and sliders into one Bézier-compatible slider path.
/// </summary>
public static class SliderMergerEngine
{
    /// <summary>
    ///     Merges adjacent supported objects in their supplied order.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap whose hit-object list is changed.</param>
    /// <param name="markedObjects">The selected, bookmarked, time-filtered, or complete object sequence.</param>
    /// <param name="options">The connection and geometry settings.</param>
    /// <param name="progress">Optional progress receiver, reported as a percentage.</param>
    /// <param name="cancellationToken">Cancels between object-pair evaluations.</param>
    /// <returns>The number of source objects incorporated into merged sliders.</returns>
    /// <exception cref="ArgumentException">The leniency or connection mode is invalid.</exception>
    public static int Merge(
        Beatmap beatmap,
        IReadOnlyList<HitObject> markedObjects,
        SliderMergerOptions options,
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
            progress?.Report(objects.Count == 0 ? 100 : (double)index / objects.Count * 100);

            var first = objects[index];
            var second = objects[index + 1];
            var firstConnection = first.IsSlider
                ? options.MergeOnSliderEnd
                    ? first.GetSliderPath().PositionAt(1)
                    : first.CurvePoints.Last()
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
            if (options.Leniency == 727)
            {
                survivor.SetAllCurvePoints(MakePenis(survivor.GetAllCurvePoints(), survivor.PixelLength));
                survivor.PixelLength *= 2;
                survivor.SliderType = PathType.Bezier;
            }
        }

        progress?.Report(100);
        return mergedObjects;
    }

    /// <summary>
    ///     Determines whether a Bézier control polygon encodes only straight segments.
    /// </summary>
    /// <param name="points">The complete Bézier control polygon.</param>
    /// <returns><see langword="true" /> when every interior point is a duplicated segment endpoint.</returns>
    public static bool IsLinearBezier(IReadOnlyList<Vector2> points)
    {
        // Every point at not the endpoints must have an anchor before or after it at the same position
        for (int index = 1; index < points.Count - 1; index++)
            if (points[index] != points[index - 1] && points[index] != points[index + 1])
                return false;

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
        SliderMergerOptions options)
    {
        if (options.MergeOnSliderEnd)
        {
            // In order to merge on the slider end we first move the anchors such that the last anchor is exactly on the slider end
            // After that merge as usual
            first.SetAllCurvePoints(SliderPathUtil.MoveAnchorsToLength(
                first.GetAllCurvePoints(),
                first.SliderType,
                first.PixelLength,
                out var pathType));
            first.SliderType = pathType;
        }

        var firstPath = BezierConverter.ConvertToBezierAnchors(
            first.GetAllCurvePoints(), first.SliderType);
        var secondPath = BezierConverter.ConvertToBezierAnchors(
            second.GetAllCurvePoints(), second.SliderType);
        double extraLength = 0;

        switch (options.ConnectionModeSetting)
        {
            case SliderMergerConnectionMode.Move:
                Move(secondPath, firstPath.Last() - secondPath.First());
                break;
            case SliderMergerConnectionMode.Linear:
                firstPath.Add(firstPath.Last());
                firstPath.Add(secondPath.First());
                extraLength = (first.CurvePoints.Last() - second.Pos).Length;
                break;
            case SliderMergerConnectionMode.Bezier:
                extraLength = Vector2.Distance(firstPath.Last(), secondPath.First());
                break;
            default:
                throw new ArgumentException("Unexpected slider connection mode.", nameof(options));
        }

        var mergedPath = firstPath.Concat(secondPath).ToList();
        mergedPath.Round();
        bool linear = options.ConnectionModeSetting != SliderMergerConnectionMode.Bezier && options.LinearOnLinear && IsLinearBezier(firstPath) && IsLinearBezier(secondPath);
        if (linear) RemoveDuplicateAnchors(mergedPath);

        first.SetAllCurvePoints(mergedPath);
        first.SliderType = linear ? PathType.Linear : PathType.Bezier;
        first.PixelLength = first.PixelLength + second.PixelLength + extraLength;
        first.Repeat = 1;
        return first;
    }

    private static HitObject MergeSliderAndCircle(
        HitObject first,
        HitObject second,
        SliderMergerOptions options)
    {
        var path = BezierConverter.ConvertToBezierAnchors(
            first.GetAllCurvePoints(), first.SliderType);
        path.Add(path.Last());
        path.Add(second.Pos);
        double extraLength = (first.CurvePoints.Last() - second.Pos).Length;
        path.Round();
        bool linear = options.LinearOnLinear && IsLinearBezier(path);
        if (linear) RemoveDuplicateAnchors(path);

        first.SetAllCurvePoints(path);
        first.SliderType = linear ? PathType.Linear : PathType.Bezier;
        first.PixelLength += extraLength;
        first.Repeat = 1;
        return first;
    }

    private static HitObject MergeCircleAndSlider(
        HitObject first,
        HitObject second,
        SliderMergerOptions options)
    {
        var path = BezierConverter.ConvertToBezierAnchors(
            second.GetAllCurvePoints(), second.SliderType);
        path.Insert(0, path.First());
        path.Insert(0, first.Pos);
        double extraLength = (first.Pos - second.Pos).Length;
        path.Round();
        bool linear = options.LinearOnLinear && IsLinearBezier(path);
        if (linear) RemoveDuplicateAnchors(path);

        second.SetAllCurvePoints(path);
        second.SliderType = linear ? PathType.Linear : PathType.Bezier;
        second.PixelLength += extraLength;
        second.Repeat = 1;
        return second;
    }

    private static HitObject MergeCircles(
        HitObject first,
        HitObject second,
        SliderMergerOptions options)
    {
        if (Precision.DefinitelyBigger(Vector2.Distance(first.Pos, second.Pos), 0))
        {
            first.SetAllCurvePoints([first.Pos, second.Pos]);
            first.SliderType = options.LinearOnLinear ? PathType.Linear : PathType.Bezier;
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
        return hitObject.IsSlider && hitObject.EdgeHitsounds is { Count: > 0 }
            ? GetEdge(hitObject, hitObject.EdgeHitsounds.Count - 1)
            : GetEdge(hitObject, 0);
    }

    private static EdgeData GetEdge(HitObject hitObject, int index)
    {
        if (hitObject.IsSlider && hitObject.EdgeHitsounds is { Count: > 0 } edgeHitsounds && index < edgeHitsounds.Count)
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

    private static void RemoveDuplicateAnchors(List<Vector2> points)
    {
        for (int index = 0; index < points.Count - 1; index++)
            if (points[index] == points[index + 1])
            {
                points.RemoveAt(index);
                index--;
            }
    }

    private static void Validate(SliderMergerOptions options)
    {
        if (!Enum.IsDefined(options.ImportModeSetting) || !Enum.IsDefined(options.ConnectionModeSetting))
            throw new ArgumentException("Slider Merger contains an unknown mode.", nameof(options));

        if (!double.IsFinite(options.Leniency) || options.Leniency < 0)
            throw new ArgumentException(
                "Slider Merger leniency must be a finite non-negative number.",
                nameof(options));
    }

    private static List<Vector2> MakePenis(List<Vector2> points, double sliderLength)
    {
        // Penis shape
        List<Vector2> newPoints =
        [
            new(0, 0), new(40, -40), new(0, -70), new(-40, -40), new(0, 0), new(0, 0),
            new(96, 24), new(168, 0), new(168, 0), new(96, -24), new(0, 0), new(0, 0),
            new(-40, 40), new(0, 70), new(40, 40), new(0, 0),
        ];

        double sizeMultiplier = sliderLength / 591 * 2; // 591 is the size of the dick
        double normalAngle = -(points.Last() - points.First()).Theta;
        var matrix = Matrix2.CreateRotation(normalAngle);
        matrix *= sizeMultiplier;
        for (int index = 0; index < newPoints.Count; index++)
            // transform to slider
            newPoints[index] = points.First() + Matrix2.Mult(matrix, newPoints[index]);

        return newPoints;
    }

    private readonly record struct EdgeData(int Hitsound, SampleSet SampleSet, SampleSet AdditionSet);
}
