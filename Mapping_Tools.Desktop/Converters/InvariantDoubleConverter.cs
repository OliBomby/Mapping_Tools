using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Interactions;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Converts double-precision values to and from invariant round-trip editable text.
/// </summary>
public sealed class InvariantDoubleConverter : IValueConverter
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
            TextValueConverters.InvariantDouble);

    /// <inheritdoc/>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        TextValueConverterHelper.ConvertBack(
            value,
            targetType,
            TextValueConverters.InvariantDouble);
}
