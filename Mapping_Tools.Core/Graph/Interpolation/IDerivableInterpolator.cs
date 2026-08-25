namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Extends an interpolator with an analytic first derivative.</summary>
public interface IDerivableInterpolator
{
    /// <summary>Evaluates the derivative at normalized position <paramref name="t" />.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The derivative of the normalized interpolation.</returns>
    double GetDerivative(double t);
}

