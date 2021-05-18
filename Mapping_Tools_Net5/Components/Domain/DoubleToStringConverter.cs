using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Domain {
    internal class DoubleToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) return ((double) value).ToString(CultureInfo.InvariantCulture);
            return parameter != null ? parameter.ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                if (parameter != null) {
                    return double.Parse(parameter.ToString());
                }
                return new ValidationResult(false, "Cannot convert back null.");
            }

            if (parameter == null) {
                if (TypeConverters.TryParseDouble(value.ToString(), out double result1)) {
                    return result1;
                }

                return new ValidationResult(false, "Double format error.");
            }
            TypeConverters.TryParseDouble(value.ToString(), out double result2, double.Parse(parameter.ToString()));
            return result2;
        }
    }
}
