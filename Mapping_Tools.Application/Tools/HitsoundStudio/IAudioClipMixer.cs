using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>Mixes generated neutral audio clips without exposing a desktop audio library.</summary>
/// <param name="clips">The decoded clips to mix; all clips must contain audio data.</param>
/// <param name="cancellationToken">Stops normalization or mixing before returning a clip.</param>
public interface IAudioClipMixer
{
    /// <summary>Mixes clips after resampling and channel normalization.</summary>
    Task<AudioClip> MixAsync(
        IReadOnlyList<AudioClip> clips,
        CancellationToken cancellationToken = default);
}
