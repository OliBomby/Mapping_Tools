using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Domain {
    internal class IntToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) return ((int) value).ToString(CultureInfo.InvariantCulture);
            return parameter != null ? parameter.ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                if (parameter != null) {
                    return (int) parameter;
                }
                return new ValidationResult(false, "Cannot convert back null.");
            }

            if (parameter == null) {
                if (TypeConverters.TryParseInt(value.ToString(), out int result1)) {
                    return result1;
                }

                return new ValidationResult(false, "Int format error.");
            }
            TypeConverters.TryParseInt(value.ToString(), out int result2, int.Parse(parameter.ToString()));
            return result2;
        }
    }
}
