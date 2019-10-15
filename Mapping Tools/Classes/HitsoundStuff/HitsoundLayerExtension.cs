using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Mapping_Tools.Classes.HitsoundStuff {
    static class HitsoundLayerExtension {
        public static string AllToStringOrDefault<TObj, TResult>(this List<TObj> list, Func<TObj, TResult> func, CultureInfo culture=null) {
            if (list.Count == 0)
                return "";
            TResult first = func(list.First());
            foreach (TObj o in list) {
                if (!func(o).Equals(first))
                    return "";
            }
            return Convert.ToString(first, culture);
        }

        public static string AllToStringOrDefault<TObj, TResult>(this List<TObj> list, Func<TObj, TResult> func, Func<TResult, string> stringConverter) {
            if (list.Count == 0)
                return "";
            TResult first = func(list.First());
            foreach (TObj o in list) {
                if (!func(o).Equals(first))
                    return "";
            }
            return stringConverter(first);
        }

        public static string DoubleListToStringConverter(List<double> list) {
            var accumulator = new StringBuilder(list.Count * 2); // Rough guess for capacity of StringBuilder
            foreach (double d in list) {
                accumulator.Append(d.ToString(CultureInfo.InvariantCulture)).Append(",");
            }
            if (accumulator.Length > 0)
                accumulator.Remove(accumulator.Length - 1, 1);
            return accumulator.ToString();
        }
    }
}
