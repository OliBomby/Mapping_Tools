using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public static class PathHelper {
        /// <summary>
        /// Modifies <see cref="path"/> such that there are <see cref="count"/> roughly equally spaced
        /// path points between <see cref="start"/> and <see cref="end"/>.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point. This node has to come after start or be equal to start.</param>
        /// <param name="count">The wanted number of points between start and end.</param>
        /// <returns>The new number of points between start and end.</returns>
        public static int Subdivide(this LinkedList<PathPoint> path, LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int count) {
            // Get the cumulative length between start and end
            // Count the number of nodes already between start and end
            // Interpolate path points at roughly equal distance intervals
            int inbetweenCount = 1;
            int pointsToAdd = count - inbetweenCount;

            return count;
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
