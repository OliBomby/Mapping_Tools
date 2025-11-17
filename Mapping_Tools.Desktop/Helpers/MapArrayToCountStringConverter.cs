using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Helpers;

internal class MapArrayToCountStringConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not string[] pathArray) return "(0) maps total";
        int c = pathArray.Length;
        return c == 1 ? $"({c}) map total" : $"({c}) maps total";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new InvalidOperationException("MapArrayToCountStringConverter can not convert back values.");
    }
}