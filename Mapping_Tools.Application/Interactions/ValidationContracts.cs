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