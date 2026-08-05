using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Interactions;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Converts durations to and from the invariant constant TimeSpan format.
/// </summary>
public sealed class ConstantTimeSpanConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        TextValueConverterHelper.Convert(
            value,
            targetType,
            TextValueConverters.ConstantTimeSpan);

    /// <inheritdoc/>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        TextValueConverterHelper.ConvertBack(
            value,
            targetType,
            TextValueConverters.ConstantTimeSpan);
}
