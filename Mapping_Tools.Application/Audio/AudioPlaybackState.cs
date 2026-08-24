using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Reports the lifecycle state of an audio preview session.</summary>
public enum AudioPlaybackState
{
    /// <summary>The session has not started or has been stopped.</summary>
    Stopped,

    /// <summary>The output device is currently playing.</summary>
    Playing,

    /// <summary>The output device is paused and can resume.</summary>
    Paused,

    /// <summary>The output device reported an unrecoverable failure.</summary>
    Failed,
}

