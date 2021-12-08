using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public class TumourGenerator {
        /// <summary>
        /// Places a tumour onto the path between the specified start and end points.
        /// May increase the size of path.
        /// </summary>
        /// <param name="path">The path to add a tumour to</param>
        /// <param name="tumourTemplate">The tumour shape</param>
        /// <param name="start">The index of the start point</param>
        /// <param name="end">The index of the end point</param>
        /// <param name="wrappingMode">The wrapping mode</param>
        public static void PlaceTumour(List<PathPoint> path, ITumourTemplate tumourTemplate, int start, int end, WrappingMode wrappingMode, double resolution = 1, double scalar = 1) {
            if (end < start) {
                throw new ArgumentException("The end index can not be smaller than the start index.");
            }

            // Make sure there are enough points between start and end for the tumour shape and resolution
            int pointsBetween = Math.Max(end - start - 1, 0);  // The current number of points we have
            int neededPointsBetween = (int)(tumourTemplate.GetLength() * resolution);  // The needed number of points for the tumour
            int pointsToAdd = Math.Max(neededPointsBetween - pointsBetween, 0);
            PathHelper.Subdivide(path, start, end, pointsToAdd);
            pointsBetween += pointsToAdd;
            end += pointsToAdd;

            // Add tumour offsets
            var startP = path[start];
            var endP = path[end];
            double dist = path[end].CumulativeLength - path[start].CumulativeLength;
            double startDist = path[start].CumulativeLength;
            for (int i = start + 1; i < end; i++) {
                var p = path[i];
                double t = Precision.AlmostEquals(dist, 0) ?
                    (i - start) / (pointsBetween + 1) :
                    (path[i].CumulativeLength - startDist) / dist;

                // Get the offset, original pos, and direction
                var offset = tumourTemplate.GetOffset(t) * scalar;
                var np = wrappingMode switch {
                    WrappingMode.Wrap => p,
                    WrappingMode.Replace => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), endP.Pos - startP.Pos, p.Dist, p.CumulativeLength),
                    WrappingMode.RoundReplace => new PathPoint(Vector2.Lerp(startP.Pos, endP.Pos, t), Vector2.Lerp(startP.Dir, endP.Dir, t), p.Dist, p.CumulativeLength),
                    _ => p
                };

                // Add the offset to the point
                var newPos = np.Pos + Vector2.Rotate(offset, np.Dir.Theta);

                // Modify the path
                path[i] = new PathPoint(newPos, p.Dir, p.Dist, p.CumulativeLength);
            }
        }
    }
}
