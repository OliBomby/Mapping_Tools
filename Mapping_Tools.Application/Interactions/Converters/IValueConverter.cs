using System.Globalization;

namespace Mapping_Tools.Application.Interactions.Converters;

/// <summary>
///     Converts values in both directions without depending on a desktop binding framework.
/// </summary>
public interface IValueConverter
{
    /// <summary>
    ///     Converts a source value to the requested presentation or contract type.
    /// </summary>
    /// <param name="value">The source value, which may be <see langword="null" />.</param>
    /// <param name="targetType">The type expected by the consumer.</param>
    /// <param name="parameter">Optional converter-specific context supplied by the caller.</param>
    /// <param name="culture">The culture associated with the conversion request.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCastException">The source or target type is not supported.</exception>
    object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture);

    /// <summary>
    ///     Converts a presentation or contract value back to the requested source type.
    /// </summary>
    /// <param name="value">The value supplied by the consumer, which may be <see langword="null" />.</param>
    /// <param name="targetType">The source type expected after conversion.</param>
    /// <param name="parameter">Optional converter-specific context supplied by the caller.</param>
    /// <param name="culture">The culture associated with the conversion request.</param>
    /// <returns>The converted source value.</returns>
    /// <exception cref="FormatException">The supplied value has a supported type but invalid content.</exception>
    /// <exception cref="InvalidCastException">The source or target type is not supported.</exception>
    object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture);
}
