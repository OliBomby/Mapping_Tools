using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.TimingCopier;

/// <summary>Provides the discoverable metadata for Timing Copier.</summary>
public static class TimingCopierToolDefinition
{
    /// <summary>Gets the stable Timing Copier metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "timing-copier",
        "Timing Copier",
        "Copy timing between beatmaps with optional object resnapping.",
        ["timing", "copy", "resnap", "beat divisors", "multi-map"],
        translatedDescriptionGetter: static () => ApplicationStrings.TimingCopier_Description,
        translatedSearchTermsGetter: static () => ApplicationStrings.TimingCopier_SearchTerms);
}
