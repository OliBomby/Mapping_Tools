using System.Globalization;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing;

/// <summary>
/// Helper class for File Formats
/// </summary>
public static class FileFormatHelper {
    /// <summary>
    /// Splits the string into the key and the value.
    /// </summary>
    /// <param name="line">The string with a key and value separated by ": ".</param>
    /// <returns></returns>
    public static Tuple<string, string> SplitKeyValue(string line) {
        int index = line.IndexOf(':');

        if (index < 0) {
            return null;
        }

        string left = line.Remove(index).Trim();
        string right = line.Remove(0, index + 1).Trim();

        return new Tuple<string, string>(left, right);
    }

    public static IEnumerable<string> GetCategoryLines(IEnumerable<string> lines, string category, string[] categoryIdentifiers=null) {
        if (categoryIdentifiers == null)
            categoryIdentifiers = new[] { "[" };

        bool atCategory = false;

        foreach (string line in lines) {
            if (atCategory && !string.IsNullOrWhiteSpace(line)) {
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

    public static int ParseInt(string s) {
        return int.Parse(s, CultureInfo.InvariantCulture);
    }

    public static float ParseFloat(string s) {
        return float.Parse(s, CultureInfo.InvariantCulture);
    }

    public static double ParseDouble(string s) {
        return double.Parse(s, CultureInfo.InvariantCulture);
    }

    public static bool TryParseDouble(string str, out double result) {
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryParseInt(string str, out int result) {
        return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Converts the object to a string with invariant culture.
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

    public static string ToIntInvariant(this SampleSet obj) {
        return ((int)obj).ToInvariant();
    }

    public static string ToIntInvariant(this StoryboardLayer obj) {
        return ((int)obj).ToInvariant();
    }
}