using System.ComponentModel;

namespace Mapping_Tools.Core.Tools.PatternGallery.Models;

/// <summary>
///     Stores the indexed metadata for one pattern file. The beatmap content stays
///     in the collection's Pattern Files directory and is addressed by
///     <see cref="FileName" />.
/// </summary>
public sealed class PatternGalleryPattern
{
    private string fileName = string.Empty;
    private string group = string.Empty;
    private string name = string.Empty;

    /// <summary>Gets or sets the display name shown below the thumbnail.</summary>
    [DisplayName("Name")]
    public string Name { get => name; set => name = value ?? string.Empty; }

    /// <summary>Gets or sets the optional group name used by the gallery.</summary>
    public string Group { get => group; set => group = value ?? string.Empty; }

    /// <summary>Gets or sets the local time at which the pattern was indexed.</summary>
    public DateTime CreationTime { get; set; }

    /// <summary>Gets or sets the local time at which the pattern was last placed.</summary>
    public DateTime LastUsedTime { get; set; }

    /// <summary>Gets or sets the number of successful placements.</summary>
    public int UseCount { get; set; }

    /// <summary>Gets or sets the filename of the pattern's `.osu` document.</summary>
    public string FileName { get => fileName; set => fileName = value ?? string.Empty; }

    /// <summary>Gets or sets the number of hit objects indexed from the file.</summary>
    public int ObjectCount { get; set; }

    /// <summary>Gets or sets the pattern duration from first object to last end.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Gets or sets the average beat length reported by the pattern timing.</summary>
    public double BeatLength { get; set; }
}
