using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public static class PathHelper {
        private static void Interpolate(LinkedListNode<PathPoint> p1, IEnumerable<double> ts) {
            var p2 = p1.Next;
            
            var v1 = p1.Previous?.Value ?? p1.Value;
            var v2 = p1.Value;
            var v3 = p1.Next?.Value ?? v2 + v2 - v1;
            var v4 = p1.Next?.Next?.Value ?? v3 + v3 - v2;

            // Find possible red anchors near p1



            foreach (var t in ts) {
                p1.List.AddAfter(p1, new LinkedListNode<PathPoint>(new PathPoint()));
            }

            for( int c = 0; c < catmull_detail; c++ ) {
                result.Add(CatmullFindPoint(ref v1, ref v2, ref v3, ref v4, (double) c / catmull_detail));
                result.Add(CatmullFindPoint(ref v1, ref v2, ref v3, ref v4, (double) ( c + 1 ) / catmull_detail));
            }
        }

        /// <summary>
        /// Modifies <see cref="path"/> such that there are at least <see cref="count"/> roughly equally spaced
        /// path points between <see cref="start"/> and <see cref="end"/>.
        /// </summary>
        /// <param name="path">The path to subdivide.</param>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point. This node has to come after start or be equal to start.</param>
        /// <param name="count">The wanted number of points between start and end.</param>
        /// <returns>The number of points added between start and end.</returns>
        public static int Subdivide(this LinkedList<PathPoint> path, LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int count) {
            // Get the cumulative length between start and end
            // Count the number of nodes already between start and end
            // Interpolate path points at roughly equal distance intervals
            int inbetweenCount = 1;
            int pointsToAdd = count - inbetweenCount;
            // If start cumulative length == end cumulative length add t values in PathPoint

            return count;
        }

        /// <summary>
        /// Modifies <see cref="path"/> such that each wanted t value becomes an exact point in the
        /// path points between <see cref="start"/> and <see cref="end"/>.
        /// </summary>
        /// <param name="path">The path to subdivide.</param>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point. This node has to come after start or be equal to start.</param>
        /// <param name="criticalPoints">The wanted values of t between start and end.</param>
        /// <returns>The number of points added between start and end.</returns>
        public static int EnsureCriticalPoints(this LinkedList<PathPoint> path, LinkedListNode<PathPoint> start,
            LinkedListNode<PathPoint> end, IEnumerable<double> criticalPoints) {
            // If start cumulative length == end cumulative length add t values in PathPoint
            return 0;
        }

        /// <summary>
        /// Gets the node of <see cref="path"/> with a cumulative length closest to <see cref="cumulativeLength"/>.
        /// </summary>
        /// <param name="path">The path to find the node in</param>
        /// <param name="cumulativeLength">The wanted cumulative length</param>
        /// <returns></returns>
        public static LinkedListNode<PathPoint> GetCumulativeLength(this LinkedList<PathPoint> path, double cumulativeLength) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the node of <see cref="path"/> with a cumulative length equal to <see cref="cumulativeLength"/>.
        /// This will interpolate a new point if no accurate match exists already.
        /// </summary>
        /// <param name="path">The path to find the node in</param>
        /// <param name="cumulativeLength">The wanted cumulative length</param>
        /// <param name="epsilon">The maximum allowed difference in cumulative length</param>
        /// <returns></returns>
        public static LinkedListNode<PathPoint> GetExactCumulativeLength(this LinkedList<PathPoint> path, 
            double cumulativeLength, double epsilon = Precision.DOUBLE_EPSILON) {
            throw new NotImplementedException();
        }
    }
}
