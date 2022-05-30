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
                if (overlaps.All(o => o.Layer < hint.Layer)) {
                    // We can replace the hints
                    reconstructionHints.RemoveRange(startIndex, endIndex - startIndex);
                    // Insert into list
                    reconstructionHints.Insert(startIndex, hint);
                } else {
                    // Void all overlapping hints
                    reconstructionHints.RemoveRange(startIndex, endIndex - startIndex);

                    // Make a void hint
                    var startL = Math.Min(hint.Start.Value.CumulativeLength,
                        overlaps.Min(o => o.Start.Value.CumulativeLength));
                    var endL = Math.Max(hint.End.Value.CumulativeLength,
                        overlaps.Max(o => o.End.Value.CumulativeLength));
                    var layer = Math.Max(hint.Layer, overlaps.Max(o => o.Layer));

                    reconstructionHints.Insert(startIndex,
                        new ReconstructionHint(Path.GetCumulativeLength(startL), Path.GetCumulativeLength(endL),
                            layer, null));
                }
            }
        }
    }
}