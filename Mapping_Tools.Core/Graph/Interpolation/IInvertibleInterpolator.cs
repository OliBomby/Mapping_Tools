using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Extends an interpolator with inverse lookup support.</summary>
public interface IInvertibleInterpolator
{
    /// <summary>Finds normalized positions whose interpolation equals <paramref name="y" />.</summary>
    /// <param name="y">The normalized value to invert.</param>
    /// <returns>All known normalized positions in the interpolation domain.</returns>
    IEnumerable<double> GetInverse(double y);
}

