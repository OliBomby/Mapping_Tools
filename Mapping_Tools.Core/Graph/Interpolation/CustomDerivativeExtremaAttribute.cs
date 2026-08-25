namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Marks interpolators whose derivative extrema need interior samples.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CustomDerivativeExtremaAttribute : Attribute
{
    /// <summary>Creates a derivative-extrema marker with normalized sample positions.</summary>
    /// <param name="extremaPositions">The positions at which extrema are known.</param>
    public CustomDerivativeExtremaAttribute(double[] extremaPositions)
    {
        ExtremaPositions = extremaPositions;
    }

    /// <summary>Gets the normalized positions at which derivative extrema are known.</summary>
    public double[] ExtremaPositions { get; }
}

