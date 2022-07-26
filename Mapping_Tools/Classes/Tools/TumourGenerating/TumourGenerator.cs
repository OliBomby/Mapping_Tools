using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;

namespace Mapping_Tools.Classes.Tools.TumourGenerating {
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
        /// <summary>
        /// The wrapping mode controls how the tumour sits on the slider.
        /// TODO: remove this
        /// </summary>
        public WrappingMode WrappingMode { get; set; }

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

        /// <summary>
        /// Places copious amounts of tumours on the slider.
        /// Changes slider curvepoints, pixel length, and velocity
        /// </summary>
        /// <param name="ho"></param>
        /// <returns></returns>
        public bool TumourGenerate(HitObject ho) {
            if (!ho.IsSlider || TumourLayers.Count == 0) return false;

            var oldPixelLength = ho.PixelLength;

            // Create path
            var pathWithHints = PathHelper.CreatePathWithHints(ho.GetSliderPath());
            if (pathWithHints.Path.Count == 0) {
                return false;
            }
            var totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
            var random = new Random();

            // Add tumours
            int layer = 0;
            foreach (var tumourLayer in TumourLayers) {
                if (!tumourLayer.IsActive) continue;

                var tumourStart = MathHelper.Clamp(tumourLayer.TumourStart, -1, 1);
                var tumourEnd = MathHelper.Clamp(tumourLayer.TumourEnd, 0, 1);

                // Recalculate
                if (tumourLayer.Recalculate) {
                    pathWithHints.RecalculateAndFixHints();
                    totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
                    layer++;
                }

                // Calculate count multiplier
                var totalDist = tumourEnd - tumourStart;
                var countDist = totalDist * totalLength / (tumourLayer.TumourCount - 1);

                // Find the start of the tumours
                var current = pathWithHints.Path.First;
                var nextDist = tumourStart * totalLength;
                var side = tumourLayer.TumourSidedness == TumourSidedness.AlternatingLeft;

                while (nextDist <= tumourEnd * totalLength + Precision.DOUBLE_EPSILON && current is not null) {
                    var length = tumourLayer.TumourLength.GetValue(nextDist / totalLength);
                    var endDist = Math.Min(nextDist + length, tumourLayer.TumourCount > 0 ? totalLength : tumourEnd * totalLength);

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
                        var epsilon = MathHelper.Clamp(length / 2, Precision.DOUBLE_EPSILON, 0.9);
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

                        PlaceTumour(pathWithHints, tumourLayer, layer, start, end, startT, endT, side);

                        current = start;
                    }

                    var dist = Math.Max(1, tumourLayer.TumourCount > 0 ? countDist
                        : tumourLayer.TumourDistance.GetValue(nextDist / totalLength));
                    nextDist += dist;
                }
            }

            // Reconstruct the slider
            PathHelper.Recalculate(pathWithHints.Path);
            var (anchors, pathType) = Reconstructor.Reconstruct(pathWithHints);
            var newSliderPath = new SliderPath(pathType, anchors.ToArray());

            // Set the new slider path
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
        /// <param name="otherSide">Whether to place the tumour on the other side of the slider</param>
        public void PlaceTumour([NotNull]PathWithHints pathWithHints, [NotNull]ITumourLayer tumourLayer, int layer,
            [NotNull]LinkedListNode<PathPoint> start, [NotNull]LinkedListNode<PathPoint> end,
            double startTemplateT, double endTemplateT, bool otherSide) {
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

                // T should either be undefined on both start and end point because T is not yet initialized for this distance
                // or T should be defined for both
                // If T is defined, then 0 should be on the first occurance of this dist and 1 on the last occurance of this dist

                if (double.IsNaN(startPoint.T) ^ double.IsNaN(endPoint.T)) {
                    throw new InvalidOperationException(
                        "T value must be both defined or both undefined for the same distance value.");
                }

                if (double.IsNaN(startPoint.T)) {
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
                }
                // T is initialized
            }

