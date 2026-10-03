using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Provides the discoverable metadata for Pattern Gallery.</summary>
public static class PatternGalleryToolDefinition
{
    /// <summary>Gets the stable Pattern Gallery metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "pattern-gallery",
        "Pattern Gallery",
        "Collect, preview, organize, and place reusable hit-object patterns.",
        ["pattern", "gallery", "collection", "osu", "snippet"],
        QuickRunTargets.Always,
        static () => ApplicationStrings.PatternGallery_Description,
        static () => ApplicationStrings.PatternGallery_SearchTerms);
}
