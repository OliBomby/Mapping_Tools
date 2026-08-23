using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

/// <summary>
///     Applies layered geometric tumours to slider paths and reconstructs the
///     resulting paths into osu! slider anchors.
/// </summary>
/// <remarks>
///     Simple tumours retain reconstruction hints, while wrapped or rotated
///     tumours are represented by sampled path points and red anchors. The
///     generator deliberately keeps the legacy ordering and overlap rules.
/// </remarks>
public sealed class TumourGenerator
{
    private const double relative_property_scale = 256;
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
    ///     Applies all active layers to one slider and updates its path and velocity.
    /// </summary>
    /// <param name="hitObject">The slider to mutate.</param>
    /// <param name="cancellationToken">Cancels between expensive generation stages.</param>
    /// <returns><see langword="true" /> when a new slider path was written.</returns>
    public bool TumourGenerate(HitObject hitObject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hitObject);
        if (!hitObject.IsSlider || TumourLayers.Count == 0) return false;

        double oldPixelLength = hitObject.PixelLength;

        // Create path
        var pathWithHints = PathHelper.CreatePathWithHints(hitObject.GetSliderPath());
        if (pathWithHints.Path.Count == 0) return false;

        double totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
        double initialLength = totalLength;

        // Reset the layer lengths
        layerLengths.Clear();
        int layer = 0;

