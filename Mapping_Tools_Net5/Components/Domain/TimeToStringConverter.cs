using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Domain {
    public class TimeToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is double timeValue)) {
                return "";
            }

            var timeSpan = TimeSpan.FromMilliseconds(timeValue);

            try {
                return parameter != null
                    ? value.ToInvariant()
                    : $"{(timeSpan.Days > 0 ? $"{timeSpan.Days:####}:" : string.Empty)}" +
                      $"{(timeSpan.Hours > 0 ? $"{timeSpan.Hours:00}:" : string.Empty)}" +
                      $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}:{timeSpan.Milliseconds:000}";
            } catch (OverflowException) {
                return value.ToInvariant();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is string str)) {
                return null;
            }

            try {
                return TypeConverters.ParseOsuTimestamp(str).TotalMilliseconds;
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            if (parameter is string s) {
                return TypeConverters.TryParseDouble(str, out double result2) ? result2 : double.Parse(s, CultureInfo.InvariantCulture);
            }
            if (TypeConverters.TryParseDouble(str, out double result3)) {
                return result3;
            }

            return new ValidationResult(false, "Time format error.");
        }
    }
}