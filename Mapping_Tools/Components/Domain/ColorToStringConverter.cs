using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mapping_Tools.Components.Domain {
    internal class ColorToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var str = ((Color)value).ToString();
            if (str.Length == 9) {
                str = "#" + str.Substring(3, str.Length - 3);
            }
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            string str = value.ToString();
            return (Color)ColorConverter.ConvertFromString(str);
        }
    }
}
