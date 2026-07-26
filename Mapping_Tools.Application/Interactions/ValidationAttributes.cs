using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Mapping_Tools.Application.Interactions;

/// <summary>
/// Rejects missing, empty, or whitespace-only text while preserving the
/// standard DataAnnotations validation pipeline.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class RequiredTextAttribute : ValidationAttribute
{
    /// <inheritdoc/>
    public override bool IsValid(object? value) =>
        value is string text && !string.IsNullOrWhiteSpace(text);
}

/// <summary>
/// Requires a duration to meet a constant-format inclusive lower bound.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class MinimumTimeSpanAttribute : ValidationAttribute
{
    /// <summary>
    /// Creates a lower-bound rule from the invariant constant TimeSpan format.
    /// </summary>
    /// <param name="minimum">
    /// The inclusive minimum in the <c>[-][d.]hh:mm:ss[.fffffff]</c> format.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="minimum"/> is not a constant-format duration.
    /// </exception>
    public MinimumTimeSpanAttribute(string minimum)
    {
        if (!TimeSpan.TryParseExact(
                minimum,
                "c",
                CultureInfo.InvariantCulture,
                out TimeSpan parsedMinimum))
        {
            throw new ArgumentException(
                "The minimum must use the invariant constant TimeSpan format.",
                nameof(minimum));
        }

        Minimum = parsedMinimum;
    }

    /// <summary>
    /// Gets the inclusive lower bound applied by this rule.
    /// </summary>
    public TimeSpan Minimum { get; }

    /// <inheritdoc/>
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is TimeSpan duration && duration >= Minimum)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            validationContext.MemberName is null
                ? null
                : [validationContext.MemberName]);
    }
}
