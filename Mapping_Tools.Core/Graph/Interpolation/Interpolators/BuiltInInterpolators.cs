using System.ComponentModel;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Interpolation.Interpolators;

/// <summary>Interpolates linearly between two graph anchors.</summary>
[IgnoreInterpolator]
[DisplayName("Linear")]
public sealed class LinearInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    /// <summary>Creates a linear interpolator.</summary>
    public LinearInterpolator() : base(t => t)
    {
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        return 1;
    }

    /// <inheritdoc />
    public double GetIntegral(double t1, double t2)
    {
        return 0.5 * t2 * t2 - 0.5 * t1 * t1;
    }
}

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

/// <summary>Interpolates a parameterized half sine.</summary>
[DisplayName("Half sine")]
[VerticalMirrorInterpolator]
public sealed class HalfSineInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a half-sine interpolator.</summary>
    public HalfSineInterpolator()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return linearDegenerate.GetDerivative(t);

        double p = MathHelper.Clamp(P, -1, 1);
        double b = 2 * Math.Acos(1 / (Math.Sqrt(2) * Math.Abs(p) - Math.Abs(p) + 1));
        return p < 0 ? Derivative(1 - t, b) : Derivative(t, b);
    }

    /// <inheritdoc />
    public double GetIntegral(double t1, double t2)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return linearDegenerate.GetIntegral(t1, t2);

        double p = MathHelper.Clamp(P, -1, 1);
        return Primitive(t2, p) - Primitive(t1, p);
    }

    private double Function(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return linearDegenerate.GetInterpolation(t);

        double p = MathHelper.Clamp(P, -1, 1);
        double b = 2 * Math.Acos(1 / (Math.Sqrt(2) * Math.Abs(p) - Math.Abs(p) + 1));
        return p < 0 ? 1 - F(1 - t, b) : F(t, b);
    }

    private static double F(double t, double k)
    {
        return Math.Sin(t * k) / Math.Sin(k);
    }

    private static double Derivative(double t, double k)
    {
        return -(2 * k * Math.Sin(k) * Math.Cos(k * t)) / (Math.Cos(2 * k) - 1);
    }

    private static double Primitive(double t, double p)
    {
        double b = 2 * Math.Acos(1 / (Math.Sqrt(2) * Math.Abs(p) - Math.Abs(p) + 1));
        return p > 0
            ? -(MathHelper.Cosec(b) * (Math.Cos(b * t) - 1)) / b
            : MathHelper.Cosec(b) * (Math.Cos(b) - Math.Cos(b - b * t)) / b + t;
    }
}

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

/// <summary>Provides a normalized exponential single-curve interpolation.</summary>
[DisplayName("Single curve")]
[VerticalMirrorInterpolator]
public sealed class SingleCurveInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a single-curve interpolator.</summary>
    public SingleCurveInterpolator()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 1;
        double p = -MathHelper.Clamp(P, -1, 1) * 10;
        return p * Math.Exp(p * t) / (Math.Exp(p) - 1);
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
        return (Math.Exp(p * t) - 1) / (Math.Exp(p) - 1);
    }

    private static double Primitive(double t, double p)
    {
        return (Math.Exp(p * t) / p - t) / (Math.Exp(p) - 1);
    }
}

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

/// <summary>Provides a rational single-curve interpolation.</summary>
[DisplayName("Single curve 3")]
[VerticalMirrorInterpolator]
public sealed class SingleCurveInterpolator3 : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a third single-curve interpolator.</summary>
    public SingleCurveInterpolator3()
    {
        InterpolationFunction = Function;
    }

    /// <inheritdoc />
    public double GetDerivative(double t)
    {
        if (Math.Abs(P) < Precision.DOUBLE_EPSILON) return 1;
        double p = MathHelper.Clamp(P, -1, 1) * 7;
        return Math.Exp(p) / Math.Pow(t * (Math.Exp(p) - 1) + 1, 2);
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
        return Math.Exp(p) * t / ((Math.Exp(p) - 1) * t + 1);
    }

    private static double Primitive(double t, double p)
    {
        return (Math.Exp(p * t) / p - t) / (Math.Exp(p) - 1);
    }
}

/// <summary>Provides two opposing exponential curves joined at the midpoint.</summary>
[DisplayName("Double curve")]
[VerticalMirrorInterpolator]
[CustomDerivativeExtrema(new[] { 0d, 0.5d, 1d })]
public sealed class DoubleCurveInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator
{
    private readonly LinearInterpolator linearDegenerate = new();

    /// <summary>Creates a double-curve interpolator.</summary>
    public DoubleCurveInterpolator()
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
        return (Math.Exp(p * t) - 1) / (Math.Exp(p) - 1);
    }

    private static double Derivative(double t, double p)
    {
        return Math.Exp(p * t) * p / (Math.Exp(p) - 1);
    }

    private static double Primitive(double t, double p)
    {
        return t < 0.5
            ? (2 * p * t - Math.Exp(2 * p * t)) / (4 * p - 4 * Math.Exp(p) * p)
            : (2 * p * ((2 * Math.Exp(p) - 1) * t - Math.Exp(p)) + Math.Exp(p * (2 - 2 * t))) / (4 * (Math.Exp(p) - 1) * p);
    }
}

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
