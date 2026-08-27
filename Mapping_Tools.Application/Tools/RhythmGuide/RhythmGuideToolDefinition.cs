namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Provides the discoverable metadata for Rhythm Guide.</summary>
public static class RhythmGuideToolDefinition
{
    /// <summary>Gets the stable Rhythm Guide metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "rhythm-guide",
        "Rhythm Guide",
        "Make a beatmap with circles from the rhythm of multiple maps.",
        ["rhythm", "hitsound", "guide", "reference"]);
}
