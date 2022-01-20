using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public class TumourGenerator {
        /// <summary>
        /// The wrapping mode controls how the tumour sits on the slider.
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

        /// <summary>
        /// Places a tumour onto the path between the specified start and end points.
        /// May increase the size of path.
        /// </summary>
        /// <param name="path">The path to add a tumour to</param>
        /// <param name="tumourTemplate">The tumour shape</param>
        /// <param name="start">The start point</param>
        /// <param name="end">The end point</param>
        public void PlaceTumour([NotNull]LinkedList<PathPoint> path, [NotNull]ITumourTemplate tumourTemplate, 
            [NotNull]LinkedListNode<PathPoint> start, [NotNull]LinkedListNode<PathPoint> end) {
            if (start.List != path) {
                throw new ArgumentException(@"Start node has to be part of the provided path.", nameof(start));
            }

            if (end.List != path) {
                throw new ArgumentException(@"Start node has to be part of the provided path.", nameof(end));
            }

            // Count the number of nodes between start and end
            int pointsBetween = 0;
            LinkedListNode<PathPoint> pn = start;
            while (pn.Next != null && pn.Next != end) {
                pointsBetween++;
                pn = pn.Next;
            }
            
            if (pn.Next == null && start != end) {
                throw new ArgumentException("The end index can not be smaller than the start index.");
            }

            // Make sure there are enough points between start and end for the tumour shape and resolution
            int wantedPointsBetween = Math.Max(pointsBetween, (int)(tumourTemplate.GetLength() * Resolution));  // The needed number of points for the tumour
            pointsBetween += path.Subdivide(start, end, wantedPointsBetween);
            pointsBetween += path.EnsureCriticalPoints(start, end, tumourTemplate.GetCriticalPoints());

            // Add tumour offsets
            var startP = start.Value;
            var endP = end.Value;
            double startT = startP.T;
            double endT = endP.T;
            double dist = endP.CumulativeLength - startP.CumulativeLength;
            double startDist = startP.CumulativeLength;
            pn = start;
            for (int i = 0; i < pointsBetween; i++) {
                pn = pn.Next;
                Debug.Assert(pn != null, nameof(pn) + " != null");
                var p = pn.Value;

                double t = Precision.AlmostEquals(dist, 0) ?
                    (p.T - startT) / (endT - startT) :
                    (p.CumulativeLength - startDist) / dist;

                // Get the offset, original pos, and direction
                var offset = tumourTemplate.GetOffset(t) * Scalar;
                var np = WrappingMode switch {
                    WrappingMode.Wrap => p,
                    WrappingMode.RoundWrap => new PathPoint(p.Pos, Vector2.Lerp(startP.Dir, endP.Dir, t), p.Dist, p.CumulativeLength),
                    WrappingMode.Replace => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), endP.Pos - startP.Pos, p.Dist, p.CumulativeLength),
                    WrappingMode.RoundReplace => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), Vector2.Lerp(startP.Dir, endP.Dir, t), p.Dist, p.CumulativeLength),
                    _ => p
                };

                // Add the offset to the point
                var newPos = np.Pos + Vector2.Rotate(offset, np.Dir.Theta);

                // Modify the path
                pn.Value = new PathPoint(newPos, p.Dir, p.Dist, p.CumulativeLength);
            }
        }
    }
}
