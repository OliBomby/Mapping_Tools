using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain
{
    public class MultiValueConverterGroup : List<IMultiValueConverter>, IMultiValueConverter
    {
        #region IValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((this.Count - 1) % values.Length != 0) {
                throw new ArgumentException("Could not interpret how to apply converters to values (make sure each value goes through the same number of converters, and there's one left at the end to combine them!)");
            }
            int conversionsPerValue = (this.Count - 1) / values.Length;
            for (int i = 0; i < values.Length; i++) {
                for (int j = 0; j < conversionsPerValue; j++) {
                    values[i] = this[j + i * conversionsPerValue].Convert(new object[] { values[i] }, targetType, parameter, culture);
                }
            }
            return this.Last().Convert(values, targetType, parameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
