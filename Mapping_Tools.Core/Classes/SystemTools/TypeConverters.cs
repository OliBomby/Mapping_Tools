using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Core.Classes.SystemTools {
    /// <summary>
    /// Parses user-entered numeric expressions and osu!-style timestamps.
    /// </summary>
    public class TypeConverters {
        /// <summary>
        /// Evaluates a simple arithmetic expression and converts its result to a floating-point value.
        /// </summary>
        /// <param name="str">An expression accepted by <see cref="DataTable.Compute(string, string)"/>; commas are treated as decimal points.</param>
        /// <returns>The evaluated numeric result.</returns>
        public static double ParseDouble(string str) {
            using (DataTable dt = new DataTable()) {
                string text = str.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToDouble(v);
            }
        }

        /// <summary>
        /// Evaluates a simple arithmetic expression and rounds its result using <see cref="Convert.ToInt32(object)"/>.
        /// </summary>
        /// <param name="str">An expression accepted by <see cref="DataTable.Compute(string, string)"/>; commas are treated as decimal points.</param>
        /// <returns>The evaluated result converted to a 32-bit integer.</returns>
        public static int ParseInt(string str) {
            using (DataTable dt = new DataTable()) {
                string text = str.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToInt32(v);
            }
        }

        /// <summary>
        /// Attempts to evaluate a floating-point expression without propagating parser or conversion failures.
        /// </summary>
        /// <param name="str">The expression to evaluate.</param>
        /// <param name="result">The evaluated value, or <paramref name="defaultValue"/> on failure.</param>
        /// <param name="defaultValue">The sentinel assigned when evaluation fails.</param>
        /// <returns><see langword="true"/> when the expression was evaluated and converted successfully.</returns>
        public static bool TryParseDouble(string str, out double result, double defaultValue = -1) {
            try {
                result = ParseDouble(str);
                return true;
            } catch (Exception) {
                result = defaultValue;
                return false;
            }
        }

        /// <summary>
        /// Attempts to evaluate a floating-point expression, using <c>-1</c> as the failure sentinel.
        /// </summary>
        /// <param name="str">The expression to evaluate.</param>
        /// <param name="result">The evaluated value, or <c>-1</c> on failure.</param>
        /// <returns><see langword="true"/> when the expression was evaluated and converted successfully.</returns>
        public static bool TryParseDouble(string str, out double result) {
            try {
                result = ParseDouble(str);
                return true;
            } catch (Exception) {
                result = -1;
                return false;
            }
        }

        /// <summary>
        /// Attempts to evaluate an integer expression without propagating parser or conversion failures.
        /// </summary>
        /// <param name="str">The expression to evaluate.</param>
        /// <param name="result">The converted value, or <paramref name="defaultValue"/> on failure.</param>
        /// <param name="defaultValue">The sentinel assigned when evaluation fails.</param>
        /// <returns><see langword="true"/> when the expression was evaluated and converted successfully.</returns>
        public static bool TryParseInt(string str, out int result, int defaultValue = -1) {
            try {
                result = ParseInt(str);
                return true;
            } catch (Exception) {
                result = defaultValue;
                return false;
            }
        }

        /// <summary>
        /// Attempts to evaluate an integer expression, using <c>-1</c> as the failure sentinel.
        /// </summary>
        /// <param name="str">The expression to evaluate.</param>
        /// <param name="result">The converted value, or <c>-1</c> on failure.</param>
        /// <returns><see langword="true"/> when the expression was evaluated and converted successfully.</returns>
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
        /// Parses an invariant constant-format duration or evaluates a millisecond expression.
        /// </summary>
        /// <param name="str">
        /// A <c>[-][d.]hh:mm:ss[.fffffff]</c> duration, or an arithmetic expression whose result is milliseconds.
        /// </param>
        /// <returns>The parsed or evaluated duration.</returns>
        /// <exception cref="FormatException">The input is neither a constant-format duration nor a valid numeric expression.</exception>
        /// <exception cref="OverflowException">The evaluated milliseconds exceed the <see cref="TimeSpan"/> range.</exception>
        public static TimeSpan ParseTimeSpan(string str) {
            if (TimeSpan.TryParseExact(
                    str,
                    "c",
                    CultureInfo.InvariantCulture,
                    out TimeSpan duration)) {
                return duration;
            }

            return TimeSpan.FromMilliseconds(ParseDouble(str));
        }

        /// <summary>
        /// Attempts to parse a constant-format duration or evaluate a millisecond expression.
        /// </summary>
        /// <param name="str">The duration text or millisecond expression to evaluate.</param>
        /// <param name="result">The converted duration, or <see cref="TimeSpan.Zero"/> on failure.</param>
        /// <returns><see langword="true"/> when the input produces a duration within the supported range.</returns>
        public static bool TryParseTimeSpan(string str, out TimeSpan result) {
            try {
                result = ParseTimeSpan(str);
                return true;
            } catch (Exception) {
                result = TimeSpan.Zero;
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
        /// <param name="str">A colon-separated timestamp; annotations after each numeric component are ignored.</param>
        /// <returns>The accumulated duration, including negative components when present.</returns>
        /// <exception cref="ArgumentException">The timestamp contains more than day, hour, minute, second, and millisecond components.</exception>
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
                switch (pos) {
                    case 0:
                        time += TimeSpan.FromMilliseconds(intValue);
                        break;
                    case 1:
                        time += TimeSpan.FromSeconds(intValue);
                        break;
                    case 2:
                        time += TimeSpan.FromMinutes(intValue);
                        break;
                    case 3:
                        time += TimeSpan.FromHours(intValue);
                        break;
                    case 4:
                        time += TimeSpan.FromDays(intValue);
                        break;
                    default:
                        throw new ArgumentException(@"Provided timestamp has too many values.");
                }
            }
            
            return time;
        }
    }
}
