﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff {
    public struct SliderPath :IEquatable<SliderPath> {
        /// <summary>
        /// The user-set distance of the path. If non-null, <see cref="Distance"/> will match this value,
        /// and the path will be shortened/lengthened to match this length.
        /// </summary>
        public readonly double? ExpectedDistance;

        /// <summary>
        /// The type of path.
        /// </summary>
        public readonly PathType Type;

        private Vector2[] controlPoints;

        private List<Vector2> calculatedPath;
        private List<double> cumulativeLength;
        private List<int> segmentStarts;

        private bool isInitialised;

        /// <summary>
        /// Creates a new <see cref="SliderPath"/>.
        /// </summary>
        /// <param name="type">The type of path.</param>
        /// <param name="controlPoints">The control points of the path.</param>
        /// <param name="expectedDistance">A user-set distance of the path that may be shorter or longer than the true distance between all
        /// <paramref name="controlPoints"/>. The path will be shortened/lengthened to match this length.
        /// If null, the path will use the true distance between all <paramref name="controlPoints"/>.</param>
        public SliderPath(PathType type, Vector2[] controlPoints, double? expectedDistance = null) {
            this = default;
            this.controlPoints = controlPoints;

            Type = type;
            ExpectedDistance = expectedDistance;

            EnsureInitialised();
        }

        /// <summary>
        /// The control points of the path.
        /// </summary>
        public List<Vector2> ControlPoints {
            get {
                EnsureInitialised();
                return controlPoints.ToList();
            }
        }

        /// <summary>
        /// The distance of the path after lengthening/shortening to account for <see cref="ExpectedDistance"/>.
        /// </summary>
        public double Distance {
            get {
                EnsureInitialised();
                return cumulativeLength.Count == 0 ? 0 : cumulativeLength[cumulativeLength.Count - 1];
            }
        }

        public IReadOnlyList<Vector2> CalculatedPath {
            get {
                EnsureInitialised();
                return calculatedPath;
            }
        }

        public IReadOnlyList<double> CumulativeLength {
            get {
                EnsureInitialised();
                return cumulativeLength;
            }
        }

        public IReadOnlyList<int> SegmentStarts {
            get {
                EnsureInitialised();
                return segmentStarts;
            }
        }

        /// <summary>
        /// Computes the slider path until a given progress that ranges from 0 (beginning of the slider)
        /// to 1 (end of the slider) and stores the generated path in the given list.
        /// </summary>
        /// <param name="path">The list to be filled with the computed path.</param>
        /// <param name="p0">Start progress. Ranges from 0 (beginning of the slider) to 1 (end of the slider).</param>
        /// <param name="p1">End progress. Ranges from 0 (beginning of the slider) to 1 (end of the slider).</param>
        public void GetPathToProgress(List<Vector2> path, double p0, double p1) {
            EnsureInitialised();

            double d0 = ProgressToDistance(p0);
            double d1 = ProgressToDistance(p1);

            path.Clear();

            int i = 0;
            for( ; i < calculatedPath.Count && cumulativeLength[i] < d0; ++i ) {
            }

            path.Add(InterpolateVertices(i, d0));

            for( ; i < calculatedPath.Count && cumulativeLength[i] <= d1; ++i )
                path.Add(calculatedPath[i]);

            path.Add(InterpolateVertices(i, d1));
        }

        /// <summary>
        /// Computes the position on the slider at a given progress that ranges from 0 (beginning of the path)
        /// to 1 (end of the path).
        /// </summary>
        /// <param name="progress">Ranges from 0 (beginning of the path) to 1 (end of the path).</param>
        /// <returns></returns>
        public Vector2 PositionAt(double progress) {
            EnsureInitialised();

            double d = ProgressToDistance(progress);
            return InterpolateVertices(IndexOfDistance(d), d);
        }

        /// <summary>
        /// Computes the position of the sliderball on the slider at a given ms
        /// that ranges from 0 (beginning of the path) to timeLength (end of the path).
        /// </summary>
        /// <param name="ms">Ranges from 0 (beginning of the path) to timeLength (end of the path).</param>
        /// <param name="timeLength"> Indicates the ms duration of the slider, using the slider velocity.</param>
        /// <returns></returns>
        public Vector2 SliderballPositionAt(int ms, int timeLength) {
            EnsureInitialised();

            int msSegmentIndex = IndexOfDistance(ProgressToDistance((double) ms / timeLength));
            if (msSegmentIndex != 0) {
                int testMsIndex;
                int minMsInSegment = 0;
                int maxMsInSegment = timeLength;
                for (int testMs = ms; testMs >= 0; testMs--) {
                    testMsIndex = IndexOfDistance(ProgressToDistance((double) testMs / timeLength));
                    if (testMsIndex != msSegmentIndex) {
                        minMsInSegment = testMs + 1;
                        break;
                    }
                }
                for (int testMs = ms; testMs <= timeLength; testMs++) {
                    testMsIndex = IndexOfDistance(ProgressToDistance((double) testMs / timeLength));
                    if (testMsIndex != msSegmentIndex) {
                        maxMsInSegment = testMs - 1;
                        break;
                    }
                }
                int totalMsInSegment = maxMsInSegment - minMsInSegment + 1;
                double msFracOfSegment = (double) (ms - minMsInSegment + 1) / totalMsInSegment;

                Vector2 p0 = calculatedPath[msSegmentIndex - 1];
                Vector2 p1 = calculatedPath[msSegmentIndex];

                return p0 + (p1 - p0) * msFracOfSegment;
            }
            else {
                return calculatedPath[0];
            }
        }

        /// <summary>
        /// Computes the position of the sliderball on the slider at all ms from 0 to timeLength.
        /// </summary>
        /// <param name="timeLength"> Indicates the ms duration of the slider, using the slider velocity.</param>
        /// <returns>
        /// A Vector2 array such that the index i contains the position of the sliderball at the ith ms.
        /// </returns>
        public Vector2[] SliderballPositions(int timeLength) {
            EnsureInitialised();

            Vector2[] sbPositions = new Vector2[timeLength + 1];
            int[] msPerSegment = new int[cumulativeLength.Count];
            for (int i = 0; i < timeLength+1; i++) {
                int idx = IndexOfDistance(ProgressToDistance((double)i / timeLength));
                if (idx < 0) idx = 0;
                if (idx >= cumulativeLength.Count) idx = cumulativeLength.Count - 1;
                msPerSegment[idx]++;
            }

            int curMs = 0;
            for (int j = 0; j < msPerSegment[0]; j++) {
                sbPositions[curMs] = calculatedPath[0];
                curMs++;
            }
            for (int i = 1; i < cumulativeLength.Count; i++) {
                Vector2 p0 = calculatedPath[i - 1];
                Vector2 p1 = calculatedPath[i];
                for (int j = 1; j < msPerSegment[i]+1; j++) {
                    sbPositions[curMs] = p0 + (p1 - p0) * j / msPerSegment[i];
                    curMs++;
                }
            }

            return sbPositions;
        }

        private void EnsureInitialised() {
            if( isInitialised )
                return;
            isInitialised = true;

            controlPoints = controlPoints ?? Array.Empty<Vector2>();
            calculatedPath = new List<Vector2>();
            cumulativeLength = new List<double>();
            segmentStarts = new List<int>();

            CalculatePath();
            CalculateCumulativeLength();
        }

        private List<Vector2> CalculateSubpath(List<Vector2> subControlPoints) {
            switch( Type ) {
                case PathType.Linear:
                    return PathApproximator.ApproximateLinear(subControlPoints);
                case PathType.PerfectCurve:
                    //we can only use CircularArc iff we have exactly three control points and no dissection.
                    if( ControlPoints.Length() != 3 || subControlPoints.Length() != 3 )
                        break;

                    // Here we have exactly 3 control points. Attempt to fit a circular arc.
                    List<Vector2> subpath = PathApproximator.ApproximateCircularArc(subControlPoints);

                    // If for some reason a circular arc could not be fit to the 3 given points, fall back to a numerically stable bezier approximation.
                    if( subpath.Count == 0 )
                        break;

                    return subpath;
                case PathType.Catmull:
                    return PathApproximator.ApproximateCatmull(subControlPoints);
            }

            return PathApproximator.ApproximateBezier(subControlPoints);
        }

        private void CalculatePath() {
            calculatedPath.Clear();

            // Sliders may consist of various subpaths separated by two consecutive vertices
            // with the same position. The following loop parses these subpaths and computes
            // their shape independently, consecutively appending them to calculatedPath.

            int start = 0;
            int end = 0;

            for( int i = 0; i < ControlPoints.Length(); ++i ) {
                end++;

                if( i == ControlPoints.Length() - 1 || ControlPoints[i] == ControlPoints[i + 1] && i != ControlPoints.Length() - 2) {
                    List<Vector2> cpSpan = ControlPoints.GetRange(start, end - start);

                    // Remember the index of the subpath start
                    segmentStarts.Add(calculatedPath.Count);

                    foreach( Vector2 t in CalculateSubpath(cpSpan) )
                        if( calculatedPath.Count == 0 || calculatedPath.Last() != t )
                            calculatedPath.Add(t);

                    start = end;
                }
            }
        }

        private void CalculateCumulativeLength() {
            double l = 0;

            cumulativeLength.Clear();
            cumulativeLength.Add(l);

            for( int i = 0; i < calculatedPath.Count - 1; ++i ) {
                Vector2 diff = calculatedPath[i + 1] - calculatedPath[i];
                double d = diff.Length;

                // Shorted slider paths that are too long compared to the expected distance
                if( ExpectedDistance.HasValue && ExpectedDistance - l < d ) {
                    calculatedPath[i + 1] = calculatedPath[i] + diff * (double) ( ( ExpectedDistance - l ) / d );
                    calculatedPath.RemoveRange(i + 2, calculatedPath.Count - 2 - i);

                    l = ExpectedDistance.Value;
                    cumulativeLength.Add(l);
                    break;
                }

                l += d;
                cumulativeLength.Add(l);
            }

            // Lengthen slider paths that are too short compared to the expected distance
            if( ExpectedDistance.HasValue && l < ExpectedDistance && calculatedPath.Count > 1 ) {
                Vector2 diff = calculatedPath[calculatedPath.Count - 1] - calculatedPath[calculatedPath.Count - 2];
                double d = diff.Length;

                if( d <= 0 )
                    return;

                calculatedPath[calculatedPath.Count - 1] += diff * (double) ( ( ExpectedDistance - l ) / d );
                cumulativeLength[calculatedPath.Count - 1] = ExpectedDistance.Value;
            }
        }

        private int IndexOfDistance(double d) {
            int i = cumulativeLength.BinarySearch(d);
            if( i < 0 )
                i = ~i;

            return i;
        }

        private double ProgressToDistance(double progress) {
            return MathHelper.Clamp(progress, 0, 1) * Distance;
        }

        private Vector2 InterpolateVertices(int i, double d) {
            if( calculatedPath.Count == 0 )
                return Vector2.Zero;

            if( i <= 0 )
                return calculatedPath.First();
            if( i >= calculatedPath.Count )
                return calculatedPath.Last();

            Vector2 p0 = calculatedPath[i - 1];
            Vector2 p1 = calculatedPath[i];

            double d0 = cumulativeLength[i - 1];
            double d1 = cumulativeLength[i];

            // Avoid division by and almost-zero number in case two points are extremely close to each other.
            if( Precision.AlmostEquals(d0, d1) )
                return p0;

            double w = ( d - d0 ) / ( d1 - d0 );
            return p0 + ( p1 - p0 ) * w;
        }

        public bool Equals(SliderPath other) {
            if( ControlPoints == null && other.ControlPoints != null )
                return false;
            if( other.ControlPoints == null && ControlPoints != null )
                return false;

            return ControlPoints.SequenceEqual(other.ControlPoints) && ExpectedDistance.Equals(other.ExpectedDistance) && Type == other.Type;
        }

        public override bool Equals(object obj) {
            if( obj is null )
                return false;
            return obj is SliderPath other && Equals(other);
        }

        public static bool operator ==(SliderPath left, SliderPath right) {
            return left.Equals(right);
        }

        public static bool operator !=(SliderPath left, SliderPath right) {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            var hashCode = -1383746172;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(ExpectedDistance);
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2[]>.Default.GetHashCode(controlPoints);
            return hashCode;
        }
    }
}