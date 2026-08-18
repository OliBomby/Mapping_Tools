using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders.Newgen;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

/// <summary>
/// Applies layered geometric tumours to slider paths and reconstructs the
/// resulting paths into osu! slider anchors.
/// </summary>
/// <remarks>
/// Simple tumours retain reconstruction hints, while wrapped or rotated
/// tumours are represented by sampled path points and red anchors. The
/// generator deliberately keeps the legacy ordering and overlap rules.
/// </remarks>
public sealed class TumourGenerator
{
    private const double RelativePropertyScale = 256;
    private readonly List<double> layerLengths = [];

    /// <summary>Gets or sets the number of sampled points per osu! pixel.</summary>
    public double Resolution { get; set; } = 1;

    /// <summary>Gets or sets the global tumour scale multiplier.</summary>
    public double Scalar { get; set; } = 1;

    /// <summary>Gets or sets whether only middle anchors are emitted.</summary>
    public bool JustMiddleAnchors { get; set; }

    /// <summary>Gets or sets the ordered layers applied to each slider.</summary>
    public IReadOnlyList<TumourLayer> TumourLayers { get; set; } = [];

    /// <summary>Gets the reconstruction strategy used after path edits.</summary>
    public Reconstructor Reconstructor { get; init; } = new();

    /// <summary>Gets or sets the random source used by unseeded random layers.</summary>
    public Random Random { get; set; } = new();

    /// <summary>Gets the slider lengths observed at active layer boundaries.</summary>
    public IReadOnlyList<double> LayerLengths => layerLengths;

    /// <summary>
    /// Applies all active layers to one slider and updates its path and velocity.
    /// </summary>
    /// <param name="hitObject">The slider to mutate.</param>
    /// <param name="cancellationToken">Cancels between expensive generation stages.</param>
    /// <returns><see langword="true"/> when a new slider path was written.</returns>
    public bool TumourGenerate(HitObject hitObject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hitObject);
        if (!hitObject.IsSlider || TumourLayers.Count == 0) return false;

        double oldPixelLength = hitObject.PixelLength;
        PathWithHints pathWithHints = PathHelper.CreatePathWithHints(hitObject.GetSliderPath());
        if (pathWithHints.Path.Count == 0) return false;

        double totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
        double initialLength = totalLength;
        layerLengths.Clear();
        var layer = 0;

        foreach (TumourLayer tumourLayer in TumourLayers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!tumourLayer.IsActive) continue;

            if (tumourLayer.Recalculate)
            {
                pathWithHints.RecalculateAndFixHints();
                totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
                layer++;
            }

            layerLengths.Add(totalLength);
            double tumourStart = tumourLayer.TumourStart;
            double tumourEnd = tumourLayer.TumourEnd;
            if (!tumourLayer.UseAbsoluteRange)
            {
                tumourStart = MathHelper.Clamp(tumourStart, -1, 1) * totalLength;
                tumourEnd = MathHelper.Clamp(tumourEnd, 0, 1) * totalLength;
            }

            LinkedListNode<PathPoint>? current = pathWithHints.Path.First;
            double nextDistance = tumourStart;
            bool side = tumourLayer.TumourSidedness == TumourSidedness.AlternatingLeft;
            var index = 0;
            Random random = tumourLayer.RandomSeed != 0 ? new Random(tumourLayer.RandomSeed) : Random;

