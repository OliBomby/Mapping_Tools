using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Audio.Effects;

namespace Mapping_Tools.Application.Audio.Contracts;

/// <summary>Applies the registered audio effects to an owned clip.</summary>
public interface IAudioEffectService
{
    /// <summary>Processes a clip without mutating the source.</summary>
    /// <param name="source">The source clip.</param>
    /// <param name="effects">The ordered effect descriptions.</param>
    /// <param name="cancellationToken">Token checked while processing samples.</param>
    /// <returns>A new processed clip.</returns>
    AudioClip Apply(
        AudioClip source,
        IEnumerable<AudioEffect> effects,
        CancellationToken cancellationToken = default);
}

