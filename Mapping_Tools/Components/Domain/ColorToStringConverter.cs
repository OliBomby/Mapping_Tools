using System;
using System.Globalization;
using System.Windows.Controls;
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
            if (value is not string str)
                return new ValidationResult(false, "Cannot convert back null.");

            if (str.Length > 0 && str[0] != '#')
                str = "#" + str;

            try {
                return (Color)ColorConverter.ConvertFromString(str)!;
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            return new ValidationResult(false, "Color format error.");
        }
    }
}
