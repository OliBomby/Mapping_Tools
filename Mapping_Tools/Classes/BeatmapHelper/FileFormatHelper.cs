using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Classes.BeatmapHelper;

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
        foreach ((string key, string value) in lines.Select(SplitKeyValue)) {
            dict[key] = new TValue(value);
        }
    }

    public static (string, string) SplitKeyValue(string line) {
        int index = line.IndexOf(':');
        return index == -1 ? (line.Trim(), string.Empty) : (line[..index].Trim(), line[(index + 1)..].Trim());
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

    public static IEnumerable<string> GetLinesPrefix(IEnumerable<string> lines, string[] prefixes) {
        foreach (string line in lines) {
            if (prefixes.Any(o => line.StartsWith(o))) {
                yield return line;
            }
        }
    }

    public static bool CategoryExists(IEnumerable<string> lines, string category) {
        return lines.Any(l => l == category);
    }
}