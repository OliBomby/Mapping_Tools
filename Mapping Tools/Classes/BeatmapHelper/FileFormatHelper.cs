using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Globalization;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// Helper class for File Formats
    /// </summary>
    public static class FileFormatHelper {
        /// <summary>
        /// Converts the object to an Invariant string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToInvariant(this object obj) {
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the object to the rounded Invariant string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToRoundInvariant(this double obj) {
            return Math.Round(obj).ToInvariant();
        }

        /// <summary>
        /// Converts the string into 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
