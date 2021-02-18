using System;
using System.Globalization;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Events;

namespace Mapping_Tools_Core {
    public static class InvariantHelper {
        /// <summary>
        /// Converts the object to an Invariant string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToInvariant(this object obj) {
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the double to the rounded Invariant string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToRoundInvariant(this double obj) {
            return Math.Round(obj).ToInvariant();
        }

        /// <summary>
        /// Converts the float to the rounded Invariant string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToRoundInvariant(this float obj) {
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