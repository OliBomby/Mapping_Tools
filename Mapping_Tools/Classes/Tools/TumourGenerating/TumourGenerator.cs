using System;
using System.Collections.Generic;
using System.Threading;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;

namespace Mapping_Tools.Classes.Tools.TumourGenerating;

/// <summary>
/// Generates tumours on sliders.
/// We first have a slider represented by a linked list of <see cref="PathPoint"/> and a set of reconstruction hints
/// which tell us how to reconstruct specific parts of the path with anchors. We also have a set of tumour specifications.
/// As invariant, the reconstruction hints must not be overlapping with exception of the endpoints.
/// We apply the tumours by updating the path and reconstruction hints:
/// Tumours with the Simple <see cref="WrappingMode"/> will only update the reconstruction hints and not the path.
/// Complex tumours will instead update the path with their combined offsets. No reconstruction hints will be present for these areas, but instead red anchor hints will be on the path points.
/// A special slider reconstructor will take the path and reconstruction hints to create anchors for the slider.
/// All reconstruction hints must be used when reconstructing the slider.
/// </summary>
public class TumourGenerator {
    private const double RelativePropertyScale = 256;

    /// <summary>
    /// The number of points per osu! pixel used to approximate the shape of the tumours.
    /// </summary>
    public double Resolution { get; set; } = 1;

    /// <summary>
    /// The size scalar of tumours.
    /// </summary>
    public double Scalar { get; set; } = 1;

    public bool JustMiddleAnchors { get; set; }

    public IReadOnlyList<ITumourLayer> TumourLayers  { get; set; }

    public Reconstructor Reconstructor { get; init; } = new();

    public Random Random { get; set; } = new();

    private readonly List<double> layerLengths = new();

    /// <summary>
    /// The lengths of the slider at the start of each layer in the last tumour generate call.
    /// </summary>
    public IReadOnlyList<double> LayerLengths => layerLengths;

