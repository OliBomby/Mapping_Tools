using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Extends an interpolator with an analytic definite integral.</summary>
public interface IIntegrableInterpolator
{
    /// <summary>Integrates the normalized interpolation between two positions.</summary>
    /// <param name="t1">The start position.</param>
    /// <param name="t2">The end position.</param>
    /// <returns>The signed area under the normalized interpolation.</returns>
    double GetIntegral(double t1, double t2);
}

