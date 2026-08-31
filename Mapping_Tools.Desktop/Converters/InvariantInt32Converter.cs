using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts signed 32-bit integers to and from invariant-culture editable text.
/// </summary>
public sealed class InvariantInt32Converter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.Convert(() => ValueConverterHelper.RequireValue<int>(value, targetType)
            .ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.ConvertBack(() =>
        {
            ValueConverterHelper.RequireTarget<int>(targetType);
            string text = ValueConverterHelper.RequireText(value, targetType);
            if (TypeConverters.TryParseInt(text, out int converted)) return converted;

            throw new FormatException("Enter a whole number or arithmetic expression.");
        });
    }
}
