using System;

namespace Mapping_Tools.Classes.MathUtil {
    public class GradientDescentUtil {
        /// <summary>
        /// Attempts to find a local minimum of specified function.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static double GradientDescent(Func<double, double> func, double lower, double upper, double rate) {
            const double d = 1E-6;
            double x = (lower + upper) / 2;

            while (true) {
                var gradient = (func(x + d) - func(x)) / d;

                if (Math.Abs(gradient) < Precision.DOUBLE_EPSILON) {
                    break;
                }

                if (x < lower) {
                    return lower;
                }

                if (x > upper) {
                    return upper;
                }

                x -= gradient * rate;
            }

            return x;
        }
        
        /// <summary>
        /// Attempts to find a local maximum of specified function.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static double GradientAscent(Func<double, double> func, double lower, double upper, double rate) {
            const double d = 1E-6;
            double x = (lower + upper) / 2;

            while (true) {
                var gradient = (func(x + d) - func(x)) / d;

                if (Math.Abs(gradient) < Precision.DOUBLE_EPSILON) {
                    break;
                }

                if (x < lower) {
                    return lower;
                }

                if (x > upper) {
                    return upper;
                }

                x += gradient * rate;
            }

            return x;
        }
    }
}