﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen {
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

                if (i != controlPoints.Length() - 1 &&
                    (controlPoints[i] != controlPoints[i + 1] || i == controlPoints.Length() - 2)) {
                    continue;
                }

                List<Vector2> cpSpan = controlPoints.GetRange(start, end - start);
                segments.Add(cpSpan);
                start = end;
            }

            var calculatedPath = sliderPath.CalculatedPath;
            var segmentsStarts = sliderPath.SegmentStarts;
            var segmentIndex = 1;
            var cumulativeLength = 0d;
            LinkedListNode<PathPoint> segmentStartNode = null;
            for (int i = 0; i < calculatedPath.Count; i++) {
                // This is a while loop because there could be multiple identical segment starts in a row
                // which means there are segments with no calculated points
                while (segmentIndex < segmentsStarts.Count && i + 1 > segmentsStarts[segmentIndex]) {
                    segmentIndex++;
                }

                // Check if i+1 is the first point in the next segment so we know i is a red anchor
                bool isRedAnchor = segmentIndex < segmentsStarts.Count && i + 1 == segmentsStarts[segmentIndex] && i != 0;

                // Update cumulative length
                double dist = 0;
                if (path.Last is not null) {
                    var prevPos = path.Last.Value.Pos;
                    dist = Vector2.Distance(prevPos, calculatedPath[i]);
                }
                cumulativeLength += dist;

                path.AddLast(new PathPoint(calculatedPath[i], Vector2.UnitX, cumulativeLength, red: isRedAnchor));

                // Make sure the start node is initialized
                segmentStartNode ??= path.Last;

                if (!isRedAnchor && i != calculatedPath.Count - 1 || i == 0) {
                    continue;
                }

                double endP = 1;
                if (i == calculatedPath.Count - 1) {
                    // This is the last point of the path and the last segment to add
                    // Calculate the directions and distances of the path because it is complete now
                    Recalculate(path);

                    // Adjust the length of the last hint to account for slider pixel length
                    var totalLength = path.Last!.Value.CumulativeLength;
                    var lengthAtStartOfLastHint = segmentStartNode!.Value.CumulativeLength;
                    var remainingLength = totalLength - lengthAtStartOfLastHint;
                    var lastSegmentLength = new SliderPath(sliderPath.Type, segments[segmentIndex - 1].ToArray()).Distance;
                    endP = remainingLength / lastSegmentLength;
                }

                // Add a segment from the previous red anchor to this red anchor
                pathWithHints.AddReconstructionHint(new ReconstructionHint(segmentStartNode, path.Last, -1,
                    segments[segmentIndex - 1], sliderPath.Type, endP: endP));

                segmentStartNode = path.Last;
            }

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
                current.Value = new PathPoint(pos, dir, cumulativeLength, point.T, point.Red);

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

        private static bool InsideViableRange(PathPoint p, double cumLength, double t, double epsilon) {
            return double.IsNaN(t)
                ? Precision.AlmostEquals(p.CumulativeLength, cumLength, epsilon)
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                : p.CumulativeLength == cumLength && Precision.AlmostEquals(p.T, t);
        }

        private static bool BeforeWantedPoint(PathPoint p, double cumLength, double t) {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return p.CumulativeLength < cumLength || (p.CumulativeLength == cumLength && !double.IsNaN(t) && p.T < t);
        }

        /// <summary>
        /// Finds the first point from start which matches the distance and T value within the given epsilon.
        /// If no such point exists, the point before the wanted distance is returned.
        /// </summary>
        /// <param name="start">The start node for the search.</param>
        /// <param name="cumLength">The wanted distance.</param>
        /// <param name="t">The wanted T value. If NaN then it is ignored.</param>
        /// <param name="epsilon">The maximum difference in distance or T.</param>
        /// <returns>The first matching occurrence from start.</returns>
        // Finds the first point from start which matches the distance and T value within the given epsilon.
        // If no such point exists, the point before the wanted distance is returned.
        public static LinkedListNode<PathPoint> FindFirstOccurrence(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DOUBLE_EPSILON) {
            var p = start;
            var prev = start;

            if (BeforeWantedPoint(start.Value, cumLength - epsilon, t - epsilon)) {
                // Search forwards
                // Either the current point is inside viable range and the previous point is outside viable range or
                // the current point is before the viable range and the next point is after the viable range.
                while (p is not null) {
                    if (InsideViableRange(p.Value, cumLength, t, epsilon) && !InsideViableRange(prev.Value, cumLength, t, epsilon)) {
                        return p;
                    }
                    if (!InsideViableRange(prev.Value, cumLength, t, epsilon) && !BeforeWantedPoint(p.Value, cumLength, t) && BeforeWantedPoint(prev.Value, cumLength, t)) {
                        return prev;
                    }

                    prev = p;
                    p = p.Next;
                }
            } else {
                // Search backwards
                // Either the next point is outside viable range and the current point is inside viable range or
                // the current point is before the viable range and the previous point is after the viable range.
                while (p is not null) {
                    if (!InsideViableRange(p.Value, cumLength, t, epsilon) && InsideViableRange(prev.Value, cumLength, t, epsilon)) {
                        return prev;
                    }
                    if (!InsideViableRange(p.Value, cumLength, t, epsilon) && BeforeWantedPoint(p.Value, cumLength, t) && !BeforeWantedPoint(prev.Value, cumLength, t)) {
                        return p;
                    }

                    prev = p;
                    p = p.Next;
                }
            }

            return prev;
        }

        /// <summary>
        /// Finds the last point from start which matches the distance and T value within the given epsilon.
        /// If no such point exists, the point after the wanted distance is returned.
        /// </summary>
        /// <param name="start">The start node for the search.</param>
        /// <param name="cumLength">The wanted distance.</param>
        /// <param name="t">The wanted T value. If NaN then it is ignored.</param>
        /// <param name="epsilon">The maximum difference in distance or T.</param>
        /// <returns>The last matching occurrence from start.</returns>
        public static LinkedListNode<PathPoint> FindLastOccurrence(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DOUBLE_EPSILON) {
            var p = start;
            var prev = start;

            if (BeforeWantedPoint(start.Value, cumLength + epsilon, t + epsilon)) {
                // Search forwards
                // Either the current point is inside viable range and the next point is outside viable range or
                // the current point is after the viable range and the previous point is before the viable range.
                while (p is not null) {
                    if (InsideViableRange(prev.Value, cumLength, t, epsilon) && !InsideViableRange(p.Value, cumLength, t, epsilon)) {
                        return prev;
                    }
                    if (!InsideViableRange(p.Value, cumLength, t, epsilon) && !BeforeWantedPoint(p.Value, cumLength, t) && BeforeWantedPoint(prev.Value, cumLength, t)) {
                        return p;
                    }

                    prev = p;
                    p = p.Next;
                }
            } else {
                // Search backwards
                // Either the previous point is outside viable range and the current point is inside viable range or
                // the current point is after the viable range and the next point is before the viable range.
                while (p is not null) {
                    if (!InsideViableRange(prev.Value, cumLength, t, epsilon) && InsideViableRange(p.Value, cumLength, t, epsilon)) {
                        return p;
                    }
                    if (!InsideViableRange(prev.Value, cumLength, t, epsilon) && BeforeWantedPoint(p.Value, cumLength, t) && !BeforeWantedPoint(prev.Value, cumLength, t)) {
                        return prev;
                    }

                    prev = p;
                    p = p.Next;
                }
            }

            return prev;
        }

        /// <summary>
        /// Finds the first point from start which matches the distance and T value within the given epsilon.
        /// If no such point exists, a new point is inserted into the list which matches the distance and T exactly.
        /// </summary>
        /// <param name="start">The start node for the search.</param>
        /// <param name="cumLength">The wanted distance.</param>
        /// <param name="t">The wanted T value. If NaN then it is ignored.</param>
        /// <param name="epsilon">The maximum difference in distance or T.</param>
        /// <returns>The first matching occurrence from start.</returns>
        public static LinkedListNode<PathPoint> FindFirstOccurrenceExact(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DOUBLE_EPSILON) {
            var node = FindFirstOccurrence(start, cumLength, t, epsilon);

            if (InsideViableRange(node.Value, cumLength, t, epsilon) || !BeforeWantedPoint(node.Value, cumLength, t) || node.Next is null) {
                return node;
            }

            // The point we want is between node and node.Next.
            double dt;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (double.IsNaN(t) || node.Value.CumulativeLength != node.Next.Value.CumulativeLength) {
                dt = (cumLength - node.Value.CumulativeLength) / (node.Next.Value.CumulativeLength - node.Value.CumulativeLength);
            } else {
                dt = (cumLength - node.Value.T) / (node.Next.Value.T - node.Value.T);
            }

            Interpolate(node, dt);
            return node.Next;
        }

        /// <summary>
        /// Finds the last point from start which matches the distance and T value within the given epsilon.
        /// If no such point exists, a new point is inserted into the list which matches the distance and T exactly.
        /// </summary>
        /// <param name="start">The start node for the search.</param>
        /// <param name="cumLength">The wanted distance.</param>
        /// <param name="t">The wanted T value. If NaN then it is ignored.</param>
        /// <param name="epsilon">The maximum difference in distance or T.</param>
        /// <returns>The last matching occurrence from start.</returns>
        public static LinkedListNode<PathPoint> FindLastOccurrenceExact(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DOUBLE_EPSILON) {
            var node = FindLastOccurrence(start, cumLength, t, epsilon);

            if (InsideViableRange(node.Value, cumLength, t, epsilon) || BeforeWantedPoint(node.Value, cumLength, t) || node.Previous is null) {
                return node;
            }

            // The point we want is between node and node.Previous.
            double dt;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (double.IsNaN(t) || node.Value.CumulativeLength != node.Previous.Value.CumulativeLength) {
                dt = (cumLength - node.Previous.Value.CumulativeLength) / (node.Value.CumulativeLength - node.Previous.Value.CumulativeLength);
            } else {
                dt = (cumLength - node.Previous.Value.T) / (node.Value.T - node.Previous.Value.T);
            }

            Interpolate(node.Previous, dt);
            return node.Previous;
        }
    }
}
