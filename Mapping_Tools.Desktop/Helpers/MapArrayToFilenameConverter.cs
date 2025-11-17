using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Helpers;

internal class MapArrayToFilenameConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not string[] pathArray) return string.Empty;
        return string.Join(" | ", pathArray.Select(Path.GetFileName));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new InvalidOperationException("MapArrayToFilenameConverter can not convert back values.");
    }
}