using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Creates deterministic playback sessions for fully owned clips.</summary>
public interface IAudioPlaybackService
{
    /// <summary>Starts playback and transfers resource ownership to the returned session.</summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="options">Playback settings.</param>
    /// <param name="cancellationToken">Token checked before opening the device.</param>
    /// <returns>The disposable playback session.</returns>
    Task<IAudioPlaybackSession> PlayAsync(
        AudioClip clip,
        AudioPlaybackOptions? options = null,
        CancellationToken cancellationToken = default);
}

