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

            try {
                return parameter != null
                    ? value.ToInvariant()
                    : $"{TimeSpan.FromMilliseconds(timeValue):mm\\:ss\\:fff}";
            } catch (OverflowException) {
                return value.ToInvariant();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is string str)) {
                return null;
            }

            var bracketIndex = str.IndexOf("(", StringComparison.Ordinal);
            if (TimeSpan.TryParseExact(str.Substring(0, bracketIndex == -1 ? str.Length : bracketIndex - 1).Trim(),
                @"mm\:ss\:fff", culture, TimeSpanStyles.None, out var result)) {
                return result.TotalMilliseconds;
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