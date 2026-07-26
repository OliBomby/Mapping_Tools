using System.Globalization;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
/// Reports whether a form value is acceptable and, when it is not, supplies
/// text suitable for display beside the field.
/// </summary>
/// <param name="IsValid">Whether the value may be submitted.</param>
/// <param name="ErrorMessage">The user-facing reason for rejection, or <see langword="null"/> for a valid value.</param>
public readonly record struct ValidationOutcome(bool IsValid, string? ErrorMessage)
{
    /// <summary>
    /// Gets the shared successful validation outcome.
    /// </summary>
    public static ValidationOutcome Success { get; } = new(true, null);

    /// <summary>
    /// Creates a rejected outcome with a non-empty explanation.
    /// </summary>
    /// <param name="errorMessage">The actionable reason the value cannot be submitted.</param>
    /// <returns>A failed outcome carrying <paramref name="errorMessage"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the explanation is empty or whitespace.</exception>
    public static ValidationOutcome Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new ValidationOutcome(false, errorMessage);
    }
}

/// <summary>
/// Validates a parsed form value without depending on either desktop frontend.
/// </summary>
/// <typeparam name="T">The value type accepted by the rule.</typeparam>
public interface IValueValidator<in T>
{
    /// <summary>
    /// Checks one value and returns the first user-facing validation problem.
    /// </summary>
    /// <param name="value">The parsed value to inspect.</param>
    /// <returns>A successful outcome or a failed outcome explaining how to correct the value.</returns>
    ValidationOutcome Validate(T value);
}

/// <summary>
/// Converts between editable text and a typed form value.
/// </summary>
/// <typeparam name="T">The value type represented by the text.</typeparam>
public interface ITextValueConverter<T>
{
    /// <summary>
    /// Formats an existing value for editing without losing required precision.
    /// </summary>
    /// <param name="value">The value to place in a form field.</param>
    /// <returns>The editable text representation.</returns>
    string Format(T value);

    /// <summary>
    /// Attempts to parse text and supplies a correction message on failure.
    /// </summary>
    /// <param name="text">The current field text, which may be <see langword="null"/>.</param>
    /// <param name="value">The parsed value when conversion succeeds.</param>
    /// <param name="errorMessage">A user-facing format error when conversion fails.</param>
    /// <returns><see langword="true"/> only when <paramref name="value"/> is usable.</returns>
    bool TryConvert(string? text, out T value, out string? errorMessage);
}

/// <summary>
/// Adapts feature-specific validation logic to the common form contract.
/// </summary>
/// <typeparam name="T">The value type checked by the callback.</typeparam>
public sealed class DelegateValueValidator<T> : IValueValidator<T>
{
    private readonly Func<T, ValidationOutcome> _validate;

    /// <summary>
    /// Creates a validator backed by a deterministic, side-effect-free callback.
    /// </summary>
    /// <param name="validate">The callback that evaluates each submitted value.</param>
    public DelegateValueValidator(Func<T, ValidationOutcome> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);
        _validate = validate;
    }

    /// <inheritdoc/>
    public ValidationOutcome Validate(T value) => _validate(value);
}

/// <summary>
/// Creates common reusable rules while leaving feature-specific checks explicit.
/// </summary>
public static class ValueValidators
{
    /// <summary>
    /// Creates a rule that rejects null, empty, or whitespace-only text.
    /// </summary>
    /// <param name="errorMessage">The correction shown for missing text.</param>
    /// <returns>A validator suitable for names, paths, and other required fields.</returns>
    public static IValueValidator<string> RequiredText(
        string errorMessage = "Field is required.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new DelegateValueValidator<string>(value =>
            string.IsNullOrWhiteSpace(value)
                ? ValidationOutcome.Failure(errorMessage)
                : ValidationOutcome.Success);
    }

    /// <summary>
    /// Creates an inclusive range rule for comparable numeric or ordered values.
    /// </summary>
    /// <typeparam name="T">A value type with a stable ordering.</typeparam>
    /// <param name="minimum">The smallest accepted value.</param>
    /// <param name="maximum">The largest accepted value.</param>
    /// <param name="errorMessage">The correction shown for an out-of-range value.</param>
    /// <returns>A validator accepting both supplied boundaries.</returns>
    /// <exception cref="ArgumentException">Thrown when the minimum is greater than the maximum.</exception>
    public static IValueValidator<T> InclusiveRange<T>(
        T minimum,
        T maximum,
        string errorMessage)
        where T : IComparable<T>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        if (minimum.CompareTo(maximum) > 0)
        {
            throw new ArgumentException(
                "The minimum cannot be greater than the maximum.",
                nameof(minimum));
        }

        return new DelegateValueValidator<T>(value =>
            value.CompareTo(minimum) >= 0 && value.CompareTo(maximum) <= 0
                ? ValidationOutcome.Success
                : ValidationOutcome.Failure(errorMessage));
    }
}

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