    /// <summary>
    /// Places copious amounts of tumours on the slider.
    /// Changes slider curvepoints, pixel length, and velocity
    /// </summary>
    /// <param name="ho">The slider to generate tumours on.</param>
    /// <param name="ct">Cancellation token to interrupt the tumour generation.</param>
    /// <returns></returns>
    public bool TumourGenerate(HitObject ho, CancellationToken ct = default) {
        if (!ho.IsSlider || TumourLayers.Count == 0) return false;

        var oldPixelLength = ho.PixelLength;

        // Create path
        var pathWithHints = PathHelper.CreatePathWithHints(ho.GetSliderPath());
        if (pathWithHints.Path.Count == 0) {
            return false;
        }
        var totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
        var initialLength = totalLength;

        // Reset the layer lengths
        layerLengths.Clear();

        // Add tumours
        int layer = 0;
        foreach (var tumourLayer in TumourLayers) {
            ct.ThrowIfCancellationRequested();

            // Skip inactive layers
            if (!tumourLayer.IsActive) continue;

            // Recalculate
            if (tumourLayer.Recalculate) {
                pathWithHints.RecalculateAndFixHints();
                totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
                layer++;
            }

            // Add the length for this layer
            layerLengths.Add(totalLength);

            // Get the start and end dist in osu! pixels
            var tumourStart = tumourLayer.TumourStart;
            var tumourEnd = tumourLayer.TumourEnd;
            if (!tumourLayer.UseAbsoluteRange) {
                tumourStart =  MathHelper.Clamp(tumourStart, -1, 1) * totalLength;
                tumourEnd = MathHelper.Clamp(tumourLayer.TumourEnd, 0, 1) * totalLength;
            }

            // Find the start of the tumours
            var random = tumourLayer.RandomSeed != 0 ? new Random(tumourLayer.RandomSeed) : Random;
            var current = pathWithHints.Path.First;
            var nextDist = tumourStart;
            var side = tumourLayer.TumourSidedness == TumourSidedness.AlternatingLeft;
            var i = 0;

            while (nextDist <= Math.Min(totalLength, tumourEnd) + Precision.DoubleEpsilon && current is not null &&
                   (tumourLayer.TumourCount == 0 || i++ < tumourLayer.TumourCount)) {
                ct.ThrowIfCancellationRequested();

                var length = tumourLayer.TumourLength.GetValue(ToProgress(nextDist, tumourStart, tumourEnd, totalLength));
                if (!tumourLayer.UseAbsoluteRange)
                    length *= initialLength / RelativePropertyScale;
                var endDist = Math.Min(nextDist + length, tumourEnd);

                // Get which side the tumour should be on
                side = tumourLayer.TumourSidedness switch {
                    TumourSidedness.Left => false,
                    TumourSidedness.Right => true,
                    TumourSidedness.AlternatingLeft => !side,
                    TumourSidedness.AlternatingRight => !side,
                    TumourSidedness.Random => random.NextDouble() < 0.5,
                    _ => false
                };

                if (endDist >= 0) {
                    var epsilon = MathHelper.Clamp(length / 2, Precision.DoubleEpsilon, 0.9);
                    var start = PathHelper.FindFirstOccurrenceExact(current, nextDist, epsilon: epsilon);
                    var end = PathHelper.FindLastOccurrenceExact(start, endDist, epsilon: epsilon);

                    // Calculate the T start/end for the tumour template
                    double startT= 0;
                    double endT = 1;
                    if (Precision.DefinitelyBigger(length, 0)) {
                        if (!Precision.AlmostEquals(start.Value.CumulativeLength, nextDist, epsilon))
                            startT = MathHelper.Clamp((start.Value.CumulativeLength - nextDist) / length, 0, 1);
                        if (!Precision.AlmostEquals(end.Value.CumulativeLength, nextDist + length, epsilon))
                            endT = MathHelper.Clamp((end.Value.CumulativeLength - nextDist) / length, 0, 1);
                    }

                    PlaceTumour(pathWithHints, tumourLayer, layer, start, end, startT, endT, Math.Max(0, tumourStart), Math.Min(totalLength, tumourEnd), side, initialLength);

                    current = start;
                }

                var dist = Math.Max(1, tumourLayer.TumourDistance.GetValue(ToProgress(nextDist, tumourStart, tumourEnd, totalLength)));
                if (!tumourLayer.UseAbsoluteRange)
                    dist *= initialLength / RelativePropertyScale;
                nextDist += dist;
            }
        }

        ct.ThrowIfCancellationRequested();

        // Reconstruct the slider
        PathHelper.Recalculate(pathWithHints.Path);

        if (pathWithHints.Path.Count == 0 || double.IsNaN(pathWithHints.Path.Last.Value.CumulativeLength)) {
            return false;
        }

        var (anchors, pathType) = JustMiddleAnchors ? ReconstructOnlyMiddle(pathWithHints) : Reconstructor.Reconstruct(pathWithHints);

        if (anchors is null || anchors.Count < 2) {
            return false;
        }

        // Set the new slider path
        var newSliderPath = new SliderPath(pathType, anchors.ToArray());
        ho.SetSliderPath(newSliderPath);

        // Update velocity
        var newPixelLength = ho.PixelLength;
        ho.SliderVelocity *= oldPixelLength / newPixelLength;

        return true;
    }

