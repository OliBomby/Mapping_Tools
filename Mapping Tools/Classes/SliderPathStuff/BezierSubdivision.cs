using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SliderPathStuff
{
    /// <summary>
    /// Helper methods for advanced bezier anchor and path approximation manipulation.
    /// </summary>
    // Many of these functions are designed to simulate PathApproximator methods, but
    // with added functionality which should be kept separate from PathApproximator.
    public class BezierSubdivision
    {
        public List<Vector2> points; // List of bezier control points
        public int n => points.Count - 1; // Bezier polynomial order
        public int level; // Depth of subdivision
        public int index; // Index of subdivision

        public BezierSubdivision(List<Vector2> points, int level = 0, int index = 0)
        {
            this.points = points;
            this.level = level;
            this.index = index;
        }

        public BezierSubdivision Copy()
        {
            return new BezierSubdivision(new List<Vector2>(points), level, index);
        }

        public double Flatness() // Max of the flatness metric
        {
            double worst = 0;
            for (int i = 1; i < n; i++)
                worst = Math.Max(worst, (points[i - 1] - 2 * points[i] + points[i + 1]).LengthSquared);
            return Math.Sqrt(worst) / 2;
        }

        public bool Flat(double tolerance = 0.25) // Whether it would satisfy BezierIsFlatEnough
        {
            return Flatness() <= tolerance;
        }

        public double Length() // Euclidean length of subdivision segments
        {
            double length = 0;
            for (int i = 0; i < n; i++)
                length += (points[i + 1] - points[i]).Length;
            return length;
        }

        public void Reverse() // Reverse the points
        {
            points.Reverse();
        }

        public void ScaleRight(double t) // De Casteljau reparameterization [0,t]
        {
            for (int j = 0; j < n; j++)
                for (int i = n; i > j; i--)
                    points[i] = points[i] * t + points[i - 1] * (1 - t);
        }

        public void ScaleLeft(double t) // De Casteljau reparameterization [t,1]
        {
            for (int j = n; j > 0; j--)
                for (int i = 0; i < j; i++)
                    points[i] = points[i] * (1 - t) + points[i + 1] * t;
        }

        public BezierSubdivision Next() // Next index at current level
        {
            var next = new BezierSubdivision(new List<Vector2>(points), level, index + 1);
            next.ScaleLeft(2);
            next.Reverse();
            return next;
        }

        public BezierSubdivision Prev() // Previous index at current level
        {
            var next = new BezierSubdivision(new List<Vector2>(points), level, index - 1);
            next.ScaleRight(-1);
            next.Reverse();
            return next;
        }

        public BezierSubdivision Parent() // Parent subdivision (inverse of BezierSubdivide)
        {
            var parent = new BezierSubdivision(new List<Vector2>(points), level - 1, index >> 1);
            if ((index & 1) == 0)
                parent.ScaleRight(2);
            else
                parent.ScaleLeft(-1);
            return parent;
        }

        public void Children(out BezierSubdivision leftChild, out BezierSubdivision rightChild) // Child subdivisions (BezierSubdivide)
        {
            var left = new List<Vector2>(points);
            var right = new List<Vector2>(points);
            for (int j = 0; j < n; j++)
                for (int i = n; i > j; i--) {
                    left[i] = (left[i] + left[i - 1]) / 2;
                    right[n - i] = (right[n - i] + right[n - i + 1]) / 2;
                }
            leftChild = new BezierSubdivision(left, level + 1, index << 1);
            rightChild = new BezierSubdivision(right, level + 1, index << 1 | 1);
        }

        public static void Subdivide(ref LinkedList<BezierSubdivision> subdivisions, double tolerance = 0.25) // Simulate BezierApproximate on a linked list
        {
            BezierSubdivision left;
            BezierSubdivision right;
            var current = subdivisions.First;
            while (current != null) {
                if (current.Value.Flat(tolerance)) {
                    current = current.Next;
                } else {
                    current.Value.Children(out left, out right);
                    current.Value = left;
                    subdivisions.AddAfter(current, right);
                }
            }
        }

        public void Increase(int k = 1) // Increase bezier order by k
        {
            for (int j = 0; j < k; j++) {
                points.Add(points[n]);
                for (int i = n - 1; i > 0; i--)
                    points[i] = (points[i] * (n - i) + points[i - 1] * i) / n;
            }
        }

        public double LengthToT(double length, double precision = 0.1, double tolerance = 0.25) // approximate bezier progress t for a desired path length
        {
            BezierSubdivision baseSubdivision = null;
            LinkedList<BezierSubdivision> pathApproximation;
            LinkedListNode<BezierSubdivision> current = null;
            double l = 0;
            double lnext = 0;
            while (length > lnext) {
                if (current != null)
                    current = current.Next;
                if (current == null) {
                    if (baseSubdivision == null) {
                        baseSubdivision = this;
                    } else {
                        baseSubdivision = baseSubdivision.Next();
                    }
                    pathApproximation = new LinkedList<BezierSubdivision>();
                    pathApproximation.AddLast(baseSubdivision);
                    Subdivide(ref pathApproximation, tolerance);
                    current = pathApproximation.First;
                }
                l = lnext;
                lnext += current.Value.Length();
            }
            BezierSubdivision curr = current.Value;
            BezierSubdivision left;
            BezierSubdivision right;
            while (curr.Length() > precision) {
                curr.Children(out left, out right);
                lnext = l + left.Length();
                if (length > lnext) {
                    curr = right;
                    l = lnext;
                } else {
                    curr = left;
                }
            }
            return (curr.index + (length - l) / curr.Length()) / (1 << curr.level);
        }
    }
}
