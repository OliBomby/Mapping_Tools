using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Mapping_Tools.Desktop.Converters;

internal sealed class MaterialIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is MaterialIconKind icon
            ? new MaterialIcon { Kind = icon }
            : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