        // Add tumours
        foreach (var tumourLayer in TumourLayers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Skip inactive layers
            if (!tumourLayer.IsActive) continue;

            // Recalculate
            if (tumourLayer.Recalculate)
            {
                pathWithHints.RecalculateAndFixHints();
                totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
                layer++;
            }

            // Add the length for this layer
            layerLengths.Add(totalLength);

            // Get the start and end dist in osu! pixels
            double tumourStart = tumourLayer.TumourStart;
            double tumourEnd = tumourLayer.TumourEnd;
            if (!tumourLayer.UseAbsoluteRange)
            {
                tumourStart = MathHelper.Clamp(tumourStart, -1, 1) * totalLength;
                tumourEnd = MathHelper.Clamp(tumourEnd, 0, 1) * totalLength;
            }

            // Find the start of the tumours
            var current = pathWithHints.Path.First;
            double nextDistance = tumourStart;
            bool side = tumourLayer.TumourSidedness == TumourSidedness.AlternatingLeft;
            int index = 0;
            var random = tumourLayer.RandomSeed != 0 ? new Random(tumourLayer.RandomSeed) : Random;

            while (nextDistance <= Math.Min(totalLength, tumourEnd) + Precision.DOUBLE_EPSILON
                   && current is not null
                   && (tumourLayer.TumourCount == 0 || index++ < tumourLayer.TumourCount))
            {
                cancellationToken.ThrowIfCancellationRequested();
                double progress = ToProgress(nextDistance, tumourStart, tumourEnd, totalLength);
                double length = tumourLayer.TumourLength.GetValue(progress);
                if (!tumourLayer.UseAbsoluteRange) length *= initialLength / relative_property_scale;
                double endDistance = Math.Min(nextDistance + length, tumourEnd);

                // Get which side the tumour should be on
                side = tumourLayer.TumourSidedness switch
                {
                    TumourSidedness.Left => false,
                    TumourSidedness.Right => true,
                    TumourSidedness.AlternatingLeft => !side,
                    TumourSidedness.AlternatingRight => !side,
                    TumourSidedness.Random => random.NextDouble() < 0.5,
                    _ => false,
                };

                if (endDistance >= 0)
                {
                    double epsilon = MathHelper.Clamp(length / 2, Precision.DOUBLE_EPSILON, 0.9);
                    var start = PathHelper.FindFirstOccurrenceExact(current, nextDistance, epsilon: epsilon);
                    var end = PathHelper.FindLastOccurrenceExact(start, endDistance, epsilon: epsilon);
                    // Calculate the T start/end for the tumour template
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
                if (!tumourLayer.UseAbsoluteRange) distance *= initialLength / relative_property_scale;
                nextDistance += distance;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        // Reconstruct the slider
        PathHelper.Recalculate(pathWithHints.Path);
        if (pathWithHints.Path.Count == 0 || double.IsNaN(pathWithHints.Path.Last.Value.CumulativeLength)) return false;

        var (anchors, pathType) = JustMiddleAnchors
            ? ReconstructOnlyMiddle(pathWithHints)
            : Reconstructor.Reconstruct(pathWithHints);
        if (anchors is null || anchors.Count < 2) return false;

        // Set the new slider path
        hitObject.SetSliderPath(new SliderPath(pathType, anchors.ToArray()));
        double newPixelLength = hitObject.PixelLength;

        // Update velocity
        hitObject.SliderVelocity *= oldPixelLength / newPixelLength;
        return true;
    }

    /// <summary>
    ///     Places one tumour on an already prepared path interval.
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
        var path = pathWithHints.Path;
        if (start.List != path) throw new ArgumentException("Start node has to be part of the provided path.", nameof(start));
        if (end.List != path) throw new ArgumentException("End node has to be part of the provided path.", nameof(end));

        var startPoint = start.Value;
        var endPoint = end.Value;
        if (ReferenceEquals(start, end))
        {
            // Ensure that there is a copy of the start point at the end point if we add in-between points
            // and the start and end points are the same node.
            end = new LinkedListNode<PathPoint>(endPoint);
            path.AddAfter(start, end);
        }

        if (Precision.AlmostEquals(startPoint.CumulativeLength, endPoint.CumulativeLength))
        {
            // Wii Sports Resort to T mode
            // If T is defined, then 0 should be on the first occurance of this dist and 1 on the last occurance of this dist

            // Initialize T properly
            var firstOccurrence = PathHelper.FindFirstOccurrence(start, start.Value.CumulativeLength);
            var lastOccurrence = PathHelper.FindLastOccurrence(end, end.Value.CumulativeLength);
            int pointsBetween = PathHelper.CountPointsBetween(firstOccurrence, lastOccurrence);
            double delta = 1d / (pointsBetween + 1);
            double value = 0;
            var point = firstOccurrence;
            while (point != lastOccurrence && point is not null)
            {
                point.Value = point.Value.SetT(value);
                value += delta;
                point = point.Next;
            }

            lastOccurrence.Value = lastOccurrence.Value.SetT(1);

            // T is initialized
        }

        // Count the number of nodes between start and end
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
        double betweenAngle = (endPoint.OgPos - startPoint.OgPos).LengthSquared > Precision.DOUBLE_EPSILON
            ? (endPoint.OgPos - startPoint.OgPos).Theta
            : MathHelper.LerpAngle(startPoint.AvgAngle, endPoint.AvgAngle, 0.5);
        double templateRange = endTemplateT - startTemplateT;
        var hintStart = start;
        var hintEnd = end;
        double length = Vector2.Distance(start.Value.OgPos, end.Value.OgPos);
        double scale = tumourLayer.TumourScale.GetValue(startProgress) * Scalar;
        if (!tumourLayer.UseAbsoluteRange) scale *= initialLength / relative_property_scale;
        double rotation = MathHelper.DegreesToRadians(tumourLayer.TumourRotation.GetValue(startProgress));

        // Setup tumour template with the correct shape
        var tumourTemplate = tumourLayer.TumourTemplate;
        tumourTemplate.Width = otherSide ? -scale : scale;
        tumourTemplate.Length = Precision.AlmostEquals(templateRange, 0) ? length : length / templateRange;
        tumourTemplate.Parameter = tumourTemplate.NeedsParameter ? tumourLayer.TumourParameter.GetValue(startProgress) : 0;
        // Initialize the template if necessary
        if (tumourTemplate is IRequireInit initializable) initializable.Init();

        // Make sure there are enough points between start and end for the tumour shape and resolution
        int wantedPointsBetween = Math.Max(
            pointsBetweenStartEnd,
            (int)(tumourTemplate.GetDetailLevel() * templateRange * Resolution));
        pointsBetweenStartEnd += path.EnsureCriticalPoints(
            start,
            end,
            startTemplateT,
            endTemplateT,
            tumourTemplate.GetCriticalPoints(),
            out var ensuredPoints);
        if (pointsBetweenStartEnd < wantedPointsBetween)
            pointsBetweenStartEnd += path.Subdivide(start, end, wantedPointsBetween);
        // Make sure the curvature is maintained by making sure there is at least one point between each critical point
        // And a point between start and the red point before it and a point between end and the red point after it
        pointsBetweenStartEnd += path.EnsureLocalCurvature(start, end, ensuredPoints);

        double startDistance = startPoint.CumulativeLength;
        var current = start;
        // Add tumour offsets
        while (current is not null && current.Previous != end)
        {
            var point = current.Value;
            // Scale to template T
            double t = Precision.AlmostEquals(distance, 0)
                ? (point.T - startT) / distanceT
                : (point.CumulativeLength - startDistance) / distance;
            double templateT = t * templateRange + startTemplateT;
            bool isCritical = false;
            // Check if this is a critical point
            if (ensuredPoints?.First is not null && ensuredPoints.First.Value == current)
            {
                ensuredPoints.RemoveFirst();
                isCritical = true;
            }

            // Get the offset, original pos, and direction
            var interpolatedPoint = PathPoint.Lerp(startPoint, endPoint, t);
            var position = tumourLayer.WrappingMode switch
            {
                WrappingMode.Simple => interpolatedPoint.OgPos,
                _ => point.OgPos,
            };
            (double preAngle, double postAngle) = tumourLayer.WrappingMode switch
            {
                WrappingMode.Simple => (betweenAngle, betweenAngle),
                WrappingMode.Wrap => (point.PreAngle, point.PostAngle),
                _ => (0, 0),
            };
            bool isOffsetInThisLayer = Vector2.DistanceSquared(point.OgPos, position) < Precision.DOUBLE_EPSILON;
            bool red = tumourLayer.WrappingMode switch
            {
                WrappingMode.Simple => isCritical || point.Red && isOffsetInThisLayer,
                _ => isCritical || point.Red,
            };
            // Make sure the start and end points are red
            red |= current == start || current == end;
            // Get the tumour offset
            var offset = tumourTemplate.GetOffset(templateT);

            // Modify the path
            if (current == start && start.Previous is not null && offset.LengthSquared > Precision.DOUBLE_EPSILON)
            {
                // Copy point and leave one side at 0 offset
                var newPosition = CalculateNewPos(point, position, offset, postAngle + rotation);
                current.List.AddBefore(current, new PathPoint(point.Pos, point.OgPos, point.PreAngle, point.PreAngle, point.CumulativeLength, -1, true));
                current.Value = new PathPoint(newPosition, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, 0, true);
                start = current.Previous;
                hintStart = current;
            }
            else if (current == end && end.Next is not null && offset.LengthSquared > Precision.DOUBLE_EPSILON)
            {
                // Copy point and leave one side at 0 offset
                var newPosition = CalculateNewPos(point, position, offset, preAngle + rotation);
                current.List.AddBefore(current, new PathPoint(newPosition, point.OgPos, point.PreAngle, point.PreAngle, point.CumulativeLength, 1, true));
                current.Value = new PathPoint(point.Pos, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, 2, true);
                hintEnd = current.Previous;
            }
            else if (red && !double.IsNaN(preAngle) && !double.IsNaN(postAngle) && !Precision.AlmostEquals(preAngle, postAngle) && offset.LengthSquared > Precision.DOUBLE_EPSILON)
            {
                // Copy point and offset it by both angles
                var newPosition = CalculateNewPos(point, position, offset, preAngle + rotation);
                var newPosition2 = CalculateNewPos(point, position, offset, postAngle + rotation);
                current.List.AddBefore(current, new PathPoint(newPosition, point.OgPos, point.PreAngle, point.PostAngle, point.CumulativeLength, point.T, red));
                current.Value = new PathPoint(newPosition2, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, point.T, red);
            }
            else
            {
                // Add the offset to the point
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

        if (tumourLayer.WrappingMode == WrappingMode.Simple && Precision.AlmostEquals(MathHelper.AngleDifference(rotation, 0), 0, 1E-6D))
        {
            // Maybe add a hint
            pathWithHints.AddReconstructionHint(new ReconstructionHint(
                hintStart,
                hintEnd,
                layer,
                tumourTemplate.GetReconstructionHint(),
                tumourTemplate.GetReconstructionHintPathType(),
                startTemplateT,
                endTemplateT,
                tumourTemplate.GetDistanceRelation()));
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
        var rotatedOffset = Vector2.Rotate(offset, angle);
        var actualOffset = position + rotatedOffset - point.OgPos;
        return point.Pos + actualOffset;
    }

    private static (List<Vector2> Anchors, PathType PathType) ReconstructOnlyMiddle(PathWithHints pathWithHints)
    {
        if (pathWithHints.Path.Count == 0) return ([], PathType.Linear);
        List<Vector2> anchors = [];
        var hints = pathWithHints.ReconstructionHints;
        var current = pathWithHints.Path.First;
        ReconstructionHint? currentHint = null;
        int nextHint = 0;
        while (current is not null)
        {
            while (nextHint < hints.Count && current == hints[nextHint].End)
            {
                nextHint++;
                currentHint = null;
            }

            if (nextHint < hints.Count && current == hints[nextHint].Start) currentHint = hints[nextHint];
            if (currentHint is { Anchors: not null, Layer: >= 0 } && current.Value.Red && current != currentHint.Value.Start && current != currentHint.Value.End
                || current == pathWithHints.Path.First
                || current == pathWithHints.Path.Last)
                anchors.Add(current.Value.Pos);
            current = current.Next;
        }

        return (anchors, PathType.Linear);
    }
}
