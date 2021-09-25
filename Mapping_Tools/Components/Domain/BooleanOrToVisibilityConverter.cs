using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Mapping_Tools.Components.Domain
{
    public class BooleanOrToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (object value in values) {
                if ((value is bool) && (bool)value == false) {
                    return Visibility.Collapsed;
                }
            }
            return Visibility.Visible;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BooleanOrConverter is a OneWay converter.");
        }
    }
}
