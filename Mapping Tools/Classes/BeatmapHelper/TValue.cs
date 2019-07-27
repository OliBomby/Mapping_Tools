using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class TValue {
        public string StringValue { get; set; }
        public dynamic Value { get => GetValue(); set => SetValue(value); }

        public TValue(string str) {
            StringValue = str;
        }

        public dynamic GetValue() {
            if( double.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ) {
                if( StringValue.Split('.').Count() > 1 ) {
                    return d;
                }
                else {
                    return int.Parse(StringValue, CultureInfo.InvariantCulture);
                }
            }
            else {
                return StringValue;
            }
        }

        public void SetValue(dynamic value) {
            StringValue = value.ToString();
        }

        public string GetStringValue() {
            return StringValue;
        }
    }
}
