using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Displays preview timestamps as osu! time text and parses timestamp or numeric input.
/// </summary>
public sealed class TimeToStringConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not double milliseconds) return string.Empty;

        if (parameter is not null) return milliseconds.ToString("R", CultureInfo.InvariantCulture);

        try
        {
            var time = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(time.Days > 0 ? $"{time.Days:####}:" : string.Empty)}"
                   + $"{(time.Hours > 0 ? $"{time.Hours:00}:" : string.Empty)}"
                   + $"{time.Minutes:00}:{time.Seconds:00}:{time.Milliseconds:000}";
        }
        catch (OverflowException)
        {
            return milliseconds.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string text)
            return new BindingNotification(
                new FormatException("Enter a valid time."),
                BindingErrorType.DataValidationError);

        try
        {
            return TypeConverters.ParseOsuTimestamp(text).TotalMilliseconds;
        }
        catch (Exception)
        {
            if (TypeConverters.TryParseDouble(text, out double result)) return result;

            if (parameter is not null) return -1d;

            return new BindingNotification(
                new FormatException("Time format error."),
                BindingErrorType.DataValidationError);
        }
    }
}
