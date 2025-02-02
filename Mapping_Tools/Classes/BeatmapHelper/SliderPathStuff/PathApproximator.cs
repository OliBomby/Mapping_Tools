﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff {
    /// <summary>
    /// Helper methods to approximate a path by interpolating a sequence of control points.
    /// </summary>
    public static class PathApproximator
    {
        internal const float BezierTolerance = 0.25f;

        /// <summary>
        /// The amount of pieces to calculate for each control point quadruplet.
        /// </summary>
        private const int CatmullDetail = 50;

        private const float CircularArcTolerance = 0.1f;

        /// <summary>
        /// Creates a piecewise-linear approximation of a bezier curve, by adaptively repeatedly subdividing
        /// the control points until their approximation error vanishes below a given threshold.
        /// </summary>
        /// <param name="controlPoints">The control points.</param>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> BezierToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            return BSplineToPiecewiseLinear(controlPoints, Math.Max(1, controlPoints.Length - 1));
        }

        /// <summary>
        /// Converts a B-spline with polynomial order <paramref name="degree"/> to a series of Bezier control points
        /// via Boehm's algorithm.
        /// </summary>
        /// <remarks>
        /// Does nothing if <paramref name="controlPoints"/> has zero points or one point.
        /// Algorithm unsuitable for large values of <paramref name="degree"/> with many knots.
        /// </remarks>
        /// <param name="controlPoints">The control points.</param>
        /// <param name="degree">The polynomial order.</param>
        /// <returns>An array of vectors containing control point positions for the resulting Bezier curve.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="degree"/> was less than 1.</exception>
        public static Vector2[] BSplineToBezier(ReadOnlySpan<Vector2> controlPoints, int degree)
        {
            // Zero-th degree splines would be piecewise-constant, which cannot be represented by the piecewise-
            // linear output of this function. Negative degrees would require rational splines which this code
            // does not support.
            if (degree < 1)
                throw new ArgumentOutOfRangeException(nameof(degree), @"Degree must be greater than 0.");

            // Spline fitting does not make sense when the input contains no points or just one point. In this case
            // the user likely wants this function to behave like a no-op.
            if (controlPoints.Length < 2)
                return controlPoints.Length == 0 ? Array.Empty<Vector2>() : new[] { controlPoints[0] };

            return BSplineToBezierInternal(controlPoints, ref degree).SelectMany(segment => segment).ToArray();
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a clamped uniform B-spline with polynomial order <paramref name="degree"/>,
        /// by dividing it into a series of bezier control points at its knots, then adaptively repeatedly
        /// subdividing those until their approximation error vanishes below a given threshold.
        /// </summary>
        /// <remarks>
        /// Does nothing if <paramref name="controlPoints"/> has zero points or one point.
        /// Generalises to bezier approximation functionality when <paramref name="degree"/> is too large to create knots.
        /// Algorithm unsuitable for large values of <paramref name="degree"/> with many knots.
        /// </remarks>
        /// <param name="controlPoints">The control points.</param>
        /// <param name="degree">The polynomial order.</param>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="degree"/> was less than 1.</exception>
        public static List<Vector2> BSplineToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints, int degree)
        {
            // Zero-th degree splines would be piecewise-constant, which cannot be represented by the piecewise-
            // linear output of this function. Negative degrees would require rational splines which this code
            // does not support.
            if (degree < 1)
                throw new ArgumentOutOfRangeException(nameof(degree), @"Degree must be greater than 0.");

            // Spline fitting does not make sense when the input contains no points or just one point. In this case
            // the user likely wants this function to behave like a no-op.
            if (controlPoints.Length < 2)
                return controlPoints.Length == 0 ? new List<Vector2>() : new List<Vector2> { controlPoints[0] };

            // With fewer control points than the degree, splines can not be unambiguously fitted. Rather than erroring
            // out, we set the degree to the minimal number that permits a unique fit to avoid special casing in
            // incremental spline building algorithms that call this function.
            degree = Math.Min(degree, controlPoints.Length - 1);

            List<Vector2> output = new List<Vector2>();
            int pointCount = controlPoints.Length - 1;

            Stack<Vector2[]> toFlatten = BSplineToBezierInternal(controlPoints, ref degree);
            Stack<Vector2[]> freeBuffers = new Stack<Vector2[]>();

            // "toFlatten" contains all the curves which are not yet approximated well enough.
            // We use a stack to emulate recursion without the risk of running into a stack overflow.
            // (More specifically, we iteratively and adaptively refine our curve with a
            // <a href="https://en.wikipedia.org/wiki/Depth-first_search">Depth-first search</a>
            // over the tree resulting from the subdivisions we make.)

            var subdivisionBuffer1 = new Vector2[degree + 1];
            var subdivisionBuffer2 = new Vector2[degree * 2 + 1];

            Vector2[] leftChild = subdivisionBuffer2;

            while (toFlatten.Count > 0)
            {
                Vector2[] parent = toFlatten.Pop();

                if (BezierIsFlatEnough(parent))
                {
                    // If the control points we currently operate on are sufficiently "flat", we use
                    // an extension to De Casteljau's algorithm to obtain a piecewise-linear approximation
                    // of the bezier curve represented by our control points, consisting of the same amount
                    // of points as there are control points.
                    BezierApproximate(parent, output, subdivisionBuffer1, subdivisionBuffer2, degree + 1);

                    freeBuffers.Push(parent);
                    continue;
                }

                // If we do not yet have a sufficiently "flat" (in other words, detailed) approximation we keep
                // subdividing the curve we are currently operating on.
                Vector2[] rightChild = freeBuffers.Count > 0 ? freeBuffers.Pop() : new Vector2[degree + 1];
                BezierSubdivide(parent, leftChild, rightChild, subdivisionBuffer1, degree + 1);

                // We re-use the buffer of the parent for one of the children, so that we save one allocation per iteration.
                for (int i = 0; i < degree + 1; ++i)
                    parent[i] = leftChild[i];

                toFlatten.Push(rightChild);
                toFlatten.Push(parent);
            }

            output.Add(controlPoints[pointCount]);
            return output;
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a Catmull-Rom spline.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> CatmullToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            var result = new List<Vector2>((controlPoints.Length - 1) * CatmullDetail * 2);

            for (int i = 0; i < controlPoints.Length - 1; i++)
            {
                var v1 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
                var v2 = controlPoints[i];
                var v3 = i < controlPoints.Length - 1 ? controlPoints[i + 1] : v2 + v2 - v1;
                var v4 = i < controlPoints.Length - 2 ? controlPoints[i + 2] : v3 + v3 - v2;

                for (int c = 0; c < CatmullDetail; c++)
                {
                    result.Add(CatmullFindPoint(ref v1, ref v2, ref v3, ref v4, (float)c / CatmullDetail));
                    result.Add(CatmullFindPoint(ref v1, ref v2, ref v3, ref v4, (float)(c + 1) / CatmullDetail));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a circular arc curve.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> CircularArcToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            CircularArcProperties pr = new CircularArcProperties(controlPoints);
            if (!pr.IsValid)
                return BezierToPiecewiseLinear(controlPoints);

            // We select the amount of points for the approximation by requiring the discrete curvature
            // to be smaller than the provided tolerance. The exact angle required to meet the tolerance
            // is: 2 * Math.Acos(1 - TOLERANCE / r)
            // The special case is required for extremely short sliders where the radius is smaller than
            // the tolerance. This is a pathological rather than a realistic case.
            int amountPoints = 2 * pr.Radius <= CircularArcTolerance ? 2 : Math.Max(2, (int)Math.Ceiling(pr.ThetaRange / (2 * Math.Acos(1 - CircularArcTolerance / pr.Radius))));

            List<Vector2> output = new List<Vector2>(amountPoints);

            for (int i = 0; i < amountPoints; ++i)
            {
                double fract = (double)i / (amountPoints - 1);
                double theta = pr.ThetaStart + pr.Direction * fract * pr.ThetaRange;
                Vector2 o = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * pr.Radius;
                output.Add(pr.Centre + o);
            }

            return output;
        }

        /// <summary>
        /// Computes the bounding box of a circular arc.
        /// </summary>
        /// <param name="controlPoints">Three distinct points on the arc.</param>
        /// <returns>The rectangle inscribing the circular arc.</returns>
        public static RectangleF CircularArcBoundingBox(ReadOnlySpan<Vector2> controlPoints)
        {
            CircularArcProperties pr = new CircularArcProperties(controlPoints);
            if (!pr.IsValid)
                return RectangleF.Empty;

            // We find the bounding box using the end-points, as well as
            // each 90 degree angle inside the range of the arc
            List<Vector2> points = new List<Vector2>
            {
                controlPoints[0],
                controlPoints[2]
            };

            const double rightAngle = Math.PI / 2;
            double step = rightAngle * pr.Direction;

            double quotient = pr.ThetaStart / rightAngle;
            // choose an initial right angle, closest to ThetaStart, going in the direction of the arc.
            // thanks to this, when looping over quadrant points to check if they lie on the arc, we only need to check against ThetaEnd.
            double closestRightAngle = rightAngle * (pr.Direction > 0 ? Math.Ceiling(quotient) : Math.Floor(quotient));

            // at most, four quadrant points must be considered.
            for (int i = 0; i < 4; ++i)
            {
                double angle = closestRightAngle + step * i;

                // check whether angle has exceeded ThetaEnd.
                // multiplying by Direction eliminates branching caused by the fact that step can be either positive or negative.
                if (Precision.DefinitelyBigger((angle - pr.ThetaEnd) * pr.Direction, 0))
                    break;

                Vector2 o = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * pr.Radius;
                points.Add(pr.Centre + o);
            }

            float minX = (float) points.Min(p => p.X);
            float minY = (float) points.Min(p => p.Y);
            float maxX = (float) points.Max(p => p.X);
            float maxY = (float) points.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a linear curve.
        /// Basically, returns the input.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> LinearToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            var result = new List<Vector2>(controlPoints.Length);

            foreach (var c in controlPoints)
                result.Add(c);

            return result;
        }

        private static Stack<Vector2[]> BSplineToBezierInternal(ReadOnlySpan<Vector2> controlPoints, ref int degree)
        {
            Stack<Vector2[]> result = new Stack<Vector2[]>();

            // With fewer control points than the degree, splines can not be unambiguously fitted. Rather than erroring
            // out, we set the degree to the minimal number that permits a unique fit to avoid special casing in
            // incremental spline building algorithms that call this function.
            degree = Math.Min(degree, controlPoints.Length - 1);

            int pointCount = controlPoints.Length - 1;
            var points = controlPoints.ToArray();

            if (degree == pointCount)
            {
                // B-spline subdivision unnecessary, degenerate to single bezier.
                result.Push(points);
            }
            else
            {
                // Subdivide B-spline into bezier control points at knots.
                for (int i = 0; i < pointCount - degree; i++)
                {
                    var subBezier = new Vector2[degree + 1];
                    subBezier[0] = points[i];

                    // Destructively insert the knot degree-1 times via Boehm's algorithm.
                    for (int j = 0; j < degree - 1; j++)
                    {
                        subBezier[j + 1] = points[i + 1];

                        for (int k = 1; k < degree - j; k++)
                        {
                            int l = Math.Min(k, pointCount - degree - i);
                            points[i + k] = (l * points[i + k] + points[i + k + 1]) / (l + 1);
                        }
                    }

                    subBezier[degree] = points[i + 1];
                    result.Push(subBezier);
                }

                result.Push(points[(pointCount - degree)..]);
                // Reverse the stack so elements can be accessed in order.
                result = new Stack<Vector2[]>(result);
            }

            return result;
        }

        /// <summary>
        /// Make sure the 2nd order derivative (approximated using finite elements) is within tolerable bounds.
        /// NOTE: The 2nd order derivative of a 2d curve represents its curvature, so intuitively this function
        ///       checks (as the name suggests) whether our approximation is _locally_ "flat". More curvy parts
        ///       need to have a denser approximation to be more "flat".
        /// </summary>
        /// <param name="controlPoints">The control points to check for flatness.</param>
        /// <returns>Whether the control points are flat enough.</returns>
        private static bool BezierIsFlatEnough(Vector2[] controlPoints)
        {
            for (int i = 1; i < controlPoints.Length - 1; i++)
            {
                if ((controlPoints[i - 1] - 2 * controlPoints[i] + controlPoints[i + 1]).LengthSquared > BezierTolerance * BezierTolerance * 4)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Subdivides n control points representing a bezier curve into 2 sets of n control points, each
        /// describing a bezier curve equivalent to a half of the original curve. Effectively this splits
        /// the original curve into 2 curves which result in the original curve when pieced back together.
        /// </summary>
        /// <param name="controlPoints">The control points to split.</param>
        /// <param name="l">Output: The control points corresponding to the left half of the curve.</param>
        /// <param name="r">Output: The control points corresponding to the right half of the curve.</param>
        /// <param name="subdivisionBuffer">The first buffer containing the current subdivision state.</param>
        /// <param name="count">The number of control points in the original list.</param>
        private static void BezierSubdivide(Vector2[] controlPoints, Vector2[] l, Vector2[] r, Vector2[] subdivisionBuffer, int count)
        {
            Vector2[] midpoints = subdivisionBuffer;

            for (int i = 0; i < count; ++i)
                midpoints[i] = controlPoints[i];

            for (int i = 0; i < count; i++)
            {
                l[i] = midpoints[0];
                r[count - i - 1] = midpoints[count - i - 1];

                for (int j = 0; j < count - i - 1; j++)
                    midpoints[j] = (midpoints[j] + midpoints[j + 1]) / 2;
            }
        }

        /// <summary>
        /// This uses <a href="https://en.wikipedia.org/wiki/De_Casteljau%27s_algorithm">De Casteljau's algorithm</a> to obtain an optimal
        /// piecewise-linear approximation of the bezier curve with the same amount of points as there are control points.
        /// </summary>
        /// <param name="controlPoints">The control points describing the bezier curve to be approximated.</param>
        /// <param name="output">The points representing the resulting piecewise-linear approximation.</param>
        /// <param name="count">The number of control points in the original list.</param>
        /// <param name="subdivisionBuffer1">The first buffer containing the current subdivision state.</param>
        /// <param name="subdivisionBuffer2">The second buffer containing the current subdivision state.</param>
        private static void BezierApproximate(Vector2[] controlPoints, List<Vector2> output, Vector2[] subdivisionBuffer1, Vector2[] subdivisionBuffer2, int count)
        {
            Vector2[] l = subdivisionBuffer2;
            Vector2[] r = subdivisionBuffer1;

            BezierSubdivide(controlPoints, l, r, subdivisionBuffer1, count);

            for (int i = 0; i < count - 1; ++i)
                l[count + i] = r[i + 1];

            output.Add(controlPoints[0]);

            for (int i = 1; i < count - 1; ++i)
            {
                int index = 2 * i;
                Vector2 p = 0.25f * (l[index - 1] + 2 * l[index] + l[index + 1]);
                output.Add(p);
            }
        }

        /// <summary>
        /// Finds a point on the spline at the position of a parameter.
        /// </summary>
        /// <param name="vec1">The first vector.</param>
        /// <param name="vec2">The second vector.</param>
        /// <param name="vec3">The third vector.</param>
        /// <param name="vec4">The fourth vector.</param>
        /// <param name="t">The parameter at which to find the point on the spline, in the range [0, 1].</param>
        /// <returns>The point on the spline at <paramref name="t"/>.</returns>
        private static Vector2 CatmullFindPoint(ref Vector2 vec1, ref Vector2 vec2, ref Vector2 vec3, ref Vector2 vec4, float t)
        {
            float t2 = t * t;
            float t3 = t * t2;

            Vector2 result;
            result.X = 0.5f * (2f * vec2.X + (-vec1.X + vec3.X) * t + (2f * vec1.X - 5f * vec2.X + 4f * vec3.X - vec4.X) * t2 + (-vec1.X + 3f * vec2.X - 3f * vec3.X + vec4.X) * t3);
            result.Y = 0.5f * (2f * vec2.Y + (-vec1.Y + vec3.Y) * t + (2f * vec1.Y - 5f * vec2.Y + 4f * vec3.Y - vec4.Y) * t2 + (-vec1.Y + 3f * vec2.Y - 3f * vec3.Y + vec4.Y) * t3);

            return result;
        }
    }
}