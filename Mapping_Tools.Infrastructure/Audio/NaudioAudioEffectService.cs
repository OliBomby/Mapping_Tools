using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Audio.Effects;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Adapts the framework-neutral effect engine to the Application audio port.</summary>
public sealed class NaudioAudioEffectService : IAudioEffectService
{
    /// <inheritdoc />
    public AudioClip Apply(
        AudioClip source,
        IEnumerable<AudioEffect> effects,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(effects);
        return AudioEffectEngine.Apply(source, effects, cancellationToken);
    }
}
