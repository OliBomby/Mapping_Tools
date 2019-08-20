using System;
using System.Globalization;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain {
    class SnapDivisorToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return $"1/{(int)value}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return int.Parse(value.ToString().Split('/')[1]);
        }
    }
}
