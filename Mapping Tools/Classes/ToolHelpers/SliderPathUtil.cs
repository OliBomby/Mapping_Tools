using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers {
    public class SliderPathUtil {
        public static List<Vector2> MoveAnchorsToLength(List<Vector2> anchors, PathType pathType, double newLength, out PathType newPathType) {
            var newAnchors = new List<Vector2>();
            var sliderPath = new SliderPath(pathType, anchors.ToArray(), newLength);
            var maxSliderPath = new SliderPath(pathType, anchors.ToArray());

            if (newLength > maxSliderPath.Distance) {
                // Extend linearly
                switch (pathType) {
                    case PathType.Bezier:
                        newPathType = PathType.Bezier;
                        newAnchors.AddRange(anchors);

                        if (newAnchors.Count > 1 && newAnchors[newAnchors.Count - 2] == newAnchors[newAnchors.Count - 1]) {
                            newAnchors[newAnchors.Count - 2] = newAnchors[newAnchors.Count - 2] + Vector2.UnitX;
                        }

                        newAnchors.Add(anchors.Last());
                        newAnchors.Add(sliderPath.PositionAt(1));
                        break;
                    case PathType.Catmull:
                    case PathType.PerfectCurve:
                        // Convert to bezier and then extend
                        newPathType = PathType.Bezier;
                        newAnchors = BezierConverter.ConvertToBezier(sliderPath).ControlPoints;
                        newAnchors.Add(anchors.Last());
                        newAnchors.Add(sliderPath.PositionAt(1));
                        break;
                    default:
                        newPathType = pathType;
                        newAnchors.AddRange(anchors);
                        newAnchors[newAnchors.Count - 1] = sliderPath.PositionAt(1);
                        break;
                }
            } else {
                switch (sliderPath.Type) {
                    case PathType.Catmull:
                    case PathType.Bezier:
                        newPathType = PathType.Bezier;

                        // Convert in case the path type is catmull
                        var convert = BezierConverter.ConvertToBezier(sliderPath).ControlPoints;

                        // Find the last bezier segment and the pixel length at that part
                        BezierSubdivision subdivision = null;
                        double totalLength = 0;

                        foreach (var bezierSubdivision in ChopAnchors(convert)) {
                            subdivision = bezierSubdivision;
                            var length = bezierSubdivision.SubdividedApproximationLength();

                            if (totalLength + length > newLength) {
                                break;
                            }

                            totalLength += length;
                            newAnchors.AddRange(bezierSubdivision.Points);
                        }

                        if (subdivision == null) break;

                        // Find T for the remaining pixel length
                        var t = subdivision.LengthToT(newLength - totalLength);

                        // ScaleRight the BezierSubdivision so the anchors end at T
                        subdivision.ScaleRight(t);

                        // Add the scaled anchors
                        newAnchors.AddRange(subdivision.Points);
                        break;
                    case PathType.PerfectCurve:
                        newPathType = PathType.PerfectCurve;
                        newAnchors.AddRange(anchors);
                        newAnchors[1] = sliderPath.PositionAt(0.5);
                        newAnchors[2] = sliderPath.PositionAt(1);
                        break;
                    default:
                        newPathType = pathType;
                        if (anchors.Count > 2) {
                            // Find the section of the linear slider which contains the slider end
                            totalLength = 0;
                            foreach (var bezierSubdivision in ChopAnchorsLinear(anchors)) {
                                newAnchors.Add(bezierSubdivision.Points[0]);
                                var length = bezierSubdivision.Length();

                                if (totalLength + length > newLength) {
                                    break;
                                }

                                totalLength += length;
                            }
                            newAnchors.Add(sliderPath.PositionAt(1));
                        } else {
                            newAnchors.AddRange(anchors);
                            newAnchors[newAnchors.Count - 1] = sliderPath.PositionAt(1);
                        }
                        break;
                }
            }

            return newAnchors;
        }

        /// <summary>
        /// Calculates the completion values of all the red anchors along the path.
        /// </summary>
        /// <param name="sliderPath"></param>
        /// <returns></returns>
        public static IEnumerable<double> GetRedAnchorCompletions(SliderPath sliderPath) {
            int start = 0;
            int end = 0;
            double totalLength = 0;
            var anchors = sliderPath.ControlPoints;

            for (int i = 0; i < anchors.Count; i++) {
                end++;

                if (i == anchors.Count - 1 || anchors[i] != anchors[i + 1]) continue;

                var cpSpan = anchors.GetRange(start, end - start);
                var subdivision = new BezierSubdivision(cpSpan);
                totalLength += subdivision.SubdividedApproximationLength();

                yield return totalLength / sliderPath.Distance;
                
                start = end;
            }
        }

        public static IEnumerable<BezierSubdivision> ChopAnchors(SliderPath sliderPath) {
            switch (sliderPath.Type) {
                case PathType.Catmull:
                case PathType.Linear:
                    return ChopAnchorsLinear(sliderPath.ControlPoints);
                default:
                    return ChopAnchors(sliderPath.ControlPoints);
            }
        }

        public static IEnumerable<BezierSubdivision> ChopAnchors(List<Vector2> anchors) {
            int start = 0;
            int end = 0;

            for (int i = 0; i < anchors.Count; i++) {
                end++;

                if (i != anchors.Count - 1 && anchors[i] != anchors[i + 1] || i == anchors.Count - 2) continue;

                var cpSpan = anchors.GetRange(start, end - start);
                var subdivision = new BezierSubdivision(cpSpan);

                yield return subdivision;
                
                start = end;
            }
        }

        public static IEnumerable<BezierSubdivision> ChopAnchorsLinear(List<Vector2> anchors) {
            for (int i = 1; i < anchors.Count; i++) {
                var subdivision = new BezierSubdivision(new List<Vector2> {anchors[i - 1], anchors[i]});
                yield return subdivision;
            }
        }
        
        public static double CalculateLoss(IReadOnlyCollection<Vector2> points, IReadOnlyList<Vector2> labels) {
            int n = points.Count;
            double totalLoss = 0;

            foreach (var point in points) {
                var minLoss = Double.PositiveInfinity;

                for (int i = 0; i < labels.Count - 1; i++) {
                    var p1 = labels[i];
                    var p2 = labels[i + 1];

                    var loss = MinimumDistance(p1, p2, point);

                    if (loss < minLoss) {
                        minLoss = loss;
                    }
                }

                totalLoss += minLoss;
            }

            return totalLoss / n;
        }

        private static double MinimumDistance(Vector2 v, Vector2 w, Vector2 p) {
            // Return minimum distance between line segment vw and point p
            double l2 = Vector2.DistanceSquared(v, w);  // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0) return Vector2.Distance(p, v);   // v == w case
            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line. 
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            // We clamp t from [0,1] to handle points outside the segment vw.
            double t = Math.Max(0, Math.Min(1, Vector2.Dot(p - v, w - v) / l2));
            Vector2 projection = v + t * (w - v);  // Projection falls on the segment
            return Vector2.Distance(p, projection);
        }
    }
}