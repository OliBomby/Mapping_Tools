using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Interactions;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Converts signed 32-bit integers to and from invariant-culture editable text.
/// </summary>
public sealed class InvariantInt32Converter : IValueConverter
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
            TextValueConverters.InvariantInt32);

    /// <inheritdoc/>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        TextValueConverterHelper.ConvertBack(
            value,
            targetType,
            TextValueConverters.InvariantInt32);
}
