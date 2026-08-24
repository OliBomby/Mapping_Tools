using System.ComponentModel;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Interpolation.Interpolators;

/// <summary>Provides a base-two version of the opposing double curve.</summary>
[DisplayName("Double curve 2")]
[VerticalMirrorInterpolator]
[CustomDerivativeExtrema(new[] { 0d, 0.5d, 1d })]
public sealed class DoubleCurveInterpolator2 : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a second double-curve interpolator.</summary>
    public DoubleCurveInterpolator2()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 1;
        double p = -MathHelper.Clamp(P, -1, 1) * 10;
        return t < 0.5 ? Derivative(2 * t, p) : Derivative(2 - 2 * t, p);
    }

    /// <inheritdoc />
    public double GetIntegral(double t1, double t2)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 0.5 * (t2 * t2 - t1 * t1);
        double p = -MathHelper.Clamp(P, -1, 1) * 10;
        return Primitive(t2, p) - Primitive(t1, p);
    }

    /// <summary>Evaluates the curve.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The normalized value.</returns>
    public double Function(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return linearDegenerate.GetInterpolation(t);
        double p = -MathHelper.Clamp(P, -1, 1) * 10;
        return t < 0.5 ? 0.5 * F(t * 2, p) : 0.5 + 0.5 * F(t * 2 - 1, -p);
    }

    private static double F(double t, double p)
    {
        return (Math.Pow(2, p * t) - 1) / (Math.Pow(2, p) - 1);
    }

    private static double Derivative(double t, double p)
    {
        return p * Math.Log(2) * Math.Pow(2, p * t) / (Math.Pow(2, p) - 1);
    }

    private static double Primitive(double t, double p)
    {
        return t < 0.5
            ? (Math.Pow(4, p * t) / (p * Math.Log(4)) - t) / (2 * (Math.Pow(2, p) - 1))
            : ((Math.Pow(2, p + 2) - 2) * t + Math.Pow(2, p) * (Math.Pow(2, p - 2 * p * t) - p * Math.Log(4)) / (p * Math.Log(2))) / (4 * (Math.Pow(2, p) - 1));
    }
}

