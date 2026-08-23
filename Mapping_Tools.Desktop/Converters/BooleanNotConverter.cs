using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Converts a Boolean value to its inverse visibility state.</summary>
public sealed class BooleanNotConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolean && !boolean;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
