using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Marks interpolators whose integral extrema need interior samples.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CustomIntegralExtremaAttribute : Attribute
{
    /// <summary>Creates an integral-extrema marker with normalized sample positions.</summary>
    /// <param name="extremaPositions">The positions at which extrema are known.</param>
    public CustomIntegralExtremaAttribute(double[] extremaPositions)
    {
        ExtremaPositions = extremaPositions;
    }

    /// <summary>Gets the normalized positions at which integral extrema are known.</summary>
    public double[] ExtremaPositions { get; }
}

