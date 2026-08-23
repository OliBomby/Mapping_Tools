using System.Globalization;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Application.Interactions.Converters;

/// <summary>
///     Formats signed 32-bit integers invariantly and evaluates user-entered arithmetic expressions when converting back.
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
        return ValueConverterHelper.RequireValue<int>(value, targetType)
            .ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        ValueConverterHelper.RequireTarget<int>(targetType);
        string text = ValueConverterHelper.RequireText(value, targetType);
        if (TypeConverters.TryParseInt(text, out int converted)) return converted;

        throw new FormatException("Enter a whole number or arithmetic expression.");
    }
}
