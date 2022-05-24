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
                if (current == hints[nextHint].End) {
                    // Add segment between start and this
                    var hint = hints[nextHint++];
                    var hintDir = hint.Anchors.Last() - hint.Anchors.First();
                    var segmentDir = current.Value.Pos - hintSegmentStart.Value.Pos;
                    Matrix2 transform;
                    if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON &&
                        segmentDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                        transform = Matrix2.Identity;
                    } else if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                        transform = Matrix2.CreateRotation(segmentDir.Theta);
                    } else if (segmentDir.LengthSquared < Precision.DOUBLE_EPSILON) {
                        transform = Matrix2.CreateRotation(current.Value.Dir.Theta - hintDir.Theta);
                    } else {
                        transform = Matrix2.CreateRotation(segmentDir.Theta - hintDir.Theta) * (segmentDir.Length / hintDir.Length);
                    }

                    anchors.AddRange(hint.Anchors.Select(hintAnchor => Matrix2.Mult(transform, hintAnchor)));

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