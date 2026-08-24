namespace Mapping_Tools.Application.Audio.Models;

/// <summary>Describes a request to play an owned clip.</summary>
public sealed class AudioPlaybackOptions
{
    /// <summary>Gets or sets whether playback should repeat until stopped.</summary>
    public bool Loop { get; set; }
}

