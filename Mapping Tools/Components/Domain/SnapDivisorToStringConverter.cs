using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain {
    internal class SnapDivisorToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) return "1/" + ((int) value).ToString(CultureInfo.InvariantCulture);

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return new ValidationResult(false, "Cannot convert back null.");
            }

            return int.Parse(value.ToString().Split('/')[1]);
        }
    }
}
