namespace Mapping_Tools.Core.MathUtil;

/// <summary>
///     Performs simple finite-difference optimization over a bounded scalar interval.
/// </summary>
public class GradientDescentUtil
{
    /// <summary>
    ///     Attempts to find a local minimum of specified function.
    /// </summary>
    /// <param name="func">The objective function to minimize.</param>
    /// <param name="lower">The inclusive lower bound for the returned input.</param>
    /// <param name="upper">The inclusive upper bound for the returned input.</param>
    /// <param name="rate">The multiplier applied to each estimated gradient step.</param>
    /// <param name="maxSteps">The maximum number of finite-difference iterations.</param>
    /// <returns>The final input clamped to the requested interval.</returns>
    public static double GradientDescent(Func<double, double> func, double lower, double upper, double rate, int maxSteps = 1024)
    {
        const double d = 1E-6;
        double x = (lower + upper) / 2;

        for (int i = 0; i < maxSteps; i++)
        {
            double gradient = (func(x + d) - func(x)) / d;

            if (Math.Abs(gradient) < Precision.DOUBLE_EPSILON) break;

            x -= gradient * rate;
        }

        return x < lower ? lower : x > upper ? upper : x;
    }

    /// <summary>
    ///     Attempts to find a local maximum of specified function.
    /// </summary>
    /// <param name="func">The objective function to maximize.</param>
    /// <param name="lower">The inclusive lower bound for the returned input.</param>
    /// <param name="upper">The inclusive upper bound for the returned input.</param>
    /// <param name="rate">The multiplier applied to each estimated gradient step.</param>
    /// <param name="maxSteps">The maximum number of finite-difference iterations.</param>
    /// <returns>The final input clamped to the requested interval.</returns>
    public static double GradientAscent(Func<double, double> func, double lower, double upper, double rate, int maxSteps = 1024)
    {
        const double d = 1E-6;
        double x = (lower + upper) / 2;

        for (int i = 0; i < maxSteps; i++)
        {
            double gradient = (func(x + d) - func(x)) / d;

            if (Math.Abs(gradient) < Precision.DOUBLE_EPSILON) break;

            x += gradient * rate;
        }

        return x < lower ? lower : x > upper ? upper : x;
    }
}
