using System.Globalization;
using Avalonia.Data.Converters;
using ApplicationInvariantInt32Converter = Mapping_Tools.Application.Interactions.Converters.InvariantInt32Converter;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts signed 32-bit integers to and from invariant-culture editable text.
/// </summary>
public sealed class InvariantInt32Converter : IValueConverter
{
    private static readonly ApplicationInvariantInt32Converter Converter = new();

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
            Converter);
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
            Converter);
    }
}
