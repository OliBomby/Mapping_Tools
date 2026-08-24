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

