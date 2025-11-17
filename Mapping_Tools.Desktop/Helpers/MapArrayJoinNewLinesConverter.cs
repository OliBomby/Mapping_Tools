using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Helpers;

internal class MapArrayJoinNewLinesConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not string[] pathArray) return string.Empty;
        return string.Join('\n', pathArray);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not string str) return Array.Empty<string>();
        return str.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
    }
}