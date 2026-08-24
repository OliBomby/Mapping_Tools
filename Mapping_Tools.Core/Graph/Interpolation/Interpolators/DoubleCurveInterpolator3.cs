using System.ComponentModel;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Interpolation.Interpolators;

/// <summary>Provides a rational version of the opposing double curve.</summary>
[DisplayName("Double curve 3")]
[VerticalMirrorInterpolator]
[CustomDerivativeExtrema(new[] { 0d, 0.5d, 1d })]
public sealed class DoubleCurveInterpolator3 : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a third double-curve interpolator.</summary>
    public DoubleCurveInterpolator3()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 1;
        double p = MathHelper.Clamp(P, -1, 1) * 7;
        return t < 0.5 ? Derivative(2 * t, p) : Derivative(2 - 2 * t, p);
    }

    /// <inheritdoc />
    public double GetIntegral(double t1, double t2)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 0.5 * (t2 * t2 - t1 * t1);
        double p = MathHelper.Clamp(P, -1, 1) * 7;
        return Primitive(t2, p) - Primitive(t1, p);
    }

    /// <summary>Evaluates the curve.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The normalized value.</returns>
    public double Function(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return linearDegenerate.GetInterpolation(t);
        double p = MathHelper.Clamp(P, -1, 1) * 7;
        return t < 0.5 ? 0.5 * F(t * 2, p) : 0.5 + 0.5 * F(t * 2 - 1, -p);
    }

    private static double F(double t, double p)
    {
        return Math.Exp(p) * t / ((Math.Exp(p) - 1) * t + 1);
    }

    private static double Derivative(double t, double p)
    {
        return Math.Exp(p) / Math.Pow(t * (Math.Exp(p) - 1) + 1, 2);
    }

    private static double Primitive(double t, double p)
    {
        return t < 0.5
            ? -(Math.Exp(p) * (Math.Log(2 * t * (Math.Exp(p) - 1) + 1) - 2 * t * (Math.Exp(p) - 1))) / (4 * Math.Pow(Math.Exp(p) - 1, 2))
            : (2 * t * (Math.Exp(p) - 2) * (Math.Exp(p) - 1)
               - Math.Exp(p) * (Math.Log(-Math.Exp(-p) * (2 * t * Math.Exp(p) - 2 * Math.Exp(p) - 2 * t + 1)) - Math.Exp(p) - 2)
               - Math.Exp(2 * p)
               - Math.Exp(p) * Math.Log(Math.Exp(p))
               - 2)
              / (4 * Math.Pow(Math.Exp(p) - 1, 2));
    }
}
