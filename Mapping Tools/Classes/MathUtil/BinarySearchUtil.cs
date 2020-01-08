using System;

namespace Mapping_Tools.Classes.MathUtil {
    public class BinarySearchUtil {
        /// <summary>
        /// Finds a value X between the lower and upper bounds such that the check function returns true for X, but not for X + delta
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lower">The lower bound</param>
        /// <param name="upper">The upper bound</param>
        /// <param name="delta">The error margin</param>
        /// <param name="distanceFunc">Function which calculates the distance between two instances of <see cref="T"/></param>
        /// <param name="midFunc">Function which calculates the <see cref="T"/> in the middle of two instances of <see cref="T"/></param>
        /// <param name="checkFunc">Function which checks the validity of an instance of <see cref="T"/></param>
        /// <returns></returns>
        public static T BinarySearch<T>(T lower, T upper, double delta, Func<T, T, double> distanceFunc, Func<T, T, T> midFunc, Func<T,bool> checkFunc) {
            while (distanceFunc(lower, upper) > delta) {
                var mid = midFunc(lower, upper);

                if (checkFunc(mid)) {
                    lower = mid;
                } else {
                    upper = mid;
                }
            }

            return lower;
        }

        public static double DoubleBinarySearch(double lower, double upper, double delta, Func<double, bool> checkFunc) {
            return BinarySearch(lower, upper, delta,
                (d1, d2) => Math.Abs(d2 - d1),
                (d1, d2) => (d1 + d2) / 2,
                checkFunc);
        }

        public static Vector2 Vector2BinarySearch(Vector2 lower, Vector2 upper, double delta, Func<Vector2, bool> checkFunc) {
            return BinarySearch(lower, upper, Math.Pow(delta, 2),
                Vector2.DistanceSquared,
                (v1, v2) => Vector2.Lerp(v1, v2, 0.5),
                checkFunc);
        }
    }
}