using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.HitsoundPreviewHelper;

/// <summary>Provides the discoverable metadata for Hitsound Preview Helper.</summary>
public static class HitsoundPreviewHelperToolDefinition
{
    /// <summary>Gets the stable Hitsound Preview Helper metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "hitsound-preview-helper",
        "Hitsound Preview Helper",
        "Place provisional hitsounds from positional zones.",
        ["hitsound", "preview", "zone", "sample", "position"],
        QuickRunTargets.Always);
}
