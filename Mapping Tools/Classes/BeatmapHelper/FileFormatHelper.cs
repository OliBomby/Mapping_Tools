using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

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

        public static void AddDictionaryToLines(Dictionary<string, TValue> dict, List<string> lines) {
            lines.AddRange(dict.Select(kvp => kvp.Key + ":" + kvp.Value.Value));
        }

        public static void FillDictionary(Dictionary<string, TValue> dict, IEnumerable<string> lines) {
            foreach (var split in lines.Select(SplitKeyValue)) {
                dict[split[0]] = new TValue(split[1]);
            }
        }

        public static string[] SplitKeyValue(string line) {
            return line.Split(new[] { ':' }, 2);
        }

        public static IEnumerable<string> GetCategoryLines(IEnumerable<string> lines, string category, string[] categoryIdentifiers=null) {
            if (categoryIdentifiers == null)
                categoryIdentifiers = new[] { "[" };

            bool atCategory = false;

            foreach (string line in lines) {
                if (atCategory && line != "") {
                    if (categoryIdentifiers.Any(o => line.StartsWith(o))) // Reached another category
                    {
                        yield break;
                    }
                    yield return line;
                }
                else {
                    if (line == category) {
                        atCategory = true;
                    }
                }
            }
        }
    }
}
