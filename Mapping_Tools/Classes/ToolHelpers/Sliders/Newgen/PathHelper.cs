using System;
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

                path.AddLast(new PathPoint(calculatedPath[i], calculatedPath[i], 0, 0, cumulativeLength, red: isRedAnchor));

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
            var lastAngle = double.NaN;
            while (current is not null) {
                var point = current.Value;
                var pos = point.Pos;

                Vector2? v1 = null;
                Vector2? v2 = null;
                double dist = 0;
                if (current.Previous is not null) {
                    var prevPos = current.Previous.Value.Pos;
                    dist = Vector2.Distance(prevPos, pos);
                    v1 = pos - prevPos;
                }

                if (current.Next is not null) {
                    var nextPos = current.Next.Value.Pos;
                    v2 = nextPos - pos;
                }

                cumulativeLength += dist;

                // Calculate the angles
                lastAngle = v1.HasValue
                    ? v1.Value.LengthSquared > Precision.DoubleEpsilon ? v1.Value.Theta : lastAngle
                    : double.NaN;
                var nextAngle = v2.HasValue
                    ? v2.Value.LengthSquared > Precision.DoubleEpsilon ? v2.Value.Theta : lastAngle
                    : double.NaN;

                // Update the path point of current
                current.Value = new PathPoint(pos, pos, lastAngle, nextAngle, cumulativeLength, point.T, point.Red);

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
            if (p1.Next is null) {
                throw new ArgumentException(@"Point 1 must have a successor.", nameof(p1));
            }

            var p2 = p1.Next;

            PathPoint v2 = p1.Value;
            PathPoint v3 = p2.Value;
            PathPoint v1 = p1.Previous is not null && !p1.Value.Red ? p1.Previous.Value : v2 + v2 - v3;
            PathPoint v4 = p2?.Next is not null && !v3.Red ? p2.Next.Value : v3 + v3 - v2;

            // Normalize v1 and v4 to prevent extreme curvature
            double length = Vector2.Distance(v2.Pos, v3.Pos);
            v1.Pos = (v1.Pos - v2.Pos).LengthSquared > Precision.DoubleEpsilon ? v2.Pos + (v1.Pos - v2.Pos).Normalized() * length : v2.Pos + v2.Pos - v3.Pos;
            v4.Pos = (v4.Pos - v3.Pos).LengthSquared > Precision.DoubleEpsilon ? v3.Pos + (v4.Pos - v3.Pos).Normalized() * length : v3.Pos + v3.Pos - v2.Pos;
            double ogLength = Vector2.Distance(v2.OgPos, v3.OgPos);
            v1.OgPos = (v1.OgPos - v2.OgPos).LengthSquared > Precision.DoubleEpsilon ? v2.OgPos + (v1.OgPos - v2.OgPos).Normalized() * ogLength : v2.OgPos + v2.OgPos - v3.OgPos;
            v4.OgPos = (v4.OgPos - v3.OgPos).LengthSquared > Precision.DoubleEpsilon ? v3.OgPos + (v4.OgPos - v3.OgPos).Normalized() * ogLength : v3.OgPos + v3.OgPos - v2.OgPos;

            foreach (var t in ts) {
                var v = PathPoint.Lerp(v2, v3, t);
                v.Pos = PathApproximator.CatmullFindPoint(ref v1.Pos, ref v2.Pos, ref v3.Pos, ref v4.Pos, t);
                v.OgPos = PathApproximator.CatmullFindPoint(ref v1.OgPos, ref v2.OgPos, ref v3.OgPos, ref v4.OgPos, t);
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
                // Interpolate path points at roughly equal distance intervals
                var wantedGap = (end.Value.T - start.Value.T) / wantedCount;
                
                // Interpolate all segments which are too big
                var p = start;
                while (p != end) {
                    var nextP = p!.Next;

                    var gap = p.Next!.Value.T - p.Value.T;
                    if (Precision.DefinitelyBigger(gap, wantedGap)) {
                        var pointsToAdd = (int) Math.Ceiling(gap / wantedGap) - 1;
                        Interpolate(p, pointsToAdd);
                        addedPoints += pointsToAdd;
                    }

                    p = nextP;
                }
            } else {
                // Distance mode
                // Interpolate path points at roughly equal distance intervals
                var wantedGap = dist / wantedCount;

                // Interpolate all segments which are too big
                var p = start;
                while (p != end) {
                    var nextP = p!.Next;

                    var gap = p.Next!.Value.CumulativeLength - p.Value.CumulativeLength;
                    if (Precision.DefinitelyBigger(gap, wantedGap)) {
                        var pointsToAdd = (int) Math.Ceiling(gap / wantedGap) - 1;
                        Interpolate(p, pointsToAdd);
                        addedPoints += pointsToAdd;
                    }

                    p = nextP;
                }
            }

            return addedPoints;
        }

        /// <summary>
        /// Modifies <see cref="path"/> such that each wanted t value becomes an exact point in the
        /// path points between <see cref="start"/> and <see cref="end"/>.
        /// Also makes sure the start, end, and critical points are marked as red.
        /// </summary>
        /// <param name="path">The path to subdivide.</param>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point. This node has to come after start or be equal to start.</param>
        /// <param name="startTemplateT">The template T of the start node.</param>
        /// <param name="endTemplateT">The template t of the end node.</param>
        /// <param name="criticalPoints">The wanted values of t between start and end.</param>
        /// <param name="ensuredPoints">The critical points found or created by this algorithm.</param>
        /// <returns>The number of points added between start and end.</returns>
        public static int EnsureCriticalPoints(this LinkedList<PathPoint> path, LinkedListNode<PathPoint> start,
            LinkedListNode<PathPoint> end, double startTemplateT, double endTemplateT, IEnumerable<double> criticalPoints, out LinkedList<LinkedListNode<PathPoint>> ensuredPoints) {
            var startCount = path.Count;
            ensuredPoints = new LinkedList<LinkedListNode<PathPoint>>();

            // If start cumulative length == end cumulative length add t values in PathPoint
            foreach (double criticalPoint in criticalPoints.Where(t => t >= startTemplateT && t <= endTemplateT)) {
                if (Precision.AlmostEquals(start.Value.CumulativeLength, end.Value.CumulativeLength)) {
                    var t = (criticalPoint - startTemplateT) / (endTemplateT - startTemplateT) *
                        (end.Value.T - start.Value.T) + start.Value.T;
                    var node = FindFirstOccurrenceExact(start, start.Value.CumulativeLength, t);
                    if (ensuredPoints.Last is null || ensuredPoints.Last.Value.Value < node.Value)
                        ensuredPoints.AddLast(node);
                } else {
                    var t = (criticalPoint - startTemplateT) / (endTemplateT - startTemplateT) *
                        (end.Value.CumulativeLength - start.Value.CumulativeLength) + start.Value.CumulativeLength;
                    var node = FindFirstOccurrenceExact(start, t);
                    if (ensuredPoints.Last is null || ensuredPoints.Last.Value.Value < node.Value)
                        ensuredPoints.AddLast(node);
                }
            }

            return path.Count - startCount;
        }

        /// <summary>
        /// Ensures there is at least one interplated point between red points after tumour has finished placing.
        /// </summary>
        /// <param name="path">The path to subdivide.</param>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point. This node has to come after start or be equal to start.</param>
        /// <param name="ensuredPoints">Critical points, can be null.</param>
        /// <returns>The number of points added between start and end.</returns>
        public static int EnsureLocalCurvature(this LinkedList<PathPoint> path, LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, IEnumerable<LinkedListNode<PathPoint>> ensuredPoints) {
            if (ReferenceEquals(start, end)) {
                throw new ArgumentException(@"Start and end points can not be the same.");
            }

            int addedPoints = 0;

            var previousRed = FindPreviousRed(start);
            if (previousRed is not null) {
                addedPoints += path.Subdivide(previousRed, start, 2);
            }

            var prev = start;
            if (ensuredPoints is not null) {
                foreach (var ensuredPoint in ensuredPoints) {
                    if (ensuredPoint.Value > prev.Value) {
                        addedPoints += path.Subdivide(prev, ensuredPoint, 2);
                    }
                    prev = ensuredPoint;
                }
            }

            if (end.Value > prev.Value) {
                addedPoints += path.Subdivide(prev, end, 2);
            }

            var nextRed = FindNextRed(end);
            if (nextRed is not null) {
                addedPoints += path.Subdivide(end, nextRed, 2);
            }

            return addedPoints;
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

        /// <summary>
        /// Iterates the range of points from start to end.
        /// </summary>
        /// <param name="start">The start node</param>
        /// <param name="end">The end node</param>
        /// <returns>The list containing points between start and end inclusive</returns>
        public static IEnumerable<PathPoint> EnumerateBetween(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            if (start.Value > end.Value) {
                throw new ArgumentException(@"The end point has to be a points that comes after the starting point in the path.");
            }

            var p = start;
            while (p is not null && p.Previous != end) {
                yield return p.Value;
                p = p.Next;
            }
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
        public static LinkedListNode<PathPoint> FindFirstOccurrence(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DoubleEpsilon) {
            var p = start;
            var prev = start;

            if (BeforeWantedPoint(start.Value, double.IsNaN(t) ? cumLength - epsilon : cumLength, t - epsilon)) {
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
                    p = p.Previous;
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
        public static LinkedListNode<PathPoint> FindLastOccurrence(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DoubleEpsilon) {
            var p = start;
            var prev = start;

            if (BeforeWantedPoint(start.Value, double.IsNaN(t) ? cumLength + epsilon : cumLength, t + epsilon)) {
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
                    p = p.Previous;
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
        public static LinkedListNode<PathPoint> FindFirstOccurrenceExact(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DoubleEpsilon) {
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
                dt = (t - node.Value.T) / (node.Next.Value.T - node.Value.T);
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
        public static LinkedListNode<PathPoint> FindLastOccurrenceExact(LinkedListNode<PathPoint> start, double cumLength, double t = double.NaN, double epsilon = Precision.DoubleEpsilon) {
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
                dt = (t - node.Previous.Value.T) / (node.Value.T - node.Previous.Value.T);
            }

            Interpolate(node.Previous, dt);
            return node.Previous;
        }

        public static LinkedListNode<PathPoint> FindNextRed(LinkedListNode<PathPoint> start) {
            var current = start?.Next;
            while (current is not null) {
                if (current.Value.Red || current.Next is null) {
                    return current;
                }
                current = current.Next;
            }

            return null;
        }

        public static LinkedListNode<PathPoint> FindPreviousRed(LinkedListNode<PathPoint> start) {
            var current = start?.Previous;
            while (current is not null) {
                if (current.Value.Red || current.Previous is null) {
                    return current;
                }
                current = current.Previous;
            }

            return null;
        }
    }
}
