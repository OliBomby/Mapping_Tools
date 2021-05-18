using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers {
    /// <summary>
    /// This class can generate bezier anchors which approximate arbitrary paths
    /// </summary>
    public class PathGenerator {
        private List<Vector2> _path; // input path
        private List<Vector2> _diff; // path segments
        private List<double> _angle; // path segment angles
        private List<double> _diffL; // length of segments
        private List<double> _pathL; // cumulative length

        public PathGenerator(List<Vector2> path) {
            SetPath(path);
        }

        public PathGenerator(List<Vector2> path, List<Vector2> diff, List<double> angle, List<double> diffL, List<double> pathL) {
            _path = path;
            _diff = diff;
            _angle = angle;
            _diffL = diffL;
            _pathL = pathL;
        }

        public void SetPath(List<Vector2> pathPoints) {
            _path = new List<Vector2> { pathPoints.First() };
            _diff = new List<Vector2>();
            _angle = new List<double>();
            _diffL = new List<double>();
            double sum = 0;
            _pathL = new List<double> { sum };

            foreach (var point in pathPoints.Skip(1)) {
                var diff = point - _path.Last();
                var dist = diff.Length;

                if (dist < Precision.DOUBLE_EPSILON) continue;

                _path.Add(point);
                _diff.Add(diff);
                _angle.Add(diff.Theta);
                _diffL.Add(dist);
                sum += dist;
                _pathL.Add(sum);
            }

            // Add last member again so these lists have the same number of elements as path
            _diff.Add(_diff.Last());
            _angle.Add(_angle.Last());
            _diffL.Add(_diffL.Last());
        }
        /// <summary>
        /// Generates anchors which approximate the entire path
        /// </summary>
        /// <param name="maxAngle"></param>
        /// <returns></returns>
        public IEnumerable<Vector2> GeneratePath(double maxAngle = Math.PI * 1 / 4) {
            return GeneratePath(0, _path.Count - 1, maxAngle);
        }

        /// <summary>
        /// Generates anchors which approximate the path between the given indices
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="maxAngle"></param>
        /// <param name="approximationMode"></param>
        /// <returns></returns>
        public IEnumerable<Vector2> GeneratePath(double startIndex, double endIndex, 
            double maxAngle = Math.PI * 1 / 4, ApproximationMode approximationMode = ApproximationMode.Best) {
            var segments = GetNonInflectionSegments(startIndex, endIndex, maxAngle);

            foreach (var segment in segments) {
                var p1 = GetContinuousPosition(segment.Item1);
                var p2 = GetContinuousPosition(segment.Item2);

                yield return p1;

                Vector2? middle;
                switch (approximationMode) {
                    case ApproximationMode.TangentIntersection:
                        middle = TangentIntersectionApproximation(segment.Item1, segment.Item2);
                        break;
                    case ApproximationMode.DoubleMiddle:
                        middle = DoubleMiddleApproximation(segment.Item1, segment.Item2);
                        break;
                    case ApproximationMode.Best:
                        middle = BestApproximation(segment.Item1, segment.Item2);
                        break;
                    default:
                        middle = null;
                        break;
                }

                if (middle.HasValue) {
                    yield return middle.Value;
                }

                yield return p2;
            }
        }

        private Vector2? BestApproximation(double startIndex, double endIndex) {
            // Make sure start index is before end index
            // The results will be the same for flipped indices
            if (startIndex > endIndex) {
                var tempEndIndex = endIndex;
                endIndex = startIndex;
                startIndex = tempEndIndex;
            }

            var p1 = GetContinuousPosition(startIndex);
            var p2 = GetContinuousPosition(endIndex);

            const int numTestPoints = 100;
            var labels = _path.GetRange((int) startIndex, (int) Math.Ceiling(endIndex) - (int) startIndex + 1);

            Vector2?[] middles = {
                TangentIntersectionApproximation(startIndex, endIndex), 
                DoubleMiddleApproximation(startIndex, endIndex)
            };

            Vector2? bestMiddle = null;
            double bestLoss = double.PositiveInfinity;

            foreach (var middle in middles) {
                var bezier = new BezierCurveQuadric(p1, p2, middle ?? (p2 - p1) / 2);

                var interpolatedPoints = new Vector2[numTestPoints];
                for (int i = 0; i < numTestPoints; i++) {
                    double t = (double) i / (numTestPoints - 1);
                    interpolatedPoints[i] = bezier.CalculatePoint(t);
                }

                var loss = SliderPathUtil.CalculateLoss(interpolatedPoints, labels);

                if (loss < bestLoss) {
                    bestLoss = loss;
                    bestMiddle = middle;
                }
            }

            return bestMiddle;
        }

        private Vector2? TangentIntersectionApproximation(double startIndex, double endIndex) {
            var p1 = GetContinuousPosition(startIndex);
            var p2 = GetContinuousPosition(endIndex);

            var a1 = GetContinuousAngle(startIndex);
            var a2 = GetContinuousAngle(endIndex - 2 * Precision.DOUBLE_EPSILON);

            if (Math.Abs(GetSmallestAngle(a1, a2)) > 0.1) {
                var t1 = new Line2(p1, a1);
                var t2 = new Line2(p2, a2);

                var middleAnchor = Line2.Intersection(t1, t2);
                if (middleAnchor != Vector2.NaN &&
                    Vector2.DistanceSquared(p1, middleAnchor) > 0.5 &&
                    Vector2.DistanceSquared(p2, middleAnchor) > 0.5) {
                    return middleAnchor;
                }
            }

            return null;
        }

        private Vector2? DoubleMiddleApproximation(double startIndex, double endIndex) {
            var p1 = GetContinuousPosition(startIndex);
            var p2 = GetContinuousPosition(endIndex);

            var d1 = GetContinuousDistance(startIndex);
            var d2 = GetContinuousDistance(endIndex);
            var middleIndex = GetIndexAtDistance((d1 + d2) / 2);

            var averagePoint = (p1 + p2) / 2;
            var middlePoint = GetContinuousPosition(middleIndex);

            if (Vector2.DistanceSquared(averagePoint, middlePoint) < 0.1) {
                return null;
            }

            var doubleMiddlePoint = averagePoint + (middlePoint - averagePoint) * 2;

            return doubleMiddlePoint;
        }

        /// <summary>
        /// Calculates the indices of sub-ranges such that the sub-ranges have no inflection points or sharp curves inside.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="maxAngle"></param>
        /// <returns></returns>
        public List<Tuple<double, double>> GetNonInflectionSegments(double startIndex, double endIndex, double maxAngle=Math.PI * 1/4) {
            int dir = Math.Sign(endIndex - startIndex);

            if (dir == 0) {
                return new List<Tuple<double, double>> {new Tuple<double, double>(startIndex, endIndex)};
            }

            // If the direction is reversed, just swap the start and end index and then reverse the result at the end
            if (dir == -1) {
                var temp = endIndex;
                endIndex = startIndex;
                startIndex = temp;
            }

            endIndex = MathHelper.Clamp(endIndex, 0, _angle.Count - 1);

            int startIndexInt = (int) Math.Ceiling(startIndex);
            int endIndexInt = (int) Math.Floor(endIndex);

            double lastAngleChange = 0;
            var lastAngle = GetContinuousAngle(startIndex);

            double startSubRange = startIndex;
            double subRangeAngleChange = 0;
            List<Tuple<double, double, double>> subRanges = new List<Tuple<double, double, double>>();
            // Loop through the whole path and divide it into sub-ranges at every inflection point
            //Console.WriteLine($"Iterating from {startIndexInt} to {endIndexInt}");
            for (int i = startIndexInt; i <= endIndexInt; i++) {
                var pos = _path[i];
                var angle = _angle[i];
                var angleChange = GetSmallestAngle(angle, lastAngle);
                //Console.WriteLine("Angle change: " + angleChange);

                // Check for inflection point or red anchors
                if ((angleChange * lastAngleChange < -Precision.DOUBLE_EPSILON && Math.Abs(startSubRange - i) > 1) ||
                    ((pos - pos.Rounded()).LengthSquared < Precision.DOUBLE_EPSILON && Math.Abs(angleChange) > Precision.DOUBLE_EPSILON)) {
                    subRanges.Add(new Tuple<double, double, double>(startSubRange, i, subRangeAngleChange));

                    //Console.WriteLine($"Adding segment for inflection point or red: {startSubRange} to {i}");
                    //Console.WriteLine($"Found inflection point or red anchor: {angleChange}, {lastAngleChange}, {pos}, {angleChange * lastAngleChange}");

                    startSubRange = i;
                    subRangeAngleChange = -Math.Abs(angleChange);  // Negate the angle change because this point invalidates the angle
                }
                else if (angleChange == 0 && lastAngleChange != 0) {
                    subRanges.Add(new Tuple<double, double, double>(startSubRange, i, subRangeAngleChange));

                    //Console.WriteLine($"Adding segment for start zero angle change: {startSubRange} to {i}");
                    //Console.WriteLine($"start of zero angle change: {angleChange}, {lastAngleChange}, {pos}, {angleChange * lastAngleChange}");

                    startSubRange = i;
                    subRangeAngleChange = -Math.Abs(angleChange);  // Negate the angle change because this point invalidates the angle
                } else if (angleChange != 0 && lastAngleChange == 0 && i - 1 >= startSubRange) {  // Extra check to prevent subranges going backwards with i - 1
                    // Place on the previous index for symmetry with the part going into the zero chain
                    subRanges.Add(new Tuple<double, double, double>(startSubRange, i - 1, 0));
                    
                    //Console.WriteLine($"Adding segment for end zero angle change: {startSubRange} to {i}");
                    //Console.WriteLine($"end of zero angle change: {angleChange}, {lastAngleChange}, {pos}, {angleChange * lastAngleChange}");

                    startSubRange = i - 1;
                }

                subRangeAngleChange += Math.Abs(angleChange);

                lastAngle = angle;
                lastAngleChange = angleChange;
            }

            if (Math.Abs(startSubRange - endIndex) > Precision.DOUBLE_EPSILON) {
                subRanges.Add(new Tuple<double, double, double>(startSubRange, endIndex, subRangeAngleChange));
            }

            // Remove all sub-ranges which start and end on the same index or start at a later index
            subRanges.RemoveAll(s => s.Item1 >= s.Item2);

            List<Tuple<double, double>> segments = new List<Tuple<double, double>>();
            // Divide each sub-range into evenly spaced segments which have an aggregate angle change less than the max
            foreach (var subRange in subRanges) {
                int numSegments = (int) Math.Floor(subRange.Item3 / maxAngle) + 1;
                //Console.WriteLine("Num segments: " + numSegments);
                //Console.WriteLine("sub-range angle: " + subRange.Item3);
                double maxSegmentAngle = subRange.Item3 / numSegments;

                int segmentStartIndexInt = (int) Math.Ceiling(subRange.Item1);
                int segmentEndIndexInt = (int) Math.Floor(subRange.Item2);

                lastAngle = GetContinuousAngle(subRange.Item1);

                double startSegment = subRange.Item1;
                double segmentAngleChange = 0;
                // Loop through the sub-range and count the angle change to make even divisions of the angle
                //Console.WriteLine($"Iterating subrange from {segmentStartIndexInt} to {segmentEndIndexInt}");
                for (int i = segmentStartIndexInt; i <= segmentEndIndexInt; i++) {
                    var angle = _angle[i];
                    var angleChange = GetSmallestAngle(angle, lastAngle);

                    segmentAngleChange += Math.Abs(angleChange);

                    if (segmentAngleChange > maxSegmentAngle + Precision.DOUBLE_EPSILON) {
                        segments.Add(new Tuple<double, double>(startSegment, i));
                        //Console.WriteLine($"Adding segment for angle: {startSegment} to {i}");

                        startSegment = i;
                        segmentAngleChange -= maxSegmentAngle;
                    }

                    lastAngle = angle;
                }

                if (Math.Abs(startSegment - subRange.Item2) > Precision.DOUBLE_EPSILON) {
                    segments.Add(new Tuple<double, double>(startSegment, subRange.Item2));
                    //Console.WriteLine($"Adding segment at the end: {startSegment}, {subRange.Item2}");
                }
            }

            // Reverse the result
            if (dir == -1) {
                List<Tuple<double, double>> reversedSegments = new List<Tuple<double, double>>(segments.Count);

                for (int i = segments.Count - 1; i >= 0; i--) {
                    var s = segments[i];
                    reversedSegments.Add(new Tuple<double, double>(s.Item2, s.Item1));
                }

                return reversedSegments;
            }

            return segments;
        }

        public Vector2 GetContinuousPosition(double index) {
            int segmentIndex = (int) Math.Floor(index);
            double segmentProgression = index - segmentIndex;

            return Math.Abs(segmentProgression) < Precision.DOUBLE_EPSILON ? 
                _path[segmentIndex] : 
                Math.Abs(segmentProgression - 1) < Precision.DOUBLE_EPSILON ?
                    _path[segmentIndex + 1] :
                Vector2.Lerp(_path[segmentIndex], _path[segmentIndex + 1], segmentProgression);
        }

        public double GetContinuousAngle(double index) {
            int segmentIndex = MathHelper.Clamp((int)Math.Floor(index + Precision.DOUBLE_EPSILON), 0, _angle.Count - 1);

            return _angle[segmentIndex];
        }

        public double GetContinuousDistance(double index) {
            int segmentIndex = (int)Math.Floor(index);
            double segmentProgression = index - segmentIndex;

            return Math.Abs(segmentProgression) < Precision.DOUBLE_EPSILON ?
                _pathL[segmentIndex] :
                Math.Abs(segmentProgression - 1) < Precision.DOUBLE_EPSILON ?
                    _pathL[segmentIndex + 1] :
                    (1 - segmentProgression) * _pathL[segmentIndex] + segmentProgression * _pathL[segmentIndex + 1];
        }

        public double GetIndexAtDistance(double distance) {
            var index = _pathL.BinarySearch(distance);
            if (index >= 0) {
                return index;
            }

            var i2 = ~index;
            var i1 = i2 - 1;
            var d1 = _pathL[i1];
            var d2 = _pathL[i2];

            return (distance - d1) / (d2 - d1) + i1;
        }

        private static double Modulo(double a, double n) {
            return a - Math.Floor(a / n) * n;
        }

        private static double GetSmallestAngle(double a1, double a2) {
            return Modulo(a2 - a1 + Math.PI, 2 * Math.PI) - Math.PI;
        }

        public static double CalculatePathLength(List<Vector2> anchors) {
            double length = 0;

            int start = 0;
            int end = 0;

            for (int i = 0; i < anchors.Length(); ++i) {
                end++;

                if (i == anchors.Length() - 1 || anchors[i] == anchors[i + 1]) {
                    List<Vector2> cpSpan = anchors.GetRange(start, end - start);

                    length += new BezierSubdivision(cpSpan).SubdividedApproximationLength();

                    start = end;
                }
            }

            return length;
        }

        public enum ApproximationMode {
            TangentIntersection,
            DoubleMiddle,
            Best
        }
    }
}