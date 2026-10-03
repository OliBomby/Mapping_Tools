using System.ComponentModel.DataAnnotations;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Validation;

/// <summary>
///     Rejects text containing characters outside the seven-bit ASCII range.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class AsciiOnlyAttribute : ValidationAttribute
{
    /// <summary>Creates an ASCII-only validation rule.</summary>
    public AsciiOnlyAttribute()
        : base(() => DesktopStrings.Shell_UseOnlyAsciiCharacters)
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        return value is not string text || text.All(character => character <= 0x7F);
    }
}
