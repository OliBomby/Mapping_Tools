namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Marks interpolators whose displayed curve extrema need interior samples.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CustomExtremaAttribute : Attribute
{
    /// <summary>Creates an extrema marker with normalized sample positions.</summary>
    /// <param name="extremaPositions">The positions at which extrema are known.</param>
    public CustomExtremaAttribute(double[] extremaPositions)
    {
        ExtremaPositions = extremaPositions;
    }

    /// <summary>Gets the normalized positions at which extrema are known.</summary>
    public double[] ExtremaPositions { get; }
}

