using System.ComponentModel;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;

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

/// <summary>Extends an interpolator with an analytic first derivative.</summary>
public interface IDerivableInterpolator
{
    /// <summary>Evaluates the derivative at normalized position <paramref name="t" />.</summary>
    /// <param name="t">The normalized position.</param>
    /// <returns>The derivative of the normalized interpolation.</returns>
    double GetDerivative(double t);
}

/// <summary>Extends an interpolator with an analytic definite integral.</summary>
public interface IIntegrableInterpolator
{
    /// <summary>Integrates the normalized interpolation between two positions.</summary>
    /// <param name="t1">The start position.</param>
    /// <param name="t2">The end position.</param>
    /// <returns>The signed area under the normalized interpolation.</returns>
    double GetIntegral(double t1, double t2);
}

/// <summary>Extends an interpolator with inverse lookup support.</summary>
public interface IInvertibleInterpolator
{
    /// <summary>Finds normalized positions whose interpolation equals <paramref name="y" />.</summary>
    /// <param name="y">The normalized value to invert.</param>
    /// <returns>All known normalized positions in the interpolation domain.</returns>
    IEnumerable<double> GetInverse(double y);
}

/// <summary>Marks an interpolator that is not offered in the user-facing selection menu.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IgnoreInterpolatorAttribute : Attribute;

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

/// <summary>Marks an interpolator whose direction can be mirrored vertically by the editor.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class VerticalMirrorInterpolatorAttribute : Attribute;

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
