﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public static class PathHelper {
        public static PathWithHints CreatePathWithHints(SliderPath sliderPath) {
            var pathWithHints = new PathWithHints();
            var path = pathWithHints.Path;

            // Get all segments of the slider path
            var controlPoints = sliderPath.ControlPoints;
            var segments = new List<List<Vector2>>();
            int start = 0;
            int end = 0;
            for (int i = 0; i < controlPoints.Length(); i++) {
                end++;

                if (i == controlPoints.Length() - 1 || controlPoints[i] == controlPoints[i + 1] && i != controlPoints.Length() - 2) {
                    List<Vector2> cpSpan = controlPoints.GetRange(start, end - start);
                    segments.Add(cpSpan);
                    start = end;
                }
            }

            var calculatedPath = sliderPath.CalculatedPath;
            var segmentsStarts = sliderPath.SegmentStarts;
            var segmentIndex = 0;
            LinkedListNode<PathPoint> segmentStartNode = null;
            for (int i = 0; i < calculatedPath.Count; i++) {
                bool red = false;
                // Check if i is the start of the next segment so we know its a red anchor
                // This is a while loop because there could be multiple identical segment starts in a row
                // which means there are segments with no calculated points
                while (i != 0 && segmentIndex < segmentsStarts.Count && i == segmentsStarts[segmentIndex]) {
                    red = true;
                }

                path.AddLast(new PathPoint(calculatedPath[i], Vector2.UnitX, 0, 0, red: red));

                // Add a reconstruction hint for each segment of the slider path
                segmentStartNode ??= path.Last;
                if (red || i == calculatedPath.Count - 1) {
                    segmentIndex++;
                    pathWithHints.AddReconstructionHint(new ReconstructionHint(segmentStartNode, path.Last, segments[segmentIndex - 1], -1));
                    segmentStartNode = path.Last;
                }
            }

            // Calculate directions and distances
            Recalculate(path);

            return pathWithHints;
        }

        /// <summary>
        /// Recalculates directions and distances of the path.
        /// The linked list nodes stay the same object.
        /// </summary>
        /// <param name="path">The path to recalculate</param>
        public static void Recalculate(LinkedList<PathPoint> path) {
            var current = path.First;
            var cumulativeLength = 0d;
            while (current is not null) {
                var point = current.Value;
                var pos = point.Pos;

                Vector2? v1 = null;
                Vector2? v2 = null;
                double dist = 0;
                if (current.Previous is not null) {
                    var prevPos = current.Previous.Value.Pos;
                    dist = Vector2.Distance(prevPos, pos);
                    v1 = (pos - prevPos).Normalized();
                }

                if (current.Next is not null) {
                    var nextPos = current.Next.Value.Pos;
                    v2 = (nextPos - pos).Normalized();
                }

                cumulativeLength += dist;
                // Calculate the average dir of the previous and next segment
                var dir = v1.HasValue && v2.HasValue ? (v1.Value + v2.Value).LengthSquared < Precision.DOUBLE_EPSILON
                        ? v1.Value
                        : (v1.Value + v2.Value).Normalized() :
                    v1 ?? (v2 ?? Vector2.UnitX);

                // Update the path point of current
                current.Value = new PathPoint(pos, dir, dist, cumulativeLength, point.T, point.Red);

                current = current.Next;
            }
        }

        public static void Interpolate(LinkedListNode<PathPoint> p1, double t) {
            Interpolate(p1, new[] { t });
        }

        /// <summary>
        /// Adds count number of interpolated points equally between p1 and p1.Next.
        /// </summary>
        public static void Interpolate(LinkedListNode<PathPoint> p1, int count) {
            Interpolate(p1, Enumerable.Range(1, count).Select(o => (double) o / (count + 1)));
        }

        public static void Interpolate(LinkedListNode<PathPoint> p1, IEnumerable<double> ts) {
            if (p1.List is null) {
                throw new ArgumentException(@"Point 1 must be part of a linked list.", nameof(p1));
            }

            var p2 = p1.Next;

            PathPoint v1 = p1.Previous is not null && !p1.Value.Red ? p1.Previous.Value : p1.Value;
            PathPoint v2 = p1.Value;
            PathPoint v3 = p2?.Value ?? v2 + v2 - v1;
            PathPoint v4 = p2?.Next is not null && !v3.Red ? p2.Next.Value : v3 + v3 - v2;

            // Normalize v1 and v4 to prevent extreme curvature
            double length = Vector2.Distance(v2.Pos, v3.Pos);
            v1.Pos = (v1.Pos - v2.Pos).LengthSquared > Precision.DOUBLE_EPSILON ? v2.Pos + (v1.Pos - v2.Pos).Normalized() * length : v1.Pos;
            v4.Pos = (v4.Pos - v3.Pos).LengthSquared > Precision.DOUBLE_EPSILON ? v3.Pos + (v4.Pos - v3.Pos).Normalized() * length : v4.Pos;

            foreach (var t in ts) {
                var v = PathPoint.Lerp(v2, v3, t);
                v.Pos = PathApproximator.CatmullFindPoint(ref v1.Pos, ref v2.Pos, ref v3.Pos, ref v4.Pos, t); ;
                var p = new LinkedListNode<PathPoint>(v);
                p1.List!.AddAfter(p1, p);
                p1 = p;
            }
        }

        /// <summary>
        /// Modifies <see cref="path"/> such that there are at least <see cref="wantedCount"/> roughly equally spaced
        /// path points between <see cref="start"/> and <see cref="end"/>.
        /// </summary>
        /// <param name="path">The path to subdivide.</param>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point. This node has to come after start or be equal to start.</param>
        /// <param name="wantedCount">The wanted number of points between start and end.</param>
        /// <returns>The number of points added between start and end.</returns>
        public static int Subdivide(this LinkedList<PathPoint> path, LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int wantedCount) {
            if (ReferenceEquals(start, end)) {
                throw new ArgumentException(@"Start and end points can not be the same.");
            }

            if (wantedCount <= 0) {
                return 0;
            }

            int addedPoints = 0;
            
            // If dist ~= 0 we go to T mode which means interpolate with T values in [0,1]
            // Otherwise dist > 0, tumour T values will be based on distance so it's only necessary
            // to subdivide segments of dist > 0 for that's where resolution is necessary.
            
            // Get the cumulative length between start and end
            var dist = end.Value.CumulativeLength - start.Value.CumulativeLength;
            var tMode = Precision.AlmostEquals(dist, 0);

            if (tMode) {
                // Count the number of nodes already between start and end
                int count = CountPointsBetween(start, end);

                // Interpolate path points at roughly equal distance intervals
                int pointsToAdd = wantedCount - count;
                int pointsToAddToEachSegment = (int) Math.Ceiling(pointsToAdd / (double) (count + 1));
                
                // Add pointsToAddToEachSegment number of points in each segment of the arc
                // TODO: I can actually be smarter about this and base the number of points on the distT of the segment and leave this implementation for the distT = 0 case
                // TODO: Create a SortedList with each point between and the segment distT. Take the largest segment, divide it by a number proportional to distT / total distT, and update list.
                var p = start;
                while (p != end) {
                    var nextP = p!.Next;

                    // Add pointsToAddToEachSegment after p
                    Interpolate(p, pointsToAddToEachSegment);
                    addedPoints += pointsToAddToEachSegment;

                    p = nextP;
                }
            } else {
                // Distance mode

            }

            return addedPoints;
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

        /// <summary>
        /// Counts the number of nodes between the start and end node.
        /// </summary>
        /// <param name="start">The start node.</param>
        /// <param name="end">The end node.</param>
        /// <returns>The number of nodes between the start and end node.</returns>
        public static int CountPointsBetween(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            int count = 0;
            var p = start;
            while (p.Next is not null && p.Next != end) {
                count++;
                p = p.Next;
            }

            return p.Next is null
                ? throw new ArgumentException(
                    @"The end point has to be a node that comes after the starting node in the linked list.",
                    nameof(end))
                : count;
        }
    }
}
