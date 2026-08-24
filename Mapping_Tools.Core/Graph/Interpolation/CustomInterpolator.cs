using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Provides an interpolator backed by a caller-supplied function.</summary>
[IgnoreInterpolator]
[DisplayName("Custom")]
public class CustomInterpolator : IGraphInterpolator
{
    /// <summary>Evaluates a custom interpolation function.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The interpolated value.</returns>
    public delegate double InterpolationDelegate(double t);

    /// <summary>Creates an identity custom interpolator.</summary>
    public CustomInterpolator() : this(t => t)
    {
    }

    /// <summary>Creates an interpolator backed by <paramref name="interpolationFunction" />.</summary>
    /// <param name="interpolationFunction">The function to invoke for each evaluation.</param>
    public CustomInterpolator(InterpolationDelegate interpolationFunction)
    {
        InterpolationFunction = interpolationFunction ?? throw new ArgumentNullException(nameof(interpolationFunction));
    }

    /// <summary>Gets or sets the function used to evaluate this interpolator.</summary>
    public InterpolationDelegate InterpolationFunction { get; set; }

    /// <inheritdoc />
    public double P { get; set; }

    /// <inheritdoc />
    public double GetInterpolation(double t)
    {
        return InterpolationFunction(t);
    }
}

