using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

/// <summary>Provides the discoverable metadata for Hitsound Copier.</summary>
public static class HitsoundCopierToolDefinition
{
    /// <summary>Gets the stable Hitsound Copier metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "hitsound-copier",
        "Hitsound Copier",
        "Copy hitsounds, samples, and storyboard sounds between beatmaps.",
        ["hitsound", "copy", "sample", "storyboard", "mute", "multi-map"],
        QuickRunTargets.Always);
}
