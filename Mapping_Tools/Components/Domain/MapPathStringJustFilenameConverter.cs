using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain;

internal class MapPathStringJustFilenameConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is not string str ? string.Empty : string.Join(" | ", str.Split('|').Select(Path.GetFileName));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new InvalidOperationException("MapPathStringJustFilenameConverter can not convert back values.");
    }
}