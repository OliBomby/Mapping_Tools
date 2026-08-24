using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Describes a request to play an owned clip.</summary>
public sealed class AudioPlaybackOptions
{
    /// <summary>Gets or sets whether playback should repeat until stopped.</summary>
    public bool Loop { get; set; }
}

