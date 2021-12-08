using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public static class PathHelper {
        /// <summary>
        /// Modifies <see cref="path"/> such that there are <see cref="count"/> roughly equally spaced
        /// path points between <see cref="start"/> and <see cref="end"/>.
        /// </summary>
        /// <param name="start">The start point index.</param>
        /// <param name="end">The end point index. This node has to come after start or be equal to start.</param>
        /// <param name="count">The wanted number of points between start and end.</param>
        public static void Subdivide(this List<PathPoint> path, int start, int end, int count) {
            // Get the cumulative length between start and end
            // Count the number of nodes already between start and end
            // Interpolate path points at roughly equal distance intervals
            int inbetweenCount = 1;
        }

        /// <summary>
        /// Gets the index of <see cref="path"/> with a cumulative length closest to <see cref="cumulativeLength"/>.
        /// </summary>
        /// <param name="cumulativeLength"></param>
        /// <returns></returns>
        public static int getCumulativeLength(this List<PathPoint> path, double cumulativeLength) {
            throw new NotImplementedException();
        }
    }
}
