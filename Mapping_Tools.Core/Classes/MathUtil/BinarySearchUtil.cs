using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.MathUtil {
    /// <summary>
    /// Provides boundary searches for monotonic predicates and sorted projections.
    /// </summary>
    public class BinarySearchUtil {
        /// <summary>
        /// Finds a value X between the lower and upper bounds such that the check function returns true for X, but not for X + epsilon
        /// </summary>
        /// <typeparam name="T">The search-space value type.</typeparam>
        /// <param name="lower">The lower bound</param>
        /// <param name="upper">The upper bound</param>
        /// <param name="epsilon">The error margin</param>
        /// <param name="distanceFunc">Function which calculates the distance between two instances of <see cref="T"/></param>
        /// <param name="midFunc">Function which calculates the <see cref="T"/> in the middle of two instances of <see cref="T"/></param>
        /// <param name="checkFunc">Function which checks the validity of an instance of <see cref="T"/></param>
        /// <returns>The last known value for which <paramref name="checkFunc"/> is true, within <paramref name="epsilon"/> of the boundary.</returns>
        public static T ContinuousBinarySearch<T>(T lower, T upper, double epsilon, Func<T, T, double> distanceFunc, Func<T, T, T> midFunc, Func<T,bool> checkFunc) {
            if (!checkFunc(lower) && checkFunc(upper))
                // Bounds seem to be reversed
                (lower, upper) = (upper, lower);

            if (!checkFunc(lower))
                return lower;

            if (checkFunc(upper))
                return upper;

            while (distanceFunc(lower, upper) > epsilon) {
                var mid = midFunc(lower, upper);

                if (checkFunc(mid)) {
                    lower = mid;
                } else {
                    upper = mid;
                }
            }

            return lower;
        }

        /// <summary>
        /// Finds the true-to-false boundary of a monotonic predicate over scalar values.
        /// </summary>
        /// <param name="lower">One end of the search interval.</param>
        /// <param name="upper">The other end of the search interval.</param>
        /// <param name="epsilon">The maximum remaining interval width.</param>
        /// <param name="checkFunc">A monotonic predicate that is true on the returned side of the boundary.</param>
        /// <returns>The last accepted scalar value within the requested precision.</returns>
        public static double DoubleBinarySearch(double lower, double upper, double epsilon, Func<double, bool> checkFunc) {
            return ContinuousBinarySearch(lower, upper, epsilon,
                (d1, d2) => Math.Abs(d2 - d1),
                (d1, d2) => (d1 + d2) / 2,
                checkFunc);
        }

        /// <summary>
        /// Finds the true-to-false boundary along the straight segment between two points.
        /// </summary>
        /// <param name="lower">One endpoint of the segment.</param>
        /// <param name="upper">The other endpoint of the segment.</param>
        /// <param name="epsilon">The maximum Euclidean distance between the final bounds.</param>
        /// <param name="checkFunc">A monotonic predicate along the segment.</param>
        /// <returns>The last accepted point within the requested distance.</returns>
        public static Vector2 Vector2BinarySearch(Vector2 lower, Vector2 upper, double epsilon, Func<Vector2, bool> checkFunc) {
            return ContinuousBinarySearch(lower, upper, Math.Pow(epsilon, 2),
                Vector2.DistanceSquared,
                (v1, v2) => Vector2.Lerp(v1, v2, 0.5),
                checkFunc);
        }

        /// <summary>
        /// Finds the index of the item in the sorted collection which has its property equal to the search term.
        /// If it cant find something equal it'll return the complement of the index of the first item greater than the search term.
        /// </summary>
        /// <typeparam name="T">The collection element type.</typeparam>
        /// <typeparam name="T2">The comparable projected-key type.</typeparam>
        /// <param name="items">Elements sorted in ascending order by <paramref name="termFunc"/>.</param>
        /// <param name="searchTerm">The key to locate.</param>
        /// <param name="termFunc">Projects an element to its sorted key.</param>
        /// <param name="equalitySelection">Selects which duplicate match is returned.</param>
        /// <returns>A matching index, or the bitwise complement of the first greater element's insertion index.</returns>
        public static int BinarySearch<T, T2>(IReadOnlyList<T> items, T2 searchTerm, Func<T, T2> termFunc, EqualitySelection equalitySelection = EqualitySelection.FirstFound) {
            var n = items.Count;
            var min = 0;
            var max = n - 1;
            var comparer = Comparer<T2>.Default;
            bool equalityFound = false;
            while (min <= max) {
                var mid = min + (max - min) / 2;
                T2 midTerm = termFunc(items[mid]);
                
                switch (comparer.Compare(midTerm, searchTerm)) {
                    case 0:
                        equalityFound = true;
                        switch (equalitySelection) {
                            case EqualitySelection.Leftmost:
                                max = mid - 1;
                                break;
                            case EqualitySelection.Rightmost:
                                min = mid + 1;
                                break;
                            default:
                            case EqualitySelection.FirstFound:
                                return mid;
                        }
                        break;
                    case 1:
                        max = mid - 1;
                        break;
                    case -1:
                        min = mid + 1;
                        break;
                }
            }

            if (equalityFound) {
                switch (equalitySelection) {
                    case EqualitySelection.Leftmost:
                        return min;
                    case EqualitySelection.Rightmost:
                        return min - 1;
                }
            }

            return ~min;
        }

        /// <summary>
        /// Chooses how a binary search resolves duplicate keys.
        /// </summary>
        public enum EqualitySelection {
            /// <summary>
            /// Return whichever equal element the binary-search traversal encounters first.
            /// </summary>
            FirstFound,
            /// <summary>
            /// Return the first element in the equal-key run.
            /// </summary>
            Leftmost,
            /// <summary>
            /// Return the last element in the equal-key run.
            /// </summary>
            Rightmost
        }
    }
}
