using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.MetadataManager;

/// <summary>Provides the discoverable metadata for Metadata Manager.</summary>
public static class MetadataManagerToolDefinition
{
    /// <summary>Gets the stable Metadata Manager metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "metadata-manager",
        "Metadata Manager",
        "Edit metadata once and apply it to multiple beatmaps.",
        ["metadata", "artist", "title", "tags", "colours"],
        translatedDescriptionGetter: static () => ApplicationStrings.MetadataManager_Description,
        translatedSearchTermsGetter: static () => ApplicationStrings.MetadataManager_SearchTerms);
}
