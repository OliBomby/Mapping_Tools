using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Helpers;

internal class MapPathStringToCountStringConverter : IValueConverter {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        int c = 0;
        if (!string.IsNullOrEmpty(value as string)) {
            c = ((string) value).Split('|').Length;
        }
        return c == 1 ? $"({c}) map total" : $"({c}) maps total";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return string.Empty;
    }
}