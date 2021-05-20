using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain {
    internal class DoubleArrayToStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is double[] beatDivisors)) return string.Empty;

            var builder = new StringBuilder();
            bool first = true;
            foreach (var d in beatDivisors) {
                if (!first) {
                    builder.Append(", ");
                }

                builder.Append(d.ToInvariant());

                first = false;
            }

            return builder.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is string str)) return new double[0];
            if (string.IsNullOrWhiteSpace(str)) return new double[0];

            var vals = str.Split(',');
            var beatDivisors = new double[vals.Length];

            for (int i = 0; i < vals.Length; i++) {
                var val = vals[i];

                var valid = TypeConverters.TryParseDouble(val, out double doubleValue);
                if (valid) {
                    beatDivisors[i] = doubleValue;
                } else {
                    return new ValidationResult(false, "Double format error.");
                }
            }

            return beatDivisors;
        }
    }
}
