using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mapping_Tools.Core.BeatmapHelper.IO;

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
}