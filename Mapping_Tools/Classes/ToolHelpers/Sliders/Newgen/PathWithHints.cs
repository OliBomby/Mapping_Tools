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
            bool hintYielded = false;
            foreach (ReconstructionHint overlap in overlaps) {
                // Determine the positions of overlap
                var p1 = overlap.Start.Value.CumulativeLength;
                var p2 = overlap.End.Value.CumulativeLength;
                var p3 = hint.Start.Value.CumulativeLength;
                var p4 = hint.End.Value.CumulativeLength;

                // Yield the hint if we reach the right overlap
                if (p1 >= p3 && p2 > p4 && !hintYielded) {
                    yield return hint;
                    hintYielded = true;
                }

                if (p1 < p3 &&
                    p2 > p4) {
                    // Enclosing overlap, has to be split into left and right segments
                    var overlapLengthL = p2 - p3;
                    var overlapLengthR = p4 - p1;
                    yield return new ReconstructionHint(overlap.Start, hint.Start, overlap.Layer, overlap.Anchors,
                        overlap.PathType, overlap.StartP, overlap.GetLengthP() - overlapLengthL);
                    yield return hint;
                    yield return new ReconstructionHint(hint.End, overlap.End, overlap.Layer, overlap.Anchors,
                        overlap.PathType, overlap.GetStartP() + overlapLengthR, overlap.LengthP - overlapLengthR);

                    hintYielded = true;
                } else if (p1 < p3) {
                    // Left overlap, cut to left segment
                    var overlapLength = p2 - p3;
                    yield return new ReconstructionHint(overlap.Start, hint.Start, overlap.Layer, overlap.Anchors,
                        overlap.PathType, overlap.StartP, overlap.GetLengthP() - overlapLength);
                } else if (p2 > p4) {
                    // Right overlap, cut to right segment
                    var overlapLength = p4 - p1;
                    yield return new ReconstructionHint(hint.End, overlap.End, overlap.Layer, overlap.Anchors,
                        overlap.PathType, overlap.GetStartP() + overlapLength, overlap.LengthP - overlapLength);
                } else {
                    // Enclosed overlap, remove overlaps completely
                }

                if (overlap.Layer < hint.Layer) {
                }
                // If its on a previous layer then remove all overlapping parts of the overlapping hint

                // If its on the same layer then void all overlapping parts of the overlapping hint

                // Parts of the hint which do no overlap get to stay but they are cut to just the part which is not overlapping
            }

            if (!hintYielded) {
                yield return hint;
            }
        }
    }
}