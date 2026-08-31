using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts double-precision values to and from invariant round-trip editable text.
/// </summary>
public sealed class InvariantDoubleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.Convert(() =>
        {
            double converted = ValueConverterHelper.RequireValue<double>(value, targetType);
            return converted == 727
                ? "727 WYSI"
                : converted.ToString("R", CultureInfo.InvariantCulture);
        });
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
            ValueConverterHelper.RequireTarget<double>(targetType);
            string text = ValueConverterHelper.RequireText(value, targetType);
            if (text == "727 WYSI") return 727d;

            if (TypeConverters.TryParseDouble(text, out double converted)) return converted;

            if (parameter is not null
                && TypeConverters.TryParseDouble(parameter.ToString()!, out double fallback))
                return fallback;

            throw new FormatException("Enter a valid number or arithmetic expression.");
        });
    }
}
