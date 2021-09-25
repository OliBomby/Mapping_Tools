using System;
using System.Globalization;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain
{
    public class IdentityConverter : IValueConverter, IMultiValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

