using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.SystemTools;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Edits beat-divisor arrays using the legacy comma-separated rational or positive-number format.
///     Invalid text remains in the field and is reported through Avalonia binding validation.
/// </summary>
public sealed class BeatDivisorArrayToStringConverter : IValueConverter
{
    /// <summary>Formats rational divisors as fractions and irrational divisors as invariant numbers.</summary>
    /// <param name="value">The divisor array supplied by the binding source.</param>
    /// <param name="targetType">The binding target type, which must accept text.</param>
    /// <param name="parameter">Unused converter configuration.</param>
    /// <param name="culture">Unused UI culture; the persisted format is invariant.</param>
    /// <returns>The comma-separated legacy representation, or an empty string for a non-array value.</returns>
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return value is IBeatDivisor[] beatDivisors
            ? string.Join(", ", beatDivisors.Select(Format))
            : string.Empty;
    }

    /// <summary>
    ///     Parses comma-separated positive fractions or invariant numeric expressions into divisors.
    /// </summary>
    /// <param name="value">The edited field text.</param>
    /// <param name="targetType">The binding source type, which must accept a divisor array.</param>
    /// <param name="parameter">Unused converter configuration.</param>
    /// <param name="culture">Unused UI culture; the legacy input format is invariant.</param>
    /// <returns>
    ///     A divisor array when every entry is valid; otherwise a data-validation notification that
    ///     preserves the invalid edit in the field.
    /// </returns>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text)) return Invalid("Enter at least one beat divisor.");

        string[] entries = text.Split(',', StringSplitOptions.TrimEntries);
        var divisors = new IBeatDivisor[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            string entry = entries[index];
            string[] fraction = entry.Split('/', StringSplitOptions.TrimEntries);
            if (fraction.Length == 2
                && int.TryParse(fraction[0], NumberStyles.None, CultureInfo.InvariantCulture, out int numerator)
                && int.TryParse(fraction[1], NumberStyles.None, CultureInfo.InvariantCulture, out int denominator)
                && numerator > 0
                && denominator > 0)
            {
                divisors[index] = new RationalBeatDivisor(numerator, denominator);
                continue;
            }

            if (!TypeConverters.TryParseDouble(entry, out double number)) return Invalid($"Beat divisor '{entry}' is not a valid fraction or number.");

            if (!double.IsFinite(number) || number <= 0) return Invalid("Beat divisor must be greater than zero.");

            divisors[index] = new IrrationalBeatDivisor(number);
        }

        return divisors;
    }

    private static string Format(IBeatDivisor divisor)
    {
        return divisor switch
        {
            RationalBeatDivisor rational => FormattableString.Invariant(
                $"{rational.Numerator}/{rational.Denominator}"),
            IrrationalBeatDivisor irrational => irrational.GetValue().ToString(CultureInfo.InvariantCulture),
            _ => divisor.ToString() ?? string.Empty,
        };
    }

    private static BindingNotification Invalid(string message)
    {
        return new BindingNotification(
            new FormatException(message),
            BindingErrorType.DataValidationError);
    }
}
