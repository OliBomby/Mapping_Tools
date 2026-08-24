using System.ComponentModel;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Interpolation.Interpolators;

/// <summary>Interpolates a parameterized parabola.</summary>
[DisplayName("Parabola")]
[VerticalMirrorInterpolator]
public sealed class ParabolaInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    /// <summary>Creates a parabola interpolator.</summary>
    public ParabolaInterpolator()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        return -2 * MathHelper.Clamp(P, -1, 1) * t + MathHelper.Clamp(P, -1, 1) + 1;
    }

    /// <inheritdoc />
    public double GetIntegral(double t1, double t2)
    {
        return Primitive(t2) - Primitive(t1);
    }

    /// <summary>Evaluates the parameterized parabola.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The interpolated value.</returns>
    public double Function(double t)
    {
        double p = MathHelper.Clamp(P, -1, 1);
        return -p * Math.Pow(t, 2) + (p + 1) * t;
    }

    private double Primitive(double t)
    {
        double p = MathHelper.Clamp(P, -1, 1);
        return -p * Math.Pow(t, 3) / 3 + (p + 1) * Math.Pow(t, 2) / 2;
    }
}

