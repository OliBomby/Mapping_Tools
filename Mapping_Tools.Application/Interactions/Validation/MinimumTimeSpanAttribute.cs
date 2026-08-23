using System.ComponentModel.DataAnnotations;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Application.Interactions.Validation;

/// <summary>
///     Requires a duration to meet an inclusive lower bound.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MinimumTimeSpanAttribute : ValidationAttribute
{
    /// <summary>
    ///     Creates a lower-bound rule from a constant-format duration or millisecond expression.
    /// </summary>
    /// <param name="minimum">
    ///     The inclusive minimum as <c>[-][d.]hh:mm:ss[.fffffff]</c> text or an arithmetic expression in milliseconds.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="minimum" /> is not a supported duration or millisecond expression.
    /// </exception>
    public MinimumTimeSpanAttribute(string minimum)
    {
        if (!TypeConverters.TryParseTimeSpan(minimum, out var parsedMinimum))
            throw new ArgumentException(
                "The minimum must be a constant-format duration or millisecond expression.",
                nameof(minimum));

        Minimum = parsedMinimum;
    }

    /// <summary>
    ///     Gets the inclusive lower bound applied by this rule.
    /// </summary>
    public TimeSpan Minimum { get; }

    /// <inheritdoc />
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;

        if (value is TimeSpan duration && duration >= Minimum) return ValidationResult.Success;

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            validationContext.MemberName is null
                ? null
                : [validationContext.MemberName]);
    }
}
