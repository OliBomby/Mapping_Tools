namespace Mapping_Tools.Core.Tools.MapsetMerger.Models;

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