            // Count the number of nodes between start and end
            int pointsBetween = PathHelper.CountPointsBetween(start, end);

            var totalLength = path.Last!.Value.CumulativeLength;
            startPoint = start.Value;
            endPoint = end.Value;
            var startProg = startPoint.CumulativeLength / totalLength;
            var endProg = endPoint.CumulativeLength / totalLength;
            double startT = startPoint.T;
            double endT = endPoint.T;
            double dist = endPoint.CumulativeLength - startPoint.CumulativeLength;
            double distT = endT - startT;
            double betweenAngle = (endPoint.OgPos - startPoint.OgPos).LengthSquared > Precision.DOUBLE_EPSILON
                ? (endPoint.OgPos - startPoint.OgPos).Theta
                : MathHelper.LerpAngle(startPoint.AvgAngle, endPoint.AvgAngle, 0.5);
            double templateRange = endTemplateT - startTemplateT;

            var length = Vector2.Distance(start.Value.OgPos, end.Value.OgPos);
            var scale = tumourLayer.TumourScale.GetValue(startProg) * Scalar;
            var rotation = MathHelper.DegreesToRadians(tumourLayer.TumourRotation.GetValue(startProg));

            // Setup tumour template with the correct shape
            var tumourTemplate = tumourLayer.TumourTemplate;
            tumourTemplate.Width = otherSide ? -scale : scale;
            tumourTemplate.Length = length;
            tumourTemplate.Parameter = tumourTemplate.NeedsParameter ? tumourLayer.TumourParameter.GetValue(startProg) : 0;

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
                var pos = WrappingMode switch {
                    WrappingMode.Simple => interpolatedPoint.OgPos,
                    WrappingMode.Replace => interpolatedPoint.OgPos,
                    WrappingMode.RoundReplace => interpolatedPoint.OgPos,
                    _ => point.OgPos
                };
                var angle = WrappingMode switch {
                    WrappingMode.Simple => betweenAngle,
                    WrappingMode.Replace => betweenAngle,
                    WrappingMode.RoundReplace => interpolatedPoint.AvgAngle,
                    WrappingMode.RoundWrap => interpolatedPoint.AvgAngle,
                    WrappingMode.Wrap => point.AvgAngle,
                    _ => betweenAngle,
                };
                var red = WrappingMode switch {
                    WrappingMode.Simple => isCritical || (point.Red && point.Pos != point.OgPos),
                    WrappingMode.Replace => isCritical || (point.Red && point.Pos != point.OgPos),
                    WrappingMode.RoundReplace => isCritical || (point.Red && point.Pos != point.OgPos),
                    _ => isCritical || point.Red
                };
                // Make sure the start and end points are red
                red |= current == start || current == end;

                // Add the offset to the point
                var offset = Vector2.Rotate(tumourTemplate.GetOffset(templateT), angle + rotation);
                var actualOffset = pos + offset - point.OgPos;
                var newPos = point.Pos + actualOffset;

                // Modify the path
                current.Value = new PathPoint(newPos, point.OgPos, point.PreAngle, point.PostAngle, point.CumulativeLength, point.T, red);

                current = current.Next;
            }

            // Maybe add a hint
            if (WrappingMode == WrappingMode.Simple && Precision.AlmostEquals(MathHelper.AngleDifference(rotation, 0), 0, 1E-6D)) {
                var hintAnchors = tumourTemplate.GetReconstructionHint();
                var hintType = tumourTemplate.GetReconstructionHintPathType();
                var distFunc = tumourTemplate.GetDistanceRelation();
                var startP = distFunc?.Invoke(startTemplateT) ?? startTemplateT;
                var endP = distFunc?.Invoke(endTemplateT) ?? endTemplateT;

                pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, hintAnchors, hintType, startP, endP, distFunc: distFunc));
            } else {
                pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, null));
            }
        }
    }
}
