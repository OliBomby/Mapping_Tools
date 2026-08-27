using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>Provides the discoverable metadata for Hitsound Studio.</summary>
public static class HitsoundStudioToolDefinition
{
    /// <summary>Gets the stable Hitsound Studio metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "hitsound-studio",
        "Hitsound Studio",
        "Import, edit, preview, generate, and export hitsound layers.",
        ["hitsound", "studio", "sample", "MIDI", "SoundFont", "export", "layer"],
        QuickRunTargets.Always);
}
