using System.Globalization;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Domain;

public record HitObjectReference(int? ComboIndex = null, int? Time = null, int? ColumnIndex = null);

/// <summary>
/// Utility class for parsing timestamps as seen in osu! clipboard functionality and modding.
/// </summary>
public static partial class TimestampParser
{
    // Matches:
    //   <time>                      e.g. "00:00:891", "60:00:074", "00:-01:-230"
    //   optional " ( ... ) "        e.g. "(1)" or "(57031|2,57411|1)"
    //   optional " - ..." suffix    e.g. " - ", " - words"
    //
    // time = 3..5 signed integer parts separated by colon
    //        (minutes:seconds:milliseconds) [+ optional hours] [+ optional days]
    [GeneratedRegex(@"^\s*(?<time>[+\-]?\d+(?::[+\-]?\d+){2,4})\s*(?:\(\s*(?<refs>[^)]*?)\s*\))?\s*(?:-\s*.*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex TimestampRegex();

    // Each ref item is either:
    //   - a single signed int: combo index
    //   - two signed ints separated by '|': time(ms) | columnIndex
    [GeneratedRegex(@"^\s*(?<a>[+\-]?\d+)\s*(?:\|\s*(?<b>[+\-]?\d+)\s*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex RefItemRegex();

    /// <summary>
    /// Parses a timestamp string into a TimeSpan and a list of HitObjectReferences.
    /// <example>00:00:891</example>
    /// <example>00:00:891 (1)</example>
    /// <example>60:00:074 (2,4) - </example>
    /// <example>60:00:074 - </example>
    /// <example>60:00:074 - words</example>
    /// <example>00:-01:-230 (1) - </example>
    /// <example>00:57:031 (57031|2,57411|1,57790|0,58170|2,58170|3) - </example>
    /// </summary>
    /// <param name="timestamp">The timestamp to parse.</param>
    /// <returns>(TimeSpan time, List&lt;HitObjectReference&gt; references)</returns>
    /// <exception cref="ArgumentException">Thrown when the input is malformed.</exception>
    public static (TimeSpan, List<HitObjectReference>) ParseTimestamp(string timestamp)
    {
        if (timestamp is null)
            throw new ArgumentException("Timestamp cannot be null.", nameof(timestamp));

        var timestampRegex = TimestampRegex();
        var m = timestampRegex.Match(timestamp);
        if (!m.Success)
            throw new ArgumentException("Timestamp does not match the expected pattern.", nameof(timestamp));

        // Parse the time portion (3..5 parts).
        var timeParts = m.Groups["time"].Value.Split(':');
        if (timeParts.Length is < 3 or > 5)
            throw new ArgumentException("Timestamp must have 3 to 5 colon-separated parts.", nameof(timestamp));

        // Map from right to left: ... days? : hours? : minutes : seconds : milliseconds
        // Allow signed parts; compute total milliseconds to support mixed signs safely.
        int[] nums;
        try
        {
            nums = timeParts.Select(ParseIntStrict).ToArray();
        }
        catch (OverflowException ex)
        {
            throw new ArgumentException("One of the time parts is too large.", nameof(timestamp), ex);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Time parts must be integers.", nameof(timestamp), ex);
        }

        // Rightmost indices
        int ms = nums[^1];
        int sec = nums[^2];
        int min = nums[^3];
        int hours = nums.Length >= 4 ? nums[^4] : 0;
        int days  = nums.Length == 5 ? nums[^5] : 0;

        // Convert to total milliseconds (long to avoid overflow), then to TimeSpan.
        long totalMs =
              (long)days  * 24 * 60 * 60 * 1000
            + (long)hours * 60 * 60 * 1000
            + (long)min   * 60 * 1000
            + (long)sec   * 1000
            + ms;

        // TimeSpan.FromMilliseconds handles negative totals as expected.
        var time = TimeSpan.FromMilliseconds(totalMs);

        // Parse references (if any).
        var refsText = m.Groups["refs"].Success ? m.Groups["refs"].Value.Trim() : null;
        var refs = new List<HitObjectReference>();

        if (string.IsNullOrEmpty(refsText)) {
            return (time, refs);
        }

        // Allow empty between commas? No: treat as error if malformed token appears.
        var items = refsText.Split(',');
        var refItemRegex = RefItemRegex();
        foreach (var raw in items)
        {
            var s = raw.Trim();
            if (s.Length == 0)
                throw new ArgumentException("Empty reference item found.", nameof(timestamp));

            var rm = refItemRegex.Match(s);
            if (!rm.Success)
                throw new ArgumentException($"Invalid reference item: '{s}'.", nameof(timestamp));

            var aText = rm.Groups["a"].Value;
            var bGroup = rm.Groups["b"];
            int a = ParseIntStrict(aText);

            if (bGroup.Success)
            {
                // Format: time(ms)|columnIndex -> ComboIndex = -1 (not used)
                int b = ParseIntStrict(bGroup.Value);
                refs.Add(new HitObjectReference(Time: a, ColumnIndex: b));
            }
            else
            {
                // Single number -> combo index; mark Time/ColumnIndex as -1 (sentinel)
                refs.Add(new HitObjectReference(ComboIndex: a));
            }
        }

        return (time, refs);
    }

    private static int ParseIntStrict(string s)
    {
        // Reject whitespace and non-integers explicitly.
        if (!int.TryParse(s, NumberStyles.AllowLeadingSign,
                          CultureInfo.InvariantCulture, out int value))
            throw new FormatException($"Invalid integer: '{s}'.");
        return value;
    }
}