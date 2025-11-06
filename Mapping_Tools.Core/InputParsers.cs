using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Core;

public class InputParsers {
    public static double ParseDouble(string str) {
        using DataTable dt = new DataTable();
        string text = str.Replace(",", ".");
        var v = dt.Compute(text, "");
        return Convert.ToDouble(v);
    }

    public static int ParseInt(string str) {
        using DataTable dt = new DataTable();
        string text = str.Replace(",", ".");
        var v = dt.Compute(text, "");
        return Convert.ToInt32(v);
    }

    public static bool TryParseDouble(string str, out double result, double defaultValue) {
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

    public static bool TryParseInt(string str, out int result, int defaultValue) {
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

    /// <summary>
    /// Valid timestamps:
    /// <example>00:00:891 (1) - </example>
    /// <example>60:00:074 (2,4) - </example>
    /// <example>60:00:074 - </example>
    /// <example>00:-01:-230 (1) - </example>
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static TimeSpan ParseOsuTimestamp(string str) {
        var split = str.Split(':');
        var time = TimeSpan.Zero;
        for (int i = 0; i < split.Length; i++) {
            // Use regex to filter out just the number part
            var match = Regex.Match(split[i], "-?[0-9]*");
            var trimmedString = match.Value;

            var intValue = int.Parse(trimmedString, CultureInfo.InvariantCulture);

            // Invert the index so 0 is the rightmost time value
            var pos = split.Length - 1 - i;
            time += pos switch {
                0 => TimeSpan.FromMilliseconds(intValue),
                1 => TimeSpan.FromSeconds(intValue),
                2 => TimeSpan.FromMinutes(intValue),
                3 => TimeSpan.FromHours(intValue),
                4 => TimeSpan.FromDays(intValue),
                _ => throw new ArgumentException(@"Provided timestamp has too many values.")
            };
        }

        return time;
    }
}