using System.Globalization;
using Avalonia.Data.Converters;
using ApplicationConstantTimeSpanConverter = Mapping_Tools.Application.Interactions.Converters.ConstantTimeSpanConverter;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts durations to and from the invariant constant TimeSpan format.
/// </summary>
public sealed class ConstantTimeSpanConverter : IValueConverter
{
    private static readonly ApplicationConstantTimeSpanConverter converter = new();

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
