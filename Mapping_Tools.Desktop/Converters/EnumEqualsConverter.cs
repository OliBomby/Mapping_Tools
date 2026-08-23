using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Shows a control when an enum value matches a parameter name.</summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Enum current && parameter is string expected && string.Equals(current.ToString(), expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
