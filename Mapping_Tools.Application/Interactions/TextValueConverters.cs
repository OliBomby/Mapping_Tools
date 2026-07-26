using System.Globalization;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
/// Provides culture-stable converters used by shared numeric and text forms.
/// </summary>
public static class TextValueConverters
{
    /// <summary>
    /// Gets a converter that preserves text exactly, treating <see langword="null"/> as an empty field.
    /// </summary>
    public static ITextValueConverter<string> String { get; } = new StringTextValueConverter();

    /// <summary>
    /// Gets a converter for invariant-culture floating-point values, including exponent notation.
    /// </summary>
    public static ITextValueConverter<double> InvariantDouble { get; } = new InvariantDoubleTextValueConverter();

    /// <summary>
    /// Gets a converter for invariant-culture signed 32-bit integers.
    /// </summary>
    public static ITextValueConverter<int> InvariantInt32 { get; } = new InvariantInt32TextValueConverter();

    /// <summary>
    /// Gets a converter for durations in the invariant constant TimeSpan format.
    /// </summary>
    public static ITextValueConverter<TimeSpan> ConstantTimeSpan { get; } = new ConstantTimeSpanTextValueConverter();

    private sealed class StringTextValueConverter : ITextValueConverter<string>
    {
        public string Format(string value) => value ?? string.Empty;

        public bool TryConvert(string? text, out string value, out string? errorMessage)
        {
            value = text ?? string.Empty;
            errorMessage = null;
            return true;
        }
    }

    private sealed class InvariantDoubleTextValueConverter : ITextValueConverter<double>
    {
        public string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        public bool TryConvert(string? text, out double value, out string? errorMessage)
        {
            bool converted = double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
            errorMessage = converted ? null : "Enter a valid number using a period as the decimal separator.";
            return converted;
        }
    }

    private sealed class InvariantInt32TextValueConverter : ITextValueConverter<int>
    {
        public string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

        public bool TryConvert(string? text, out int value, out string? errorMessage)
        {
            bool converted = int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
            errorMessage = converted ? null : "Enter a whole number.";
            return converted;
        }
    }

    private sealed class ConstantTimeSpanTextValueConverter : ITextValueConverter<TimeSpan>
    {
        public string Format(TimeSpan value) =>
            value.ToString("c", CultureInfo.InvariantCulture);

        public bool TryConvert(
            string? text,
            out TimeSpan value,
            out string? errorMessage)
        {
            bool converted = TimeSpan.TryParseExact(
                text,
                "c",
                CultureInfo.InvariantCulture,
                out value);
            errorMessage = converted
                ? null
                : "Use the format hh:mm:ss, for example 00:10:00.";
            return converted;
        }
    }
}