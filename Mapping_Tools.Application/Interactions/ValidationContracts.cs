using System.ComponentModel.DataAnnotations;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
/// Creates reusable DataAnnotations rules for value-dialog requests.
/// </summary>
public static class ValueValidators
{
    /// <summary>
    /// Creates a rule that rejects null, empty, or whitespace-only text.
    /// </summary>
    /// <param name="errorMessage">The correction shown for missing text.</param>
    /// <returns>A standard annotation suitable for names, paths, and other required fields.</returns>
    public static ValidationAttribute RequiredText(
        string errorMessage = "Field is required.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new RequiredAttribute
        {
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates an inclusive range rule for comparable numeric or ordered values.
    /// </summary>
    /// <typeparam name="T">A value type with a stable ordering.</typeparam>
    /// <param name="minimum">The smallest accepted value.</param>
    /// <param name="maximum">The largest accepted value.</param>
    /// <param name="errorMessage">The correction shown for an out-of-range value.</param>
    /// <returns>A standard annotation accepting both supplied boundaries.</returns>
    /// <exception cref="ArgumentException">Thrown when the minimum is greater than the maximum.</exception>
    public static ValidationAttribute InclusiveRange<T>(
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

        return new DelegateValidationAttribute(
            value => value is T typedValue
                     && typedValue.CompareTo(minimum) >= 0
                     && typedValue.CompareTo(maximum) <= 0,
            errorMessage);
    }

    private sealed class DelegateValidationAttribute : ValidationAttribute
    {
        private readonly Func<object?, bool> _isValid;

        public DelegateValidationAttribute(
            Func<object?, bool> isValid,
            string errorMessage)
        {
            _isValid = isValid;
            ErrorMessage = errorMessage;
        }

        public override bool IsValid(object? value) => _isValid(value);
    }
}