    /// <summary>
    /// Places a tumour onto the path between the specified start and end points.
    /// May increase the size of path.
    /// </summary>
    /// <param name="pathWithHints">The path to add a tumour to</param>
    /// <param name="layer">The layer index for hints</param>
    /// <param name="start">The start point</param>
    /// <param name="end">The end point</param>
    /// <param name="tumourLayer">The tumour layer</param>
    /// <param name="startTemplateT">T value for where to start with the tumour template</param>
    /// <param name="endTemplateT">T value for where to end with the tumour template</param>
    /// <param name="tumourStart">The start distance of the sequence of tumours to tumour layer values</param>
    /// <param name="tumourEnd">The end distance of the sequence of tumours to tumour layer values</param>
    /// <param name="otherSide">Whether to place the tumour on the other side of the slider</param>
    /// <param name="initialLength">The initial pixel length of the slider. Determines the scale of the tumour.</param>
    public void PlaceTumour([NotNull]PathWithHints pathWithHints, [NotNull]ITumourLayer tumourLayer, int layer,
        [NotNull]LinkedListNode<PathPoint> start, [NotNull]LinkedListNode<PathPoint> end,
        double startTemplateT, double endTemplateT, double tumourStart, double tumourEnd, bool otherSide, double initialLength) {
        var path = pathWithHints.Path;
        if (start.List != path) {
            throw new ArgumentException(@"Start node has to be part of the provided path.", nameof(start));
        }

        if (end.List != path) {
            throw new ArgumentException(@"End node has to be part of the provided path.", nameof(end));
        }

        var startPoint = start.Value;
        var endPoint = end.Value;

        // Ensure that there is a copy of the start point at the end point if we add in-between points
        // and the start and end points are the same node.
        if (ReferenceEquals(start, end)) {
            end = new LinkedListNode<PathPoint>(endPoint);
            start.List!.AddAfter(start, end);
        }

        if (Precision.AlmostEquals(startPoint.CumulativeLength, endPoint.CumulativeLength)) {
            // Wii Sports Resort to T mode
            // If T is defined, then 0 should be on the first occurance of this dist and 1 on the last occurance of this dist

            // Initialize T properly
            var firstOccurrence = PathHelper.FindFirstOccurrence(start, start.Value.CumulativeLength);
            var lastOccurrence = PathHelper.FindLastOccurrence(end, end.Value.CumulativeLength);

            int pointsBetweenTi = PathHelper.CountPointsBetween(firstOccurrence, lastOccurrence);
            var dti = 1d / (pointsBetweenTi + 1);
            var tti = 0d;
            var pti = firstOccurrence;
            while (pti != lastOccurrence && pti is not null) {
                pti.Value = pti.Value.SetT(tti);
                tti += dti;
                pti = pti.Next;
            }

            lastOccurrence.Value = lastOccurrence.Value.SetT(1);

            // T is initialized
        }

        // Count the number of nodes between start and end
        int pointsBetween = PathHelper.CountPointsBetween(start, end);

        var totalLength = path.Last!.Value.CumulativeLength;
        startPoint = start.Value;
        endPoint = end.Value;
        var startProg = ToProgress(startPoint.CumulativeLength, tumourStart, tumourEnd, totalLength);
        var endProg = endPoint.CumulativeLength / totalLength;
        double startT = startPoint.T;
        double endT = endPoint.T;
        double dist = endPoint.CumulativeLength - startPoint.CumulativeLength;
        double distT = endT - startT;
        double betweenAngle = (endPoint.OgPos - startPoint.OgPos).LengthSquared > Precision.DoubleEpsilon
            ? (endPoint.OgPos - startPoint.OgPos).Theta
            : MathHelper.LerpAngle(startPoint.AvgAngle, endPoint.AvgAngle, 0.5);
        double templateRange = endTemplateT - startTemplateT;
        var hintStart = start;
        var hintEnd = end;

        var length = Vector2.Distance(start.Value.OgPos, end.Value.OgPos);
        var scale = tumourLayer.TumourScale.GetValue(startProg) * Scalar;
        if (!tumourLayer.UseAbsoluteRange)
            scale *= initialLength / RelativePropertyScale;
        var rotation = MathHelper.DegreesToRadians(tumourLayer.TumourRotation.GetValue(startProg));

        // Setup tumour template with the correct shape
        var tumourTemplate = tumourLayer.TumourTemplate;
        tumourTemplate.Width = otherSide ? -scale : scale;
        tumourTemplate.Length = Precision.AlmostEquals(templateRange, 0) ? length : length / templateRange;
        tumourTemplate.Parameter = tumourTemplate.NeedsParameter && tumourLayer.TumourParameter is not null ? tumourLayer.TumourParameter.GetValue(startProg) : 0;

        // Initialize the template if necessary
        if (tumourTemplate is IRequireInit initializable) {
            initializable.Init();
        }

        // Make sure there are enough points between start and end for the tumour shape and resolution
        int wantedPointsBetween = Math.Max(pointsBetween, (int)(tumourTemplate.GetDetailLevel() * templateRange * Resolution));  // The needed number of points for the tumour
        pointsBetween += path.EnsureCriticalPoints(start, end, startTemplateT, endTemplateT,
            tumourTemplate.GetCriticalPoints(), out var ensuredPoints);
        if (pointsBetween < wantedPointsBetween) {
            pointsBetween += path.Subdivide(start, end, wantedPointsBetween);
        }

        // Make sure the curvature is maintained by making sure there is at least one point between each critical point
        // And a point between start and the red point before it and a point between end and the red point after it
        pointsBetween += path.EnsureLocalCurvature(start, end, ensuredPoints);

        // Add tumour offsets
        double startDist = startPoint.CumulativeLength;
        var current = start;
        while (current is not null && current.Previous != end) {
            var point = current.Value;

            double t = Precision.AlmostEquals(dist, 0) ?
                (point.T - startT) / distT :
                (point.CumulativeLength - startDist) / dist;

            // Scale to template T
            var templateT = t * templateRange + startTemplateT;

            // Check if this is a critical point
            bool isCritical = false;
            if (ensuredPoints.First != null && ensuredPoints.First.Value == current) {
                ensuredPoints.RemoveFirst();
                isCritical = true;
            }

            // Get the offset, original pos, and direction
            var interpolatedPoint = PathPoint.Lerp(startPoint, endPoint, t);
            var pos = tumourLayer.WrappingMode switch {
                WrappingMode.Simple => interpolatedPoint.OgPos,
                _ => point.OgPos
            };
            (double preAngle, double postAngle) = tumourLayer.WrappingMode switch {
                WrappingMode.Simple => (betweenAngle, betweenAngle),
                WrappingMode.Wrap => (point.PreAngle, point.PostAngle),
                _ => (0, 0),
            };
            var isOffsetInThisLayer = Vector2.DistanceSquared(point.OgPos, pos) < Precision.DoubleEpsilon;
            var red = tumourLayer.WrappingMode switch {
                WrappingMode.Simple => isCritical || (point.Red && isOffsetInThisLayer),
                _ => isCritical || point.Red
            };

            // Make sure the start and end points are red
            red |= current == start || current == end;

            // Get the tumour offset
            var offset = tumourTemplate.GetOffset(templateT);

            if (current == start && start.Previous is not null && offset.LengthSquared > Precision.DoubleEpsilon) {
                // Copy point and leave one side at 0 offset
                var newPos = CalculateNewPos(point, pos, offset, postAngle + rotation);

                current.List.AddBefore(current, new PathPoint(point.Pos, point.OgPos, point.PreAngle, point.PreAngle, point.CumulativeLength, -1, true));
                current.Value = new PathPoint(newPos, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, 0, true);
                start = current.Previous;
                hintStart = current;
            } else if (current == end && end.Next is not null && offset.LengthSquared > Precision.DoubleEpsilon) {
                // Copy point and leave one side at 0 offset
                var newPos = CalculateNewPos(point, pos, offset, preAngle + rotation);

                current.List.AddBefore(current, new PathPoint(newPos, point.OgPos, point.PreAngle, point.PreAngle, point.CumulativeLength, 1, true));
                current.Value = new PathPoint(point.Pos, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, 2, true);
                hintEnd = current.Previous;
            } else if (red && !double.IsNaN(preAngle) && !double.IsNaN(postAngle) && !Precision.AlmostEquals(preAngle, postAngle)
                       && offset.LengthSquared > Precision.DoubleEpsilon) {
                // Copy point and offset it by both angles
                var newPos = CalculateNewPos(point, pos, offset, preAngle + rotation);
                var newPos2 = CalculateNewPos(point, pos, offset, postAngle + rotation);

                current.List.AddBefore(current, new PathPoint(newPos, point.OgPos, point.PreAngle, point.PostAngle, point.CumulativeLength, point.T, red));
                current.Value = new PathPoint(newPos2, point.OgPos, point.PostAngle, point.PostAngle, point.CumulativeLength, point.T, red);
            } else {
                // Add the offset to the point
                var angle = MathHelper.LerpAngle(preAngle, postAngle, 0.5);
                var newPos = CalculateNewPos(point, pos, offset, angle + rotation);

                // Modify the path
                current.Value = new PathPoint(newPos, point.OgPos, point.PreAngle, point.PostAngle, point.CumulativeLength, point.T, red);
            }

            current = current.Next;
        }

        // Maybe add a hint
        if (tumourLayer.WrappingMode == WrappingMode.Simple &&
            Precision.AlmostEquals(MathHelper.AngleDifference(rotation, 0), 0, 1E-6D)) {
            var hintAnchors = tumourTemplate.GetReconstructionHint();
            var hintType = tumourTemplate.GetReconstructionHintPathType();
            var distFunc = tumourTemplate.GetDistanceRelation();

            pathWithHints.AddReconstructionHint(new ReconstructionHint(hintStart, hintEnd, layer, hintAnchors, hintType, startTemplateT, endTemplateT, distFunc: distFunc));

            // Add null segments for the possible on-continuous endpoints of the tumour
            if (start != hintStart) {
                pathWithHints.AddReconstructionHint(new ReconstructionHint(start, hintStart, layer, null));;
            }
            if (end != hintEnd) {
                pathWithHints.AddReconstructionHint(new ReconstructionHint(hintEnd, end, layer, null));;
            }
        } else {
            pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, null));
        }
    }

    private static double ToProgress(double dist, double start, double end, double totalLength) {
        start = Math.Max(0, start);
        end = Math.Min(totalLength, end);

        return (dist - start) / (end - start);
    }

    private static Vector2 CalculateNewPos(PathPoint point, Vector2 pos, Vector2 offset, double angle) {
        var rotatedOffset = Vector2.Rotate(offset, angle);
        var actualOffset = pos + rotatedOffset - point.OgPos;
        return point.Pos + actualOffset;
    }

    private (List<Vector2>, PathType) ReconstructOnlyMiddle(PathWithHints pathWithHints) {
        if (pathWithHints.Path.Count == 0) return (null, PathType.Linear);

        var anchors = new List<Vector2>();
        var hints = pathWithHints.ReconstructionHints;
        var current = pathWithHints.Path.First;
        ReconstructionHint? currentHint = null;
        var nextHint = 0;

        while (current is not null) {
            // Skip any finished hints
            while (nextHint < hints.Count && current == hints[nextHint].End) {
                nextHint++;
                currentHint = null;
            }

            if (nextHint < hints.Count && current == hints[nextHint].Start) {
                currentHint = hints[nextHint];
            }

            // Add the red points to the anchors if this is a valid hint
            if ((currentHint is { Anchors: { }, Layer: >= 0 } && current.Value.Red &&
                 current != currentHint.Value.Start && current != currentHint.Value.End) || current == pathWithHints.Path.First || current == pathWithHints.Path.Last) {
                anchors.Add(current.Value.Pos);
            }

            current = current.Next;
        }

        return (anchors, PathType.Linear);
    }
}