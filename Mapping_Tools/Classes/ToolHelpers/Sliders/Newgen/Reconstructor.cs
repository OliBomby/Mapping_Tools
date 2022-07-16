using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen {
    /// <summary>
    /// Reconstructs the anchors of a complete slider out of a <see cref="PathWithHints"/>.
    /// </summary>
    public class Reconstructor {
        private PathGenerator2 PathGenerator { get; set; } = new();

        public (List<Vector2>, PathType) Reconstruct(PathWithHints pathWithHints) {
            var anchors = new List<Vector2>();
            var current = pathWithHints.Path.First;
            var hints = ConstructHints(pathWithHints.Path, pathWithHints.ReconstructionHints);;
            var nextHint = 0;
            LinkedListNode<PathPoint> hintSegmentStart = null;
            var pathType = hints.Count == 1 && hints[0].Start == pathWithHints.Path.First &&
                           hints[0].End == pathWithHints.Path.Last
                ? hints[0].PathType
                : PathType.Bezier;

            while (current is not null) {
                if (nextHint < hints.Count && current == hints[nextHint].End && hintSegmentStart is not null) {
                    // Add segment between start and this
                    var hint = hints[nextHint++];
                    if (hint.Anchors is null || hint.Anchors.Count == 0) {
                        // Null segment, should be reconstructed from points
                        // TODO reconstruct parts without hints correctly. This code here does not trigger for parts without hints
                        anchors.Add(hintSegmentStart.Value.Pos);
                        anchors.Add(current.Value.Pos);
                    } else {
                        // Cut hint anchors to completion
                        var cutAnchors = CutAnchors(hint.Anchors, hint.PathType, hint.StartP, hint.EndP, out var hintPathType);

                        // Convert hint path type
                        if (pathType != PathType.Bezier && hintPathType != pathType) {
                            throw new Exception("Can not convert hint path to non-bezier path type.");
                        }

                        var convertedAnchors = pathType == PathType.Bezier ? BezierConverter.ConvertToBezierAnchors(cutAnchors, hintPathType) : cutAnchors;

                        // Add hint anchors
                        anchors.AddRange(TransformAnchors(convertedAnchors, hintSegmentStart.Value.Pos, current.Value.Pos,
                            MathHelper.LerpAngle(hintSegmentStart.Value.AvgAngle, current.Value.AvgAngle, 0.5)));
                    }

                    hintSegmentStart = null;
                }
                if (nextHint < hints.Count && current == hints[nextHint].Start) {
                    hintSegmentStart = current;
                }

                current = current.Next;
            }

            return (anchors, pathType);
        }

        private static List<Vector2> CutAnchors(List<Vector2> anchors, PathType pathType, double startP, double endP, out PathType newPathType) {
            newPathType = pathType;
            if (Precision.AlmostEquals(startP, 0) && Precision.AlmostEquals(endP, 1)) {
                return anchors;
            }

            var fullLength = new SliderPath(pathType, anchors.ToArray()).Distance;
            var newLengthStart = (1 - startP) * fullLength;
            var newLengthEnd = (endP - startP) * fullLength;

            if (!Precision.AlmostEquals(startP, 0)) {
                var anchorsReversed = anchors.ToArray();
                Array.Reverse(anchorsReversed);
                var sliderPath = new SliderPath(pathType, anchorsReversed, newLengthStart);
                anchors = SliderPathUtil.MoveAnchorsToLength(sliderPath, fullLength, newLengthStart, out newPathType);
                pathType = newPathType;
                fullLength = newLengthStart;
                anchors.Reverse();
            }

            if (!Precision.AlmostEquals(endP, 1)) {
                var sliderPath = new SliderPath(pathType, anchors.ToArray(), newLengthEnd);
                anchors = SliderPathUtil.MoveAnchorsToLength(sliderPath, fullLength, newLengthEnd, out newPathType);
            }

            return anchors;
        }

        private static Vector2[] TransformAnchors(IReadOnlyList<Vector2> anchors, Vector2 start, Vector2 end, double theta) {
            var hintStartPos = anchors[0];
            var hintDir = anchors[^1] - hintStartPos;
            var segmentDir = end - start;
            Matrix2 transform;

            if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON &&
                segmentDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                transform = Matrix2.CreateRotation(-theta);
            } else if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                transform = Matrix2.CreateRotation(segmentDir.Theta);
            } else if (segmentDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                transform = Matrix2.CreateRotation(hintDir.Theta - theta);
            } else {
                // Scale along the axis of hintDir
                //transform = Matrix2.CreateRotation(-segmentDir.Theta);
                //transform = Matrix2.Mult(transform, Matrix2.CreateScale(segmentDir.Length / hintDir.Length, 1));
                //transform = Matrix2.Mult(transform, Matrix2.CreateRotation(hintDir.Theta));
                transform = Matrix2.CreateRotation(hintDir.Theta - segmentDir.Theta) * (segmentDir.Length / hintDir.Length);
            }

            // Transform all the anchors and put them into an array
            var transformedAnchors = new Vector2[anchors.Count];
            for (var i = 0; i < anchors.Count; i++) {
                if (i == 0) {
                    transformedAnchors[i] = start;
                } else if (i == anchors.Count - 1) {
                    transformedAnchors[i] = end;
                } else {
                    transformedAnchors[i] = Matrix2.Mult(transform, anchors[i] - hintStartPos) + start;
                }
            }

            return transformedAnchors;
        }

        /// <summary>
        /// Constructs full hints for a path.
        /// Accurate angle and distances must be calculated on the path beforehand.
        /// </summary>
        /// <param name="path">The path to construct hints for</param>
        /// <param name="existingHints">Existing hints to be used instead of generated hints if they are more efficient.</param>
        public List<ReconstructionHint> ConstructHints(LinkedList<PathPoint> path, IReadOnlyList<ReconstructionHint> existingHints = null) {
            var hints = existingHints is null
                ? new List<ReconstructionHint>()
                : new List<ReconstructionHint>(existingHints.Where(o => o.Anchors is not null));

            var layer = existingHints is null ? 0 : existingHints.Max(hint => hint.Layer) + 1;
            var current = path.First;
            var nextHint = 0;
            ReconstructionHint? currentHint = null;
            LinkedListNode<PathPoint> segmentStart = null;

            // Loop through path and find all segments which are either an existing hint or a gap between two hints
            // Also split on all red points
            while (current is not null) {
                if (segmentStart is not null && (
                        currentHint.HasValue && current == currentHint.Value.End ||
                        (nextHint < hints.Count && current == hints[nextHint].Start) ||
                        (!currentHint.HasValue && current.Value.Red) ||
                        current.Next is null)) {
                    // End of hint and hint active or its the start of hint and there is no hint active
                    // Create between start and this
                    var segmentEnd = current;

                    // Construct a hint
                    var constructedHint = ConstructHint(segmentStart, segmentEnd, layer);

                    if (currentHint.HasValue) {
                        // Keep the better hint
                        if (currentHint.Value.Anchors.Count > constructedHint.Anchors.Count) {
                            hints[nextHint - 1] = constructedHint;
                        }
                    } else {
                        // Insert the constructed hint
                        hints.Insert(nextHint++, constructedHint);
                    }

                    currentHint = null;
                    segmentStart = null;
                }
                if (segmentStart is null) {
                    segmentStart = current;

                    if (nextHint < hints.Count && current == hints[nextHint].Start) {
                        currentHint = hints[nextHint++];
                    }
                }

                current = current.Next;
            }

            return hints;
        }

        private ReconstructionHint ConstructHint(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int layer) {
            var anchors = PathGenerator.GeneratePath(start, end).ToList();
            return new ReconstructionHint(start, end, layer, anchors);
        }
    }
}