using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen {
    /// <summary>
    /// Its a path with reconstruction hints.
    /// </summary>
    public class PathWithHints {
        public LinkedList<PathPoint> Path { get; } = new();

        /// <summary>
        /// Invariant: Non-overlapping and sorted
        /// </summary>
        private readonly List<ReconstructionHint> reconstructionHints = new();

        public IReadOnlyList<ReconstructionHint> ReconstructionHints => reconstructionHints;

        public void AddReconstructionHint(ReconstructionHint hint) {
            // Find sorted place in the list
            // The hints are not overlapping so end times are also sorted
            var startIndex = BinarySearchUtil.BinarySearch(reconstructionHints, hint.Start.Value.CumulativeLength,
                o => o.End.Value.CumulativeLength, BinarySearchUtil.EqualitySelection.Rightmost);
            if (startIndex < 0) {
                startIndex = ~startIndex;
            } else {
                startIndex++;
            }

            var endIndex = BinarySearchUtil.BinarySearch(reconstructionHints, hint.End.Value.CumulativeLength,
                o => o.Start.Value.CumulativeLength, BinarySearchUtil.EqualitySelection.Leftmost);
            if (endIndex < 0) {
                endIndex = ~endIndex;
            }

            // Handle overlapping hints
            if (endIndex < startIndex) {
                // No overlaps
                reconstructionHints.Insert(startIndex, hint);
            } else {
                var overlaps = reconstructionHints.GetRange(startIndex, endIndex - startIndex);

                // Remove all overlapping segments now, parts of the overlaps can be added back after
                reconstructionHints.RemoveRange(startIndex, endIndex - startIndex);

                // Add the merged overlaps
                reconstructionHints.InsertRange(startIndex, MergeOverlaps(hint, overlaps));
            }
        }

        private static IEnumerable<ReconstructionHint> MergeOverlaps(ReconstructionHint hint, List<ReconstructionHint> overlaps) {
            LinkedListNode<PathPoint> hintYieldedRight = hint.Start;
            foreach (ReconstructionHint overlap in overlaps) {
                // Determine the positions of overlap
                // If the overlap is on a previous layer then remove all overlapping parts and keep the hint completely
                // If the overlap is on the same layer then void all overlapping parts
                // If the overlap is on a later layer, then remove all overlapping hint and keep the overlap
                var p1 = overlap.Start.Value.CumulativeLength;
                var p2 = overlap.End.Value.CumulativeLength;
                var p3 = hint.Start.Value.CumulativeLength;
                var p4 = hint.End.Value.CumulativeLength;

                if (p1 < p3 && p2 > p4) {
                    if (overlap.Layer > hint.Layer) {
                        // Dont add the hint, keep the overlap
                        yield return overlap;
                        hintYieldedRight = hint.End;
                    } else {
                        // Enclosing overlap, has to be split into left and right segments
                        yield return CutHint(overlap, overlap.Start, hint.Start);

                        // Yield all remaining hint, because this must be the last overlap
                        // The hint doesn't have to be cut in this case
                        if (overlap.Layer < hint.Layer) {
                            yield return hint;
                        } else {
                            yield return CreateVoid(hint.Start, hint.End, hint.Layer);
                        }
                        hintYieldedRight = hint.End;

                        yield return CutHint(overlap, hint.End, overlap.End);
                    }
                } else if (p1 < p3) {
                    if (overlap.Layer > hint.Layer) {
                        // Dont add the hint, keep the overlap
                        yield return overlap;
                        hintYieldedRight = overlap.End;
                    } else {
                        // Left overlap, cut to left segment
                        yield return CutHint(overlap, overlap.Start, hint.Start);

                        // Cut and void some of the hint in the part of the overlap
                        if (overlap.Layer >= hint.Layer) {
                            yield return CreateVoid(hintYieldedRight, overlap.End, hint.Layer);
                            hintYieldedRight = overlap.End;
                        }
                    }
                } else if (p2 > p4) {
                    // Yield all remaining hint, because this must be the last overlap
                    if (overlap.Layer < hint.Layer) {
                        yield return CutHint(hint, hintYieldedRight, hint.End);
                    } else if (overlap.Layer == hint.Layer) {
                        yield return CutHint(hint, hintYieldedRight, overlap.Start);
                        yield return CreateVoid(overlap.Start, hint.End, hint.Layer);
                    } else {
                        yield return CutHint(hint, hintYieldedRight, overlap.Start);
                    }

                    hintYieldedRight = hint.End;

                    if (overlap.Layer > hint.Layer) {
                        // Keep the overlap
                        yield return overlap;
                    } else {
                        // Right overlap, cut to right segment
                        var overlapLength = p4 - p1;
                        yield return CutHint(overlap, hint.End, overlap.End);
                    }
                } else {
                    // Enclosed overlap, remove overlap completely
                    if (overlap.Layer == hint.Layer) {
                        // Cut hint until the overlap and void the overlapping part
                        yield return CutHint(hint, hintYieldedRight, overlap.Start);
                        yield return CreateVoid(overlap.Start, overlap.End, hint.Layer);
                        hintYieldedRight = overlap.End;
                    } else if (overlap.Layer > hint.Layer) {
                        // Cut hint until the overlap and add the overlapping part
                        yield return CutHint(hint, hintYieldedRight, overlap.Start);
                        hintYieldedRight = overlap.End;
                        yield return overlap;
                    }
                }
            }

            // Yield all remaining hint, if not yielded already
            if (hintYieldedRight.Value.CumulativeLength < hint.End.Value.CumulativeLength) {
                yield return CutHint(hint, hintYieldedRight, hint.End);
            }
            // TODO: Fix the StartP and LengthP values so they reflect length on the actual hint anchors
            // TODO: Add unit tests
        }

        private static ReconstructionHint CreateVoid(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int layer) {
            return new ReconstructionHint(start, end, layer, null);
        }

        private static ReconstructionHint CutHint(ReconstructionHint hint, LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            return new ReconstructionHint(start, end, hint.Layer, hint.Anchors,
                hint.PathType, hint.StartP, hint.LengthP);
        }
    }
}