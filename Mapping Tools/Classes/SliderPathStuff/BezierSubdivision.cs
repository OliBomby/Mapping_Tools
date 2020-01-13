using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SliderPathStuff
{
    /// <summary>
    /// Helper methods for advanced bezier anchor and path approximation manipulation.
    /// A <see cref="BezierSubdivision"/> represents a single bezier polynomial.
    /// </summary>
    // Many of these functions are designed to simulate PathApproximator methods, but
    // with added functionality which should be kept separate from PathApproximator.
    public class BezierSubdivision : ICloneable
    {
        public List<Vector2> Points; // List of bezier control Points
        public int Order => Points.Count - 1; // Bezier polynomial order
        public int Level; // Depth of subdivision
        public int Index; // Index of subdivision

        public BezierSubdivision(List<Vector2> points, int level = 0, int index = 0)
        {
            Points = points;
            Level = level;
            Index = index;
        }

        public double Flatness() // Max of the flatness metric
        {
            double worst = 0;
            for (int i = 1; i < Order; i++) {
                worst = Math.Max(worst, (Points[i - 1] - 2 * Points[i] + Points[i + 1]).LengthSquared);
            }
            return Math.Sqrt(worst) / 2;
        }

        public bool Flat(double tolerance = 0.25) // Whether it would satisfy BezierIsFlatEnough
        {
            return Flatness() <= tolerance;
        }

        public double Length() // Euclidean length of subdivision segments
        {
            double length = 0;
            for (int i = 0; i < Order; i++)
                length += (Points[i + 1] - Points[i]).Length;
            return length;
        }

        public void Reverse() // Reverse the Points
        {
            Points.Reverse();
        }

        public void ScaleRight(double t) // De Casteljau reparameterization [0,t]
        {
            for (int j = 0; j < Order; j++)
                for (int i = Order; i > j; i--)
                    Points[i] = Points[i] * t + Points[i - 1] * (1 - t);
        }

        public void ScaleLeft(double t) // De Casteljau reparameterization [t,1]
        {
            for (int j = Order; j > 0; j--)
                for (int i = 0; i < j; i++)
                    Points[i] = Points[i] * (1 - t) + Points[i + 1] * t;
        }

        public BezierSubdivision Next() // Next index at current level
        {
            var next = new BezierSubdivision(new List<Vector2>(Points), Level, Index + 1);
            next.ScaleLeft(2);
            next.Reverse();
            return next;
        }

        public BezierSubdivision Prev() // Previous index at current level
        {
            var next = new BezierSubdivision(new List<Vector2>(Points), Level, Index - 1);
            next.ScaleRight(-1);
            next.Reverse();
            return next;
        }

        public BezierSubdivision Parent() // Parent subdivision (inverse of BezierSubdivide)
        {
            var parent = new BezierSubdivision(new List<Vector2>(Points), Level - 1, Index >> 1);
            if ((Index & 1) == 0)
                parent.ScaleRight(2);
            else
                parent.ScaleLeft(-1);
            return parent;
        }

        public void Children(out BezierSubdivision leftChild, out BezierSubdivision rightChild) // Child subdivisions (BezierSubdivide)
        {
            var left = new List<Vector2>(Points);
            var right = new List<Vector2>(Points);
            for (int j = 0; j < Order; j++)
                for (int i = Order; i > j; i--) {
                    left[i] = (left[i] + left[i - 1]) / 2;
                    right[Order - i] = (right[Order - i] + right[Order - i + 1]) / 2;
                }
            leftChild = new BezierSubdivision(left, Level + 1, Index << 1);
            rightChild = new BezierSubdivision(right, Level + 1, Index << 1 | 1);
        }

        public static void Subdivide(ref LinkedList<BezierSubdivision> subdivisions, double tolerance = 0.25) // Simulate BezierApproximate on a linked list
        {
            var current = subdivisions.First;
            while (current != null) {
                if (current.Value.Flat(tolerance)) {
                    current = current.Next;
                } else {
                    current.Value.Children(out var left, out var right);
                    current.Value = left;
                    subdivisions.AddAfter(current, right);
                }
            }
        }

        /// <summary>
        /// Subdivided the <see cref="BezierSubdivision"/> and calculates the aggregate length.
        /// </summary>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public double SubdividedLength(double tolerance = 0.25) {
            var pathApproximation = new LinkedList<BezierSubdivision>();
            pathApproximation.AddLast(this);
            Subdivide(ref pathApproximation, tolerance);
            return pathApproximation.Sum(o => o.Length());
        }

        public void Increase(int k = 1) // Increase bezier order by k
        {
            for (int j = 0; j < k; j++) {
                Points.Add(Points[Order]);
                for (int i = Order - 1; i > 0; i--)
                    Points[i] = (Points[i] * (Order - i) + Points[i - 1] * i) / Order;
            }
        }

        public double LengthToT(double length, double precision = 0.1, double tolerance = 0.25) // approximate bezier progress t for a desired path length
        {
            LinkedListNode<BezierSubdivision> current = null;
            double l = 0;
            double lnext = 0;
            while (length > lnext) {
                current = current?.Next;
                if (current == null) {
                    var pathApproximation = new LinkedList<BezierSubdivision>();
                    pathApproximation.AddLast(this);
                    Subdivide(ref pathApproximation, tolerance);
                    current = pathApproximation.First;
                }
                l = lnext;
                lnext += current.Value.Length();
            }

            if (current == null) return 0;

            BezierSubdivision curr = current.Value;
            while (curr.Length() > precision) {
                curr.Children(out var left, out var right);
                lnext = l + left.Length();
                if (length > lnext) {
                    curr = right;
                    l = lnext;
                } else {
                    curr = left;
                }
            }

            return (curr.Index + (length - l) / curr.Length()) / (1 << curr.Level);
        }

        public object Clone() {
            return new BezierSubdivision(new List<Vector2>(Points), Level, Index);
        }
    }
}
