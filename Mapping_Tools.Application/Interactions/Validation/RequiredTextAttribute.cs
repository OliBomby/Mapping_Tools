using System.ComponentModel.DataAnnotations;

namespace Mapping_Tools.Application.Interactions.Validation;

/// <summary>
/// Rejects null, empty, and whitespace-only text while allowing any non-whitespace content.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class RequiredTextAttribute : ValidationAttribute
{
    /// <summary>
    /// Creates a required-text rule with a correction suitable for a general form field.
    /// </summary>
    public RequiredTextAttribute()
        : base("Field is required.")
    {
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value) =>
        value is string text && !string.IsNullOrWhiteSpace(text);
}
