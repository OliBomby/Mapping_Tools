namespace Mapping_Tools.Application.Tools.MapsetMerger;

/// <summary>Provides the discoverable metadata for Mapset Merger.</summary>
public static class MapsetMergerToolDefinition
{
    /// <summary>Gets the stable Mapset Merger metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "mapset-merger",
        "Mapset Merger",
        "Combine multiple mapsets and resolve beatmap, audio, image, storyboard, and sample conflicts.",
        ["mapset", "merge", "audio", "image", "storyboard", "samples", "conflicts"]);
}
