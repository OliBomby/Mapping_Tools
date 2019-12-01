using System;
using System.Globalization;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain{
    /// <summary>
    /// Converts the <see cref="Enum"/> type to a boolean.
    /// </summary>
    public class EnumToBoolConverter : IValueConverter {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value != null && value.Equals(parameter);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value != null && ((bool)value) ? parameter : Binding.DoNothing;
        }
    }
}