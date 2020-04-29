using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools {
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
        /// Generates anchors which approximate the path between the given indices
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="maxAngle"></param>
        /// <returns></returns>
        public IEnumerable<Vector2> GeneratePath(int startIndex, int endIndex, double maxAngle = Math.PI * 1 / 4) {
            var segments = GetNonInflectionSegments(startIndex, endIndex, maxAngle);

            foreach (var segment in segments) {
                var p1 = _path[segment.Item1];
                var p2 = _path[segment.Item2];

                yield return p1;

                var t1 = new Line2(p1, _angle[segment.Item1]);
                var t2 = new Line2(p2, _angle[segment.Item2]);

                var middleAnchor = Line2.Intersection(t1, t2);
                if (middleAnchor != Vector2.NaN) {
                    yield return middleAnchor;
                }

                yield return p2;
            }
        }

        /// <summary>
        /// Calculates the indices of sub-ranges such that the sub-ranges have no inflection points or sharp curves inside
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="maxAngle"></param>
        /// <returns></returns>
        public List<Tuple<int, int>> GetNonInflectionSegments(int startIndex, int endIndex, double maxAngle=Math.PI * 1/4) {
            endIndex = Math.Min(endIndex, _angle.Count - 1);

            double lastAngleChange = GetSmallestAngle(_angle[Math.Max(startIndex - 1, 0)], _angle[Math.Max(startIndex - 2, 0)]);
            var lastAngle = _angle[Math.Max(startIndex - 1, 0)];

            int startSubRange = startIndex;
            double subRangeAngleChange = 0;
            List<Tuple<int, int, double>> subRanges = new List<Tuple<int, int, double>>();
            // Loop through the whole path and divide it into sub-ranges at every inflection point
            for (int i = startIndex; i <= endIndex; i++) {
                var angle = _angle[i];
                var angleChange = GetSmallestAngle(angle, lastAngle);

                // Check for inflection point or super sharp angles
                if (angleChange * lastAngleChange < 0 || Math.Abs(angleChange) > Math.PI * 1/8) {
                    subRanges.Add(new Tuple<int, int, double>(startSubRange, i, subRangeAngleChange));

                    startSubRange = i;
                    subRangeAngleChange = -angleChange;  // Negate the angle change because this point invalidates the angle
                }

                subRangeAngleChange += angleChange;

                if (i == endIndex) {
                    subRanges.Add(new Tuple<int, int, double>(startSubRange, i, subRangeAngleChange));
                }

                lastAngle = angle;
                lastAngleChange = angleChange;
            }

            List<Tuple<int, int>> segments = new List<Tuple<int, int>>();
            // Divide each sub-range into evenly spaced segments which have an aggregate angle change less than the max
            foreach (var subRange in subRanges) {
                int numSegments = (int) Math.Ceiling(subRange.Item3 / maxAngle);
                double maxSegmentAngle = subRange.Item3 / numSegments;
                
                lastAngle = _angle[Math.Max(subRange.Item1 - 1, 0)];

                int startSegment = subRange.Item1;
                double segmentAngleChangle = 0;
                // Loop through the sub-range and count the angle change to make even divisions of the angle
                for (int i = subRange.Item1; i <= subRange.Item2; i++) {
                    var angle = _angle[i];
                    var angleChange = GetSmallestAngle(angle, lastAngle);

                    segmentAngleChangle += angleChange;

                    if (segmentAngleChangle >= maxSegmentAngle || i == subRange.Item2) {
                        segments.Add(new Tuple<int, int>(startSegment, i));

                        startSegment = i;
                        segmentAngleChangle -= maxSegmentAngle;
                    }

                    lastAngle = angle;
                }
            }

            return segments;
        }

        private static double Modulo(double a, double n) {
            return a - Math.Floor(a / n) * n;
        }

        private static double GetSmallestAngle(double a1, double a2) {
            return Modulo(a2 - a1 + Math.PI, 2 * Math.PI) - Math.PI;
        }
    }
}