using System.Globalization;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Application.Interactions.Converters;

/// <summary>
///     Formats durations invariantly and accepts either constant-format text or a millisecond expression.
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
        return ValueConverterHelper.RequireValue<TimeSpan>(value, targetType)
            .ToString("c", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        ValueConverterHelper.RequireTarget<TimeSpan>(targetType);
        string text = ValueConverterHelper.RequireText(value, targetType);
        if (TypeConverters.TryParseTimeSpan(text, out var converted)) return converted;

        throw new FormatException(
            "Use the format hh:mm:ss or enter an arithmetic expression in milliseconds.");
    }
}
