using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Core.Graph.Interpolation;

/// <summary>Provides the stable legacy ordering and metadata for built-in interpolators.</summary>
public static class GraphInterpolatorCatalog
{
    private static readonly Type[] builtInTypes =
    [
        typeof(SingleCurveInterpolator),
        typeof(SingleCurveInterpolator2),
        typeof(SingleCurveInterpolator3),
        typeof(DoubleCurveInterpolator),
        typeof(DoubleCurveInterpolator2),
        typeof(DoubleCurveInterpolator3),
        typeof(HalfSineInterpolator),
        typeof(WaveInterpolator),
        typeof(ParabolaInterpolator),
    ];

    /// <summary>Gets the interpolator types displayed by the graph context menu.</summary>
    public static IReadOnlyList<Type> GetInterpolators()
    {
        return builtInTypes;
    }

    /// <summary>Gets the display name for an interpolator type.</summary>
    /// <param name="type">The interpolator type.</param>
    /// <returns>The display name declared by the type, or its CLR name.</returns>
    public static string GetName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetCustomAttributes(typeof(DisplayNameAttribute), false)
                   .OfType<DisplayNameAttribute>()
                   .Select(attribute => attribute.DisplayName)
                   .FirstOrDefault()
               ?? type.Name;
    }

    /// <summary>Gets the stable menu index for an interpolator type.</summary>
    /// <param name="type">The interpolator type.</param>
    /// <returns>The menu index, or -1 when the type is not selectable.</returns>
    public static int GetInterpolatorIndex(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Array.IndexOf(builtInTypes, type);
    }

    /// <summary>Gets an interpolator type from a persisted menu index.</summary>
    /// <param name="index">The persisted menu index.</param>
    /// <returns>The indexed type, or the default single-curve type for invalid input.</returns>
    public static Type GetInterpolatorByIndex(int index)
    {
        return index >= 0 && index < builtInTypes.Length
            ? builtInTypes[index]
            : typeof(SingleCurveInterpolator);
    }

    /// <summary>Creates an interpolator of the requested type.</summary>
    /// <param name="type">A parameterless interpolator type.</param>
    /// <returns>A new interpolator instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the type cannot be instantiated as an interpolator.</exception>
    public static IGraphInterpolator GetInterpolator(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!typeof(IGraphInterpolator).IsAssignableFrom(type) || type.IsAbstract) throw new ArgumentException("The type is not a concrete graph interpolator.", nameof(type));

        return (IGraphInterpolator)(Activator.CreateInstance(type)
                                    ?? throw new ArgumentException("The interpolator has no usable parameterless constructor.", nameof(type)));
    }

    /// <summary>Copies an interpolator while retaining its tension parameter.</summary>
    /// <param name="interpolator">The interpolator to copy.</param>
    /// <returns>A new interpolator instance with the same parameter.</returns>
    public static IGraphInterpolator Clone(IGraphInterpolator interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        if (interpolator.GetType() == typeof(CustomInterpolator) && interpolator is CustomInterpolator custom)
            return new CustomInterpolator(custom.InterpolationFunction) { P = custom.P };

        var clone = GetInterpolator(interpolator.GetType());
        clone.P = interpolator.P;
        return clone;
    }

    /// <summary>Gets the largest absolute sampled value of an interpolator.</summary>
    /// <param name="interpolator">The interpolator to inspect.</param>
    /// <returns>The largest absolute value at its endpoints or declared extrema.</returns>
    public static double GetBiggestValue(IGraphInterpolator interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        double[] positions = interpolator.GetType().GetCustomAttributes(typeof(CustomExtremaAttribute), false)
            .OfType<CustomExtremaAttribute>().SelectMany(attribute => attribute.ExtremaPositions).DefaultIfEmpty(0).ToArray();
        if (positions.Length == 1 && positions[0] == 0) positions = [0, 1];

        return positions.Max(position => Math.Abs(interpolator.GetInterpolation(position)));
    }

    /// <summary>Gets the largest absolute sampled derivative of an interpolator.</summary>
    /// <param name="interpolator">The interpolator to inspect.</param>
    /// <returns>The largest absolute derivative, or one for a non-derivable interpolator.</returns>
    public static double GetBiggestDerivative(IGraphInterpolator interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        if (interpolator is not IDerivableInterpolator derivable) return 1;

        double[] positions = interpolator.GetType().GetCustomAttributes(typeof(CustomDerivativeExtremaAttribute), false)
            .OfType<CustomDerivativeExtremaAttribute>().SelectMany(attribute => attribute.ExtremaPositions).DefaultIfEmpty(0).ToArray();
        if (positions.Length == 1 && positions[0] == 0) positions = [0, 1];

        return positions.Max(position => Math.Abs(derivable.GetDerivative(position)));
    }
}
