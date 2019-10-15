using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Globalization;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// 
    /// </summary>
    public static class FileFormatHelper {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToInvariant(this object obj) {
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        public static string ToRoundInvariant(this double obj) {
            return Math.Round(obj).ToInvariant();
        }

        public static string ToIntInvariant(this SampleSet obj) {
            return ((int)obj).ToInvariant();
        }

        public static string ToIntInvariant(this StoryboardLayer obj) {
            return ((int)obj).ToInvariant();
        }

        public static bool TryParseDouble(string str, out double result) {
            return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseInt(string str, out int result) {
            return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
