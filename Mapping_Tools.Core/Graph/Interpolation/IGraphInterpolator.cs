namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Describes a normalized interpolation from zero to one.</summary>
public interface IGraphInterpolator
{
    /// <summary>Gets or sets the curve tension used by parameterized interpolators.</summary>
    double P { get; set; }

    /// <summary>Evaluates the interpolation at normalized position <paramref name="t" />.</summary>
    /// <param name="t">The normalized position, normally in the inclusive range zero to one.</param>
    /// <returns>The normalized interpolated value.</returns>
    double GetInterpolation(double t);
}

