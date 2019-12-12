using System;
using System.Data;

namespace Mapping_Tools.Classes.SystemTools {
    public class TypeConverters {
        public static double ParseDouble(string str) {
            using (DataTable dt = new DataTable()) {
                string text = str.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToDouble(v);
            }
        }

        public static int ParseInt(string str) {
            using (DataTable dt = new DataTable()) {
                string text = str.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToInt32(v);
            }
        }

        public static bool TryParseDouble(string str, out double result, double defaultValue = -1) {
            try {
                result = ParseDouble(str);
                return true;
            } catch (Exception) {
                result = defaultValue;
                return false;
            }
        }

        public static bool TryParseDouble(string str, out double result) {
            try {
                result = ParseDouble(str);
                return true;
            } catch (Exception) {
                result = -1;
                return false;
            }
        }

        public static bool TryParseInt(string str, out int result, int defaultValue = -1) {
            try {
                result = ParseInt(str);
                return true;
            } catch (Exception) {
                result = defaultValue;
                return false;
            }
        }

        public static bool TryParseInt(string str, out int result) {
            try {
                result = ParseInt(str);
                return true;
            } catch (Exception) {
                result = -1;
                return false;
            }
        }
    }
}
