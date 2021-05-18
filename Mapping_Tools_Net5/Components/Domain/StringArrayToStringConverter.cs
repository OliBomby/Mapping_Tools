using System;
using System.Globalization;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain {
    class StringArrayToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value == null ? "" : string.Join("|", (string[])value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value?.ToString().Split('|');
        }
    }
}
