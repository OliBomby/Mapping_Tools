using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Formats beatmap path arrays as vertical-bar-separated text and parses edited text back to paths.
/// </summary>
public sealed class StringArrayToStringConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IEnumerable<string> paths ? string.Join('|', paths) : string.Empty;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.Split(
                   '|',
                   StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
               ?? [];
    }
}
