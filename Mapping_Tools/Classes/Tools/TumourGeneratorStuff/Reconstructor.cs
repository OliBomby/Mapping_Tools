using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    /// <summary>
    /// Reconstructs the anchors of a complete slider out of a <see cref="PathWithHints"/>.
    /// </summary>
    public class Reconstructor {
        public List<Vector2> Reconstruct(PathWithHints pathWithHints) {
            var anchors = new List<Vector2>();

            var current = pathWithHints.Path.First;
            var hints = pathWithHints.ReconstructionHints;
            var nextHint = 0;
            LinkedListNode<PathPoint> hintSegmentStart = null;

            while (current is not null) {
                if (current == hints[nextHint].End && hintSegmentStart is not null) {
                    // Add segment between start and this
                    var hint = hints[nextHint++];
                    if (hint.Anchors is null || hint.Anchors.Count == 0) {
                        // Null segment, should be reconstructed from points
                        // TODO reconstruct parts without hints correctly. This code here does not trigger for parts without hints
                        anchors.Add(hintSegmentStart.Value.Pos);
                        anchors.Add(current.Value.Pos);
                    } else {
                        // Add hint anchors
                        var hintStartPos = hint.Anchors.First();
                        var segmentStartPos = hintSegmentStart.Value.Pos;
                        var hintDir = hint.Anchors.Last() - hintStartPos;
                        var segmentDir = current.Value.Pos - segmentStartPos;
                        Matrix2 transform;
                        if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON &&
                            segmentDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                            transform = Matrix2.Identity;
                        } else if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                            transform = Matrix2.CreateRotation(segmentDir.Theta);
                        } else if (segmentDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                            transform = Matrix2.CreateRotation(current.Value.Dir.Theta - hintDir.Theta);
                        } else {
                            transform = Matrix2.CreateRotation(hintDir.Theta - segmentDir.Theta) * (segmentDir.Length / hintDir.Length);
                        }

                        anchors.AddRange(hint.Anchors.Select(hintAnchor =>
                            Matrix2.Mult(transform, hintAnchor - hintStartPos) + segmentStartPos));
                    }

                    hintSegmentStart = null;
                }
                if (nextHint < hints.Count && current == hints[nextHint].Start) {
                    hintSegmentStart = current;
                }

                current = current.Next;
            }

            return anchors;
        }
    }
}