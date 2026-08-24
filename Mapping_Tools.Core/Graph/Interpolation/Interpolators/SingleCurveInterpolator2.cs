using System.ComponentModel;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Interpolation.Interpolators;

/// <summary>Provides a base-two exponential single-curve interpolation.</summary>
[DisplayName("Single curve 2")]
[VerticalMirrorInterpolator]
public sealed class SingleCurveInterpolator2 : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a second single-curve interpolator.</summary>
    public SingleCurveInterpolator2()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 1;
        double p = -MathHelper.Clamp(P, -1, 1) * 10;
        return p * Math.Log(2) * Math.Pow(2, p * t) / (Math.Pow(2, p) - 1);
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
        return (Math.Pow(2, p * t) - 1) / (Math.Pow(2, p) - 1);
    }

    private static double Primitive(double t, double p)
    {
        return (Math.Pow(2, p * t) / (p * Math.Log(2)) - t) / (Math.Pow(2, p) - 1);
    }
}

