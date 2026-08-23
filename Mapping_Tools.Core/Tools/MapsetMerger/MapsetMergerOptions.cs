namespace Mapping_Tools.Core.Tools.MapsetMerger;

/// <summary>
///     Describes the non-visual option that changes how storyboard content is
///     represented in merged beatmaps.
/// </summary>
public class MapsetMergerOptions
{
    /// <summary>
    ///     Gets or sets whether the first external storyboard is copied into every
    ///     beatmap instead of being emitted as a separate <c>.osb</c> file.
    /// </summary>
    public bool MoveSbToBeatmap { get; set; }
}

/// <summary>
///     Identifies one source mapset and the name used for its exported assets.
/// </summary>
public sealed class MapsetMergerInput
{
    /// <summary>Creates a mapset input.</summary>
    /// <param name="name">The output folder and reference prefix.</param>
    /// <param name="path">The source mapset directory.</param>
    public MapsetMergerInput(string name, string path)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>Gets or sets the output-safe mapset name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the source mapset directory.</summary>
    public string Path { get; set; }
}

/// <summary>
///     Collects the source assets referenced by one rewritten mapset.
/// </summary>
public sealed class MapsetMergerReferences
{
    /// <summary>Gets custom hitsound names, normally extensionless.</summary>
    public HashSet<string> HitSoundFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets explicitly named audio and storyboard sample files.</summary>
    public HashSet<string> OtherAudioFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets background, sprite, and animation image names.</summary>
    public HashSet<string> ImageFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets explicitly named video files.</summary>
    public HashSet<string> VideoFiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Merges another reference set into this one.</summary>
    /// <param name="other">The references to add.</param>
    public void Add(MapsetMergerReferences other)
    {
        ArgumentNullException.ThrowIfNull(other);
        HitSoundFiles.UnionWith(other.HitSoundFiles);
        OtherAudioFiles.UnionWith(other.OtherAudioFiles);
        ImageFiles.UnionWith(other.ImageFiles);
        VideoFiles.UnionWith(other.VideoFiles);
    }
}