            while (nextDistance <= Math.Min(totalLength, tumourEnd) + Precision.DoubleEpsilon &&
                   current is not null &&
                   (tumourLayer.TumourCount == 0 || index++ < tumourLayer.TumourCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                double progress = ToProgress(nextDistance, tumourStart, tumourEnd, totalLength);
                double length = tumourLayer.TumourLength.GetValue(progress);
                if (!tumourLayer.UseAbsoluteRange) length *= initialLength / RelativePropertyScale;
                double endDistance = Math.Min(nextDistance + length, tumourEnd);

                side = tumourLayer.TumourSidedness switch
                {
                    TumourSidedness.Left => false,
                    TumourSidedness.Right => true,
                    TumourSidedness.AlternatingLeft => !side,
                    TumourSidedness.AlternatingRight => !side,
                    TumourSidedness.Random => random.NextDouble() < 0.5,
                    _ => false
                };

                if (endDistance >= 0)
                {
                    double epsilon = MathHelper.Clamp(length / 2, Precision.DoubleEpsilon, 0.9);
                    LinkedListNode<PathPoint> start = PathHelper.FindFirstOccurrenceExact(current, nextDistance, epsilon: epsilon);
                    LinkedListNode<PathPoint> end = PathHelper.FindLastOccurrenceExact(start, endDistance, epsilon: epsilon);
                    double startT = 0;
                    double endT = 1;
                    if (Precision.DefinitelyBigger(length, 0))
                    {
                        if (!Precision.AlmostEquals(start.Value.CumulativeLength, nextDistance, epsilon))
                            startT = MathHelper.Clamp((start.Value.CumulativeLength - nextDistance) / length, 0, 1);
                        if (!Precision.AlmostEquals(end.Value.CumulativeLength, nextDistance + length, epsilon))
                            endT = MathHelper.Clamp((end.Value.CumulativeLength - nextDistance) / length, 0, 1);
                    }

                    PlaceTumour(
                        pathWithHints,
                        tumourLayer,
                        layer,
                        start,
                        end,
                        startT,
                        endT,
                        Math.Max(0, tumourStart),
                        Math.Min(totalLength, tumourEnd),
                        side,
                        initialLength);
                    current = start;
                }

                double distance = Math.Max(1, tumourLayer.TumourDistance.GetValue(progress));
                if (!tumourLayer.UseAbsoluteRange) distance *= initialLength / RelativePropertyScale;
                nextDistance += distance;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        PathHelper.Recalculate(pathWithHints.Path);
        if (pathWithHints.Path.Count == 0 || double.IsNaN(pathWithHints.Path.Last.Value.CumulativeLength)) return false;

        (List<Vector2> anchors, PathType pathType) = JustMiddleAnchors
            ? ReconstructOnlyMiddle(pathWithHints)
            : Reconstructor.Reconstruct(pathWithHints);
        if (anchors is null || anchors.Count < 2) return false;

        hitObject.SetSliderPath(new SliderPath(pathType, anchors.ToArray()));
        double newPixelLength = hitObject.PixelLength;
        hitObject.SliderVelocity *= oldPixelLength / newPixelLength;
        return true;
    }

    /// <summary>
    /// Places one tumour on an already prepared path interval.
    /// </summary>
    /// <param name="pathWithHints">The mutable sampled path.</param>
    /// <param name="tumourLayer">The layer supplying shape and placement values.</param>
    /// <param name="layer">The layer precedence used for hint overlap resolution.</param>
    /// <param name="start">The first path node covered by the tumour.</param>
    /// <param name="end">The last path node covered by the tumour.</param>
    /// <param name="startTemplateT">The template progress at the interval start.</param>
    /// <param name="endTemplateT">The template progress at the interval end.</param>
    /// <param name="tumourStart">The bounded sequence start distance.</param>
    /// <param name="tumourEnd">The bounded sequence end distance.</param>
    /// <param name="otherSide">Whether the tumour is mirrored across the slider.</param>
    /// <param name="initialLength">The original slider length used for relative scaling.</param>
    public void PlaceTumour(
        PathWithHints pathWithHints,
        TumourLayer tumourLayer,
        int layer,
        LinkedListNode<PathPoint> start,
        LinkedListNode<PathPoint> end,
        double startTemplateT,
        double endTemplateT,
        double tumourStart,
        double tumourEnd,
        bool otherSide,
        double initialLength)
    {
        ArgumentNullException.ThrowIfNull(pathWithHints);
        ArgumentNullException.ThrowIfNull(tumourLayer);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(end);
        LinkedList<PathPoint> path = pathWithHints.Path;
        if (start.List != path) throw new ArgumentException("Start node has to be part of the provided path.", nameof(start));
        if (end.List != path) throw new ArgumentException("End node has to be part of the provided path.", nameof(end));

        PathPoint startPoint = start.Value;
        PathPoint endPoint = end.Value;
        if (ReferenceEquals(start, end))
        {
            end = new LinkedListNode<PathPoint>(endPoint);
            path.AddAfter(start, end);
        }

        if (Precision.AlmostEquals(startPoint.CumulativeLength, endPoint.CumulativeLength))
        {
            LinkedListNode<PathPoint> firstOccurrence = PathHelper.FindFirstOccurrence(start, start.Value.CumulativeLength);
            LinkedListNode<PathPoint> lastOccurrence = PathHelper.FindLastOccurrence(end, end.Value.CumulativeLength);
            int pointsBetween = PathHelper.CountPointsBetween(firstOccurrence, lastOccurrence);
            double delta = 1d / (pointsBetween + 1);
            double value = 0;
            LinkedListNode<PathPoint>? point = firstOccurrence;
            while (point != lastOccurrence && point is not null)
            {
                point.Value = point.Value.SetT(value);
                value += delta;
                point = point.Next;
            }
            lastOccurrence.Value = lastOccurrence.Value.SetT(1);
        }

        int pointsBetweenStartEnd = PathHelper.CountPointsBetween(start, end);
        double totalLength = path.Last!.Value.CumulativeLength;
        startPoint = start.Value;
        endPoint = end.Value;
        double startProgress = ToProgress(startPoint.CumulativeLength, tumourStart, tumourEnd, totalLength);
        double endProgress = endPoint.CumulativeLength / totalLength;
        double startT = startPoint.T;
        double endT = endPoint.T;
        double distance = endPoint.CumulativeLength - startPoint.CumulativeLength;
        double distanceT = endT - startT;
        double betweenAngle = (endPoint.OgPos - startPoint.OgPos).LengthSquared > Precision.DoubleEpsilon
            ? (endPoint.OgPos - startPoint.OgPos).Theta
            : MathHelper.LerpAngle(startPoint.AvgAngle, endPoint.AvgAngle, 0.5);
        double templateRange = endTemplateT - startTemplateT;
        LinkedListNode<PathPoint> hintStart = start;
        LinkedListNode<PathPoint> hintEnd = end;
        double length = Vector2.Distance(start.Value.OgPos, end.Value.OgPos);
        double scale = tumourLayer.TumourScale.GetValue(startProgress) * Scalar;
        if (!tumourLayer.UseAbsoluteRange) scale *= initialLength / RelativePropertyScale;
        double rotation = MathHelper.DegreesToRadians(tumourLayer.TumourRotation.GetValue(startProgress));

        ITumourTemplate tumourTemplate = tumourLayer.TumourTemplate;
        tumourTemplate.Width = otherSide ? -scale : scale;
        tumourTemplate.Length = Precision.AlmostEquals(templateRange, 0) ? length : length / templateRange;
        tumourTemplate.Parameter = tumourTemplate.NeedsParameter ? tumourLayer.TumourParameter.GetValue(startProgress) : 0;
        if (tumourTemplate is IRequireInit initializable) initializable.Init();

        int wantedPointsBetween = Math.Max(
            pointsBetweenStartEnd,
            (int)(tumourTemplate.GetDetailLevel() * templateRange * Resolution));
        pointsBetweenStartEnd += path.EnsureCriticalPoints(
            start,
            end,
            startTemplateT,
            endTemplateT,
            tumourTemplate.GetCriticalPoints(),
            out LinkedList<LinkedListNode<PathPoint>> ensuredPoints);
        if (pointsBetweenStartEnd < wantedPointsBetween)
            pointsBetweenStartEnd += path.Subdivide(start, end, wantedPointsBetween);
        pointsBetweenStartEnd += path.EnsureLocalCurvature(start, end, ensuredPoints);

        double startDistance = startPoint.CumulativeLength;
        LinkedListNode<PathPoint>? current = start;
        while (current is not null && current.Previous != end)
        {
            PathPoint point = current.Value;
            double t = Precision.AlmostEquals(distance, 0)
                ? (point.T - startT) / distanceT
                : (point.CumulativeLength - startDistance) / distance;
            double templateT = t * templateRange + startTemplateT;
            bool isCritical = false;
            if (ensuredPoints?.First is not null && ensuredPoints.First.Value == current)
            {
                ensuredPoints.RemoveFirst();
                isCritical = true;
            }

            PathPoint interpolatedPoint = PathPoint.Lerp(startPoint, endPoint, t);
            Vector2 position = tumourLayer.WrappingMode switch
            {
                WrappingMode.Simple => interpolatedPoint.OgPos,
                _ => point.OgPos
            };
            (double preAngle, double postAngle) = tumourLayer.WrappingMode switch
            {
                WrappingMode.Simple => (betweenAngle, betweenAngle),
                WrappingMode.Wrap => (point.PreAngle, point.PostAngle),
                _ => (0, 0)
            };
            bool isOffsetInThisLayer = Vector2.DistanceSquared(point.OgPos, position) < Precision.DoubleEpsilon;
            bool red = tumourLayer.WrappingMode switch
            {
                WrappingMode.Simple => isCritical || point.Red && isOffsetInThisLayer,
                _ => isCritical || point.Red
            };
            red |= current == start || current == end;
            Vector2 offset = tumourTemplate.GetOffset(templateT);

            if (current == start && start.Previous is not null && offset.LengthSquared > Precision.DoubleEpsilon)
            {
                Vector2 newPosition = CalculateNewPos(point, position, offset, postAngle + rotation);
                current.List.AddBefore(current, new PathPoint(point.Pos, point.OgPos, point.PreAngle, point.PreAngle, point.CumulativeLength, -1, true));
                current.Value = new PathPoint(newPosition, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, 0, true);
                start = current.Previous;
                hintStart = current;
            }
            else if (current == end && end.Next is not null && offset.LengthSquared > Precision.DoubleEpsilon)
            {
                Vector2 newPosition = CalculateNewPos(point, position, offset, preAngle + rotation);
                current.List.AddBefore(current, new PathPoint(newPosition, point.OgPos, point.PreAngle, point.PreAngle, point.CumulativeLength, 1, true));
                current.Value = new PathPoint(point.Pos, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, 2, true);
                hintEnd = current.Previous;
            }
            else if (red && !double.IsNaN(preAngle) && !double.IsNaN(postAngle) &&
                     !Precision.AlmostEquals(preAngle, postAngle) && offset.LengthSquared > Precision.DoubleEpsilon)
            {
                Vector2 newPosition = CalculateNewPos(point, position, offset, preAngle + rotation);
                Vector2 newPosition2 = CalculateNewPos(point, position, offset, postAngle + rotation);
                current.List.AddBefore(current, new PathPoint(newPosition, point.OgPos, point.PreAngle, point.PostAngle, point.CumulativeLength, point.T, red));
                current.Value = new PathPoint(newPosition2, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, point.T, red);
            }
            else
            {
                double angle = MathHelper.LerpAngle(preAngle, postAngle, 0.5);
                current.Value = new PathPoint(
                    CalculateNewPos(point, position, offset, angle + rotation),
                    point.OgPos,
                    point.PreAngle,
                    point.PostAngle,
                    point.CumulativeLength,
                    point.T,
                    red);
            }

            current = current.Next;
        }

        if (tumourLayer.WrappingMode == WrappingMode.Simple &&
            Precision.AlmostEquals(MathHelper.AngleDifference(rotation, 0), 0, 1E-6D))
        {
            pathWithHints.AddReconstructionHint(new ReconstructionHint(
                hintStart,
                hintEnd,
                layer,
                tumourTemplate.GetReconstructionHint(),
                tumourTemplate.GetReconstructionHintPathType(),
                startTemplateT,
                endTemplateT,
                distFunc: tumourTemplate.GetDistanceRelation()));
            if (start != hintStart) pathWithHints.AddReconstructionHint(new ReconstructionHint(start, hintStart, layer, null));
            if (end != hintEnd) pathWithHints.AddReconstructionHint(new ReconstructionHint(hintEnd, end, layer, null));
        }
        else
        {
            pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, null));
        }
    }

    private static double ToProgress(double distance, double start, double end, double totalLength)
    {
        start = Math.Max(0, start);
        end = Math.Min(totalLength, end);
        return (distance - start) / (end - start);
    }

    private static Vector2 CalculateNewPos(PathPoint point, Vector2 position, Vector2 offset, double angle)
    {
        Vector2 rotatedOffset = Vector2.Rotate(offset, angle);
        Vector2 actualOffset = position + rotatedOffset - point.OgPos;
        return point.Pos + actualOffset;
    }

    private static (List<Vector2> Anchors, PathType PathType) ReconstructOnlyMiddle(PathWithHints pathWithHints)
    {
        if (pathWithHints.Path.Count == 0) return ([], PathType.Linear);
        List<Vector2> anchors = [];
        IReadOnlyList<ReconstructionHint> hints = pathWithHints.ReconstructionHints;
        LinkedListNode<PathPoint>? current = pathWithHints.Path.First;
        ReconstructionHint? currentHint = null;
        var nextHint = 0;
        while (current is not null)
        {
            while (nextHint < hints.Count && current == hints[nextHint].End)
            {
                nextHint++;
                currentHint = null;
            }
            if (nextHint < hints.Count && current == hints[nextHint].Start) currentHint = hints[nextHint];
            if (currentHint is { Anchors: not null, Layer: >= 0 } && current.Value.Red &&
                current != currentHint.Value.Start && current != currentHint.Value.End ||
                current == pathWithHints.Path.First || current == pathWithHints.Path.Last)
            {
                anchors.Add(current.Value.Pos);
            }
            current = current.Next;
        }
        return (anchors, PathType.Linear);
    }
}
