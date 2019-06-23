using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Domain
{
    class StringToDoubleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            TypeConverters.TryParseDouble(value.ToString(), out double result, (double)parameter);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return ((double)value).ToString(CultureInfo.InvariantCulture);
        }
    }
}
