using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen {
    /// <summary>
    /// Version of <see cref="PathGenerator"/> but working with <see cref="PathPoint"/> instead.
    /// </summary>
    public class PathGenerator2 {
        public double MaxAngle { get; set; } = Math.PI * 1 / 3;
        public ApproximationMode Approximation { get; set; } = ApproximationMode.Best;
        public int NumTestPoints { get; set; } = 10;

        /// <summary>
        /// Generates anchors which approximate the path between the given path nodes.
        /// Accurate angle and distances must be calculated on the path beforehand.
        /// </summary>
        /// <returns>Bezier anchors which approximate the given path</returns>
        public IEnumerable<Vector2> GeneratePath(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            var segments = GetNonInflectionSegments(start, end);

            foreach ((LinkedListNode<PathPoint> segmentStart, LinkedListNode<PathPoint> segmentEnd) in segments) {
                yield return segmentStart.Value.Pos;

                Vector2? middle = Approximation switch {
                    ApproximationMode.TangentIntersection => TangentIntersectionApproximation(segmentStart, segmentEnd),
                    ApproximationMode.DoubleMiddle => DoubleMiddleApproximation(segmentStart, segmentEnd),
                    ApproximationMode.Best => BestApproximation(segmentStart, segmentEnd),
                    _ => null
                };

                if (middle.HasValue) {
                    yield return middle.Value;
                }

                yield return segmentEnd.Value.Pos;
            }
        }

        private Vector2? BestApproximation(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            const double nullBias = 1E-3D;

            // Make sure start index is before end index
            // The results will be the same for flipped indices
            if (start.Value > end.Value) {
                (end, start) = (start, end);
            }

            var p1 = start.Value.Pos;
            var p2 = end.Value.Pos;

            var labels = PathHelper.EnumerateBetween(start, end).Select(o => o.Pos).ToList();

            Vector2?[] middles = {
                null,
                TangentIntersectionApproximation(start, end),
                DoubleMiddleApproximation(start, end)
            };

            Vector2? bestMiddle = null;
            double bestLoss = double.PositiveInfinity;

            foreach (var middle in middles) {
                var bezier = new BezierCurveQuadric(p1, p2, middle ?? (p2 + p1) / 2);

                var interpolatedPoints = new Vector2[NumTestPoints];
                for (int i = 0; i < NumTestPoints; i++) {
                    double t = (double) i / (NumTestPoints - 1);
                    interpolatedPoints[i] = bezier.CalculatePoint(t);
                }

                var loss = SliderPathUtil.CalculateLoss(interpolatedPoints, labels);

                // Add some bias towards not using a middle anchor
                if (!middle.HasValue)
                    loss -= nullBias;

                if (Precision.AlmostBigger(loss, bestLoss)) {
                    continue;
                }

                bestLoss = loss;
                bestMiddle = middle;
            }

            return bestMiddle;
        }

        private static Vector2? TangentIntersectionApproximation(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            var p1 = start.Value.Pos;
            var p2 = end.Value.Pos;

            var a1 = start.Value.PostAngle;
            var a2 = end.Value.PreAngle;

            if (Math.Abs(MathHelper.AngleDifference(a1, a2)) < Precision.DoubleEpsilon) {
                return null;
            }

            var t1 = new Line2(p1, a1);
            var t2 = new Line2(p2, a2);

            var middleAnchor = Line2.Intersection(t1, t2);
            if (middleAnchor != Vector2.NaN &&
                Vector2.DistanceSquared(p1, middleAnchor) > 0.5 &&
                Vector2.DistanceSquared(p2, middleAnchor) > 0.5) {
                return middleAnchor;
            }

            return null;
        }

        private Vector2? DoubleMiddleApproximation(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            var p1 = start.Value.Pos;
            var p2 = end.Value.Pos;
            var averagePoint = (p1 + p2) / 2;

            var d1 = start.Value.CumulativeLength;
            var d2 = end.Value.CumulativeLength;
            var dm = (d1 + d2) / 2;
            var middle = GetExactPointAtDistance(start, dm);
            var middlePoint = middle.Pos;


            if (Vector2.DistanceSquared(averagePoint, middlePoint) < 0.1) {
                return null;
            }

            var doubleMiddlePoint = averagePoint + (middlePoint - averagePoint) * 2;

            return doubleMiddlePoint;
        }

        /// <summary>
        /// Calculates the indices of sub-ranges such that the sub-ranges have no inflection points or sharp curves inside.
        /// </summary>
        private List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>)> GetNonInflectionSegments(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end) {
            int dir = Math.Sign(end.Value.CumulativeLength - start.Value.CumulativeLength);

            switch (dir) {
                case 0:
                    return new List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>)> {(start, end)};
                case -1:
                    // If the direction is reversed, just swap the start and end index and then reverse the result at the end
                    (start, end) = (end, start);
                    break;
            }

            double lastAngleChange = 0;
            var startSubRange = start;
            var current = start;
            double subRangeAngleChange = 0;
            var subRanges = new List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>, double)>();
            // Loop through the whole path and divide it into sub-ranges at every inflection point
            while (current is not null && current.Previous != end) {
                var angleChange = current == start || current == end || current.Value.Red ? 0 : MathHelper.AngleDifference(current.Value.PreAngle, current.Value.PostAngle);

                // Check for inflection point or red anchors
                if ((angleChange * lastAngleChange < -1E-5D && current != startSubRange && current.Previous != startSubRange) ||
                    current.Value.Red && current != startSubRange) {
                    subRanges.Add((startSubRange, current, subRangeAngleChange));

                    startSubRange = current;
                    subRangeAngleChange = -Math.Abs(angleChange);  // Negate the angle change because this point invalidates the angle
                }
                else if (Precision.AlmostEquals(angleChange, 0) && !Precision.AlmostEquals(lastAngleChange, 0)) {
                    subRanges.Add((startSubRange, current, subRangeAngleChange));

                    startSubRange = current;
                    subRangeAngleChange = -Math.Abs(angleChange);  // Negate the angle change because this point invalidates the angle
                } else if (!Precision.AlmostEquals(angleChange, 0) && Precision.AlmostEquals(lastAngleChange, 0) && current != startSubRange && current.Previous != startSubRange) {  // Extra check to prevent subranges going backwards with i - 1
                    // Place on the previous index for symmetry with the part going into the zero chain
                    subRanges.Add((startSubRange, current.Previous, 0));

                    startSubRange = current.Previous;
                }

                subRangeAngleChange += Math.Abs(angleChange);

                lastAngleChange = angleChange;
                current = current.Next;
            }

            if (startSubRange != end) {
                subRanges.Add((startSubRange, end, subRangeAngleChange));
            }

            // Remove all sub-ranges which start and end on the same index or start at a later index
            subRanges.RemoveAll(s => s.Item1.Value >= s.Item2.Value);

            List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>)> segments = new List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>)>();
            // Divide each sub-range into evenly spaced segments which have an aggregate angle change less than the max
            foreach ((LinkedListNode<PathPoint> startRange, LinkedListNode<PathPoint> endRange, double angleRange) in subRanges) {
                int numSegments = (int) Math.Floor(angleRange / MaxAngle) + 1;
                double maxSegmentAngle = angleRange / numSegments;

                double segmentAngleChange = 0;
                var startSegment = startRange;
                var currentSegment = startRange;
                // Loop through the sub-range and count the angle change to make even divisions of the angle
                while (currentSegment is not null && currentSegment.Previous != endRange) {
                    var angleChange = currentSegment == startRange || currentSegment == endRange ? 0 :
                        MathHelper.AngleDifference(currentSegment.Value.PreAngle, currentSegment.Value.PostAngle);

                    segmentAngleChange += Math.Abs(angleChange);

                    if (segmentAngleChange > maxSegmentAngle + Precision.DoubleEpsilon) {
                        segments.Add((startSegment, currentSegment));

                        startSegment = currentSegment;
                        segmentAngleChange -= maxSegmentAngle;
                    }

                    currentSegment = currentSegment.Next;
                }

                if (startSegment != endRange) {
                    segments.Add((startSegment, endRange));
                }
            }

            if (dir != -1) {
                return segments;
            }

            // Reverse the result
            List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>)> reversedSegments = new List<(LinkedListNode<PathPoint>, LinkedListNode<PathPoint>)>(segments.Count);

            for (int i = segments.Count - 1; i >= 0; i--) {
                var s = segments[i];
                reversedSegments.Add((s.Item2, s.Item1));
            }

            return reversedSegments;
        }

        private static PathPoint GetExactPointAtDistance(LinkedListNode<PathPoint> start, double distance) {
            var middleFirst = PathHelper.FindFirstOccurrence(start, distance).Value;
            var middleLast = PathHelper.FindLastOccurrence(start, distance).Value;

            if (Precision.AlmostEquals(middleFirst.CumulativeLength, middleLast.CumulativeLength)) {
                return middleFirst;
            }

            var dt = (distance - middleFirst.CumulativeLength) / (middleLast.CumulativeLength - middleFirst.CumulativeLength);
            var middle = PathPoint.Lerp(middleFirst, middleLast, dt);

            return middle;
        }

        public enum ApproximationMode {
            TangentIntersection,
            DoubleMiddle,
            Best
        }
    }
}