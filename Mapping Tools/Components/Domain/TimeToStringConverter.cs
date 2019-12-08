using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain
{
    public class TimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null) return string.Format("{0:mm\\:ss\\:fff}", TimeSpan.FromMilliseconds((double)value));
            return parameter != null ? parameter.ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return TimeSpan.ParseExact(((string)value).Substring(0, ((string)value).IndexOf("(") - 1),
                                                @"mm\:ss\:fff", culture, TimeSpanStyles.AssumeNegative);
            }
            catch (FormatException)
            {
                return null;
            }
            catch (OverflowException) { return null; }
            catch ( ArgumentNullException)
            { return null; }


        }
    }
}
