namespace Mapping_Tools.Domain.Beatmaps.Sections;

/// <summary>
/// Contains all the values in the [Metadata] section of a .osu file.
/// </summary>
public class SectionMetadata {
    public string Artist { get; set; } = string.Empty;
    public string? ArtistUnicode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleUnicode { get; set; }
    public string Creator { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int BeatmapId { get; set; } = 0;
    public int BeatmapSetId { get; set; } = -1;
}