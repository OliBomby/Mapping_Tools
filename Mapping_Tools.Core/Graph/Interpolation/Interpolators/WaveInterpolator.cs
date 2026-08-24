using System.ComponentModel;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Interpolation.Interpolators;

/// <summary>Interpolates an oscillating sine or triangle wave.</summary>
[DisplayName("Wave")]
[CustomDerivativeExtrema(new[] { 0d, 0.5d, 1d })]
public sealed class WaveInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator, IInvertibleInterpolator
{
    /// <summary>Creates a wave interpolator.</summary>
    public WaveInterpolator()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        double cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;
        return P < 0
            ? TriangleWaveDerivative(t, 1 / cycles)
            : SineWaveDerivative(t * cycles * 2 * Math.PI) * cycles * 2 * Math.PI;
    }

    /// <inheritdoc />
    public double GetIntegral(double t1, double t2)
    {
        double cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;
        return P < 0
            ? TriangleWaveIntegral(t2, 1 / cycles) - TriangleWaveIntegral(t1, 1 / cycles)
            : SineWavePrimitive(t2, cycles) - SineWavePrimitive(t1, cycles);
    }

    /// <inheritdoc />
    public IEnumerable<double> GetInverse(double y)
    {
        double cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;
        return P < 0 ? TriangleWaveInverse(y, 1 / cycles) : SineWaveInverse(y, 1 / cycles);
    }

    /// <summary>Evaluates the wave selected by the sign of <see cref="IGraphInterpolator.P" />.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The wave value.</returns>
    public double Function(double t)
    {
        double cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;
        return P < 0 ? TriangleWave(t, 1 / cycles) : SineWave(t * cycles * 2 * Math.PI);
    }

    private static double SineWave(double t)
    {
        return (-Math.Cos(t) + 1) / 2;
    }

    private static double SineWaveDerivative(double t)
    {
        return Math.Sin(t) / 2;
    }

    private static double SineWavePrimitive(double t, double c)
    {
        return t / 2 - Math.Sin(2 * Math.PI * c * t) / (4 * Math.PI * c);
    }

    private static double TriangleWave(double t, double period)
    {
        double mod = t % period;
        return mod < period / 2 ? 2 * mod / period : 2 - 2 * mod / period;
    }

    private static double TriangleWaveDerivative(double t, double period)
    {
        return t % period < period / 2 ? 2 / period : -2 / period;
    }

    private static double TriangleWaveIntegral(double t, double period)
    {
        double mod = t % period;
        double cycles = Math.Floor(t / period);
        double integral = mod < period / 2
            ? Math.Pow(mod, 2) / period
            : 2 * mod - Math.Pow(mod, 2) / period - period / 2;
        return cycles * period * 0.5 + integral;
    }

    private static IEnumerable<double> SineWaveInverse(double y, double period)
    {
        double x1 = period * Math.Acos(1 - 2 * y) / (2 * Math.PI);
        double x2 = period * Math.Acos(2 * y - 1) / (2 * Math.PI) + period / 2;
        yield return x1;
        yield return x2;
        for (int i = 0; i < 1000; i++)
        {
            x1 += period;
            if (x1 > 1) yield break;
            yield return x1;
            x2 += period;
            if (x2 > 1) yield break;
            yield return x2;
        }
    }

    private static IEnumerable<double> TriangleWaveInverse(double y, double period)
    {
        double x1 = period * y / 2;
        double x2 = period * (2 - y) / 2;
        yield return x1;
        yield return x2;
        for (int i = 0; i < 1000; i++)
        {
            x1 += period;
            if (x1 > 1) yield break;
            yield return x1;
            x2 += period;
            if (x2 > 1) yield break;
            yield return x2;
        }
    }
}

