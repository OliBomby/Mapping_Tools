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
        /// <param name="maxSteps"></param>
        /// <returns></returns>
        public static double GradientDescent(Func<double, double> func, double lower, double upper, double rate, int maxSteps = 1024) {
            const double d = 1E-6;
            double x = (lower + upper) / 2;

            for (int i = 0; i < maxSteps; i++) {
                var gradient = (func(x + d) - func(x)) / d;

                if (Math.Abs(gradient) < Precision.DOUBLE_EPSILON) {
                    break;
                }

                x -= gradient * rate;
            }

            return x < lower ? lower : x > upper ? upper : x;
        }

        /// <summary>
        /// Attempts to find a local maximum of specified function.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <param name="rate"></param>
        /// <param name="maxSteps"></param>
        /// <returns></returns>
        public static double GradientAscent(Func<double, double> func, double lower, double upper, double rate, int maxSteps = 1024) {
            const double d = 1E-6;
            double x = (lower + upper) / 2;

            for (int i = 0; i < maxSteps; i++) {
                var gradient = (func(x + d) - func(x)) / d;

                if (Math.Abs(gradient) < Precision.DOUBLE_EPSILON) {
                    break;
                }

                x += gradient * rate;
            }

            return x < lower ? lower : x > upper ? upper : x;
        }
    }
}