using System.Globalization;
using Avalonia.Data.Converters;
using ApplicationInvariantDoubleConverter = Mapping_Tools.Application.Interactions.Converters.InvariantDoubleConverter;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts double-precision values to and from invariant round-trip editable text.
/// </summary>
public sealed class InvariantDoubleConverter : IValueConverter
{
    private static readonly ApplicationInvariantDoubleConverter converter = new();

    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.Convert(
            value,
            targetType,
            parameter,
            culture,
            converter);
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.ConvertBack(
            value,
            targetType,
            parameter,
            culture,
            converter);
    }
}
