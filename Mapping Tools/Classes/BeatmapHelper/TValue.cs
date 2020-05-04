using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// Helper class for a single string that can represent multiple data types.
    /// Provides methods for converting data to and from string.
    /// </summary>
    public class TValue {
        public string Value { get; set; }

        public int IntValue {
            get => GetInt();
            set => SetInt(value);
        }

        public double DoubleValue {
            get => GetDouble();
            set => SetDouble(value);
        }

        public TValue() { }

        public TValue(string str) {
            Value = str;
        }

        public void SetValue(object value) {
            Value = value.ToInvariant();
        }

        public int GetInt() => int.Parse(Value, CultureInfo.InvariantCulture);

        public void SetInt(int value) => Value = value.ToInvariant();

        public bool IsInt() => !string.IsNullOrEmpty(Value) && Regex.IsMatch(Value, @"^\-?[0-9]$");

        public double GetDouble() => double.Parse(Value, NumberStyles.Float, CultureInfo.InvariantCulture);

        public void SetDouble(double value) => Value = value.ToInvariant();

        public bool IsDouble() => !string.IsNullOrEmpty(Value) && Regex.IsMatch(Value, @"^\-?[0-9]+(\.[0-9]+)?$");

        public List<double> GetDoubleList() => Value.Split(',').Select(v => double.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture)).ToList();
    }
}
