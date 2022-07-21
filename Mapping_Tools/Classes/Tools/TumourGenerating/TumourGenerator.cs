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
                        var start = PathHelper.FindFirstOccurrenceExact(current, nextDist, epsilon:0.9);
                        var end = PathHelper.FindLastOccurrenceExact(start, endDist);

                        // Make sure start and end are red
                        start.Value = start.Value.SetRed(true);
                        end.Value = end.Value.SetRed(true);

                        // Calculate the T start/end for the tumour template
                        double startT= 0;
                        double endT = 1;
                        if (Precision.DefinitelyBigger(length, 0)) {
                            startT = (start.Value.CumulativeLength - nextDist) / length;
                            endT = (end.Value.CumulativeLength - nextDist) / length;
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
            var reconstructor = new Reconstructor();
            var (anchors, pathType) = reconstructor.Reconstruct(pathWithHints);
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

            var startP = start.Value;
            var endP = end.Value;

            // Ensure that there is a copy of the start point at the end point if we add in-between points
            // and the start and end points are the same node.
            if (ReferenceEquals(start, end)) {
                end = new LinkedListNode<PathPoint>(endP);
                start.List!.AddAfter(start, end);
            }

            if (Precision.AlmostEquals(startP.CumulativeLength, endP.CumulativeLength)) {
                // Wii Sports Resort to T mode

                // T should either be undefined on both start and end point because T is not yet initialized for this distance
                // or T should be defined for both
                // If T is defined, then 0 should be on the first occurance of this dist and 1 on the last occurance of this dist

                if (double.IsNaN(startP.T) ^ double.IsNaN(endP.T)) {
                    throw new InvalidOperationException(
                        "T value must be both defined or both undefined for the same distance value.");
                }

                if (double.IsNaN(startP.T)) {
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
            startP = start.Value;
            endP = end.Value;
            var startProg = startP.CumulativeLength / totalLength;
            var endProg = endP.CumulativeLength / totalLength;
            double startT = startP.T;
            double endT = endP.T;
            double dist = endP.CumulativeLength - startP.CumulativeLength;
            double distT = endT - startT;
            double betweenAngle = (endP.OgPos - startP.OgPos).LengthSquared > Precision.DOUBLE_EPSILON
                ? (endP.OgPos - startP.OgPos).Theta
                : MathHelper.LerpAngle(startP.AvgAngle, endP.AvgAngle, 0.5);

            var length = Vector2.Distance(start.Value.OgPos, end.Value.OgPos);
            var scale = tumourLayer.TumourScale.GetValue(startProg) * Scalar;
            var rotation = MathHelper.DegreesToRadians(tumourLayer.TumourRotation.GetValue(startProg));

            // Setup tumour template with the correct shape
            var tumourTemplate = tumourLayer.TumourTemplate;
            tumourTemplate.Width = otherSide ? -scale : scale;
            tumourTemplate.Length = length;

            // Initialize the template if necessary
            if (tumourTemplate is IRequireInit initializable) {
                initializable.Init();
            }

            // Make sure there are enough points between start and end for the tumour shape and resolution
            int wantedPointsBetween = Math.Max(pointsBetween, (int)(tumourTemplate.GetDetailLevel() * Resolution));  // The needed number of points for the tumour
            pointsBetween += path.EnsureCriticalPoints(start, end, startTemplateT, endTemplateT,
                tumourTemplate.GetCriticalPoints(), out var ensuredPoints);
            if (pointsBetween < wantedPointsBetween) {
                pointsBetween += path.Subdivide(start, end, wantedPointsBetween);
            }

            // Add tumour offsets
            double startDist = startP.CumulativeLength;
            var pn = start;
            for (int i = 0; i < pointsBetween; i++) {
                pn = pn.Next;
                Debug.Assert(pn != null, nameof(pn) + " != null");
                var p = pn.Value;

                double t = Precision.AlmostEquals(dist, 0) ?
                    (p.T - startT) / distT :
                    (p.CumulativeLength - startDist) / dist;

                // Scale to template T
                t = t * (endTemplateT - startTemplateT) + startTemplateT;

                // Check if this is a critical point
                bool isCritical = false;
                if (ensuredPoints.First != null && ensuredPoints.First.Value == pn) {
                    ensuredPoints.RemoveFirst();
                    isCritical = true;
                }

                // Get the offset, original pos, and direction
                var np = PathPoint.Lerp(startP, endP, t);
                var pos = WrappingMode switch {
                    WrappingMode.Simple => np.OgPos,
                    WrappingMode.Replace => np.OgPos,
                    WrappingMode.RoundReplace => np.OgPos,
                    _ => p.OgPos
                };
                var angle = WrappingMode switch {
                    WrappingMode.Simple => betweenAngle,
                    WrappingMode.Replace => betweenAngle,
                    WrappingMode.RoundReplace => np.AvgAngle,
                    WrappingMode.RoundWrap => np.AvgAngle,
                    WrappingMode.Wrap => p.AvgAngle,
                    _ => betweenAngle,
                };
                var red = WrappingMode switch {
                    WrappingMode.Simple => isCritical || (p.Red && p.Pos != p.OgPos),
                    WrappingMode.Replace => isCritical || (p.Red && p.Pos != p.OgPos),
                    WrappingMode.RoundReplace => isCritical || (p.Red && p.Pos != p.OgPos),
                    _ => isCritical || p.Red
                };

                // Add the offset to the point
                var offset = Vector2.Rotate(tumourTemplate.GetOffset(t), angle + rotation);
                var actualOffset = pos + offset - p.OgPos;
                var newPos = p.Pos + actualOffset;

                // Modify the path
                pn.Value = new PathPoint(newPos, p.OgPos, p.PreAngle, p.PostAngle, p.CumulativeLength, p.T, red);
            }

            // Maybe add a hint
            if (WrappingMode == WrappingMode.Simple && Precision.AlmostEquals(MathHelper.AngleDifference(rotation, 0), 0, 1E-6D)) {
                var hintAnchors = tumourTemplate.GetReconstructionHint();
                var hintType = tumourTemplate.GetReconstructionHintPathType();
                var distFunc = tumourTemplate.GetDistanceRelation();

                pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, hintAnchors, hintType, distFunc: distFunc));
            } else {
                pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, null));
            }
        }
    }
}
