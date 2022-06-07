using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders;
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
            for (int i = 0; i < TumourLayers.Count; i++) {
                var tumourLayer = TumourLayers[i];
                var tumourStart = MathHelper.Clamp(tumourLayer.TumourStart, 0, 1);
                var tumourEnd = MathHelper.Clamp(tumourLayer.TumourEnd, 0, 1);

                // Recalculate
                if (tumourLayer.Recalculate) {
                    PathHelper.Recalculate(pathWithHints.Path);
                    totalLength = pathWithHints.Path.Last!.Value.CumulativeLength;
                    layer++;
                }

                // Calculate count multiplier
                var totalDist = tumourEnd - tumourStart;
                var countDist = totalDist * totalLength / tumourLayer.TumourCount;

                // Find the start of the tumours
                var current = pathWithHints.Path.First;
                var nextDist = tumourStart * totalLength;
                var side = false;

                while (nextDist <= tumourEnd * totalLength + Precision.DOUBLE_EPSILON && current is not null) {
                    var length = tumourLayer.TumourLength.GetValue(nextDist / totalLength);
                    var endDist = nextDist + length;
                    var start = PathHelper.FindFirstOccurrenceExact(current, nextDist);
                    var end = PathHelper.FindLastOccurrenceExact(start, endDist);

                    // Calculate the T start/end for the tumour template
                    var startT = (start.Value.CumulativeLength - nextDist) / length;
                    var endT = (end.Value.CumulativeLength - endDist) / length;

                    // Get which side the tumour should be on
                    side = tumourLayer.TumourSidedness switch {
                        TumourSidedness.Left => false,
                        TumourSidedness.Right => true,
                        TumourSidedness.Alternating => !side,
                        TumourSidedness.Random => random.NextDouble() < 0.5,
                        _ => false
                    };

                    PlaceTumour(pathWithHints, tumourLayer, layer, start, end, startT, endT, side);

                    current = start;
                    var dist = Math.Max(1, tumourLayer.TumourCount > 0 ? countDist
                            : tumourLayer.TumourDistance.GetValue(nextDist / totalLength));
                    nextDist += dist;
                }
            }

            // Set the new slider path
            var reconstructor = new Reconstructor();
            var (anchors, pathType) = reconstructor.Reconstruct(pathWithHints);
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
        /// <param name="otherSide">Whether to place the tumour on the other side of the slider</param>
        private void PlaceTumour([NotNull]PathWithHints pathWithHints, [NotNull]ITumourLayer tumourLayer, int layer,
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
                    while (pti != lastOccurrence) {
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

            var totalLength = path.Last.Value.CumulativeLength;
            startP = start.Value;
            endP = end.Value;
            var startProg = startP.CumulativeLength / totalLength;
            var endProg = endP.CumulativeLength / totalLength;
            double startT = startP.T;
            double endT = endP.T;
            double dist = endP.CumulativeLength - startP.CumulativeLength;
            double distT = endT - startT;

            // Make sure there are enough points between start and end for the tumour shape and resolution
            var tumourTemplate = tumourLayer.TumourTemplate;
            int wantedPointsBetween = Math.Max(pointsBetween, (int)(tumourTemplate.GetLength() * Resolution));  // The needed number of points for the tumour
            if (pointsBetween < wantedPointsBetween) {
                pointsBetween += path.Subdivide(start, end, wantedPointsBetween);
            }
            pointsBetween += path.EnsureCriticalPoints(start, end, tumourTemplate.GetCriticalPoints());

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

                // Get the offset, original pos, and direction
                var scale = tumourLayer.TumourScale.GetValue(t * (endProg - startProg) + startProg) * Scalar;
                var offset = tumourTemplate.GetOffset(t) * scale;
                var np = WrappingMode switch {
                    WrappingMode.Wrap => p,
                    WrappingMode.RoundWrap => new PathPoint(p.Pos, Vector2.Lerp(startP.Dir, endP.Dir, t), p.CumulativeLength),
                    WrappingMode.Replace => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), endP.Pos - startP.Pos, p.CumulativeLength),
                    WrappingMode.RoundReplace => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), Vector2.Lerp(startP.Dir, endP.Dir, t), p.CumulativeLength),
                    _ => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), endP.Pos - startP.Pos, p.CumulativeLength)
                };

                // Add the offset to the point
                var newPos = np.Pos + Vector2.Rotate(offset, np.Dir.Theta);

                // Modify the path
                pn.Value = new PathPoint(newPos, p.Dir, p.CumulativeLength, p.T, p.Red);
            }

            // Maybe add a hint
            if (WrappingMode == WrappingMode.Simple) {
                var hintAnchors = tumourTemplate.GetReconstructionHint();
                var hintType = tumourTemplate.GetReconstructionHintPathType();
                var scale = tumourLayer.TumourScale.GetValue(startProg) * Scalar;
                var scaledAnchors = Precision.AlmostEquals(scale, 1) ? hintAnchors :
                    hintAnchors.Select(o => new Vector2(o.X, o.Y * scale)).ToList();
                pathWithHints.AddReconstructionHint(new ReconstructionHint(start, end, layer, scaledAnchors, hintType));
            }
        }
    }
}
