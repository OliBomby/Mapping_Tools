using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts durations to and from the invariant constant TimeSpan format.
/// </summary>
public sealed class ConstantTimeSpanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ValueConverterHelper.Convert(() => ValueConverterHelper.RequireValue<TimeSpan>(value, targetType)
            .ToString("c", CultureInfo.InvariantCulture));
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
            ValueConverterHelper.RequireTarget<TimeSpan>(targetType);
            string text = ValueConverterHelper.RequireText(value, targetType);
            if (TypeConverters.TryParseTimeSpan(text, out var converted)) return converted;

            throw new FormatException(
                "Use the format hh:mm:ss or enter an arithmetic expression in milliseconds.");
        });
    }
}
