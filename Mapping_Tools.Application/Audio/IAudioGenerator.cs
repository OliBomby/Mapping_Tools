using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Generates a complete hitsound clip from a source file or SoundFont note.</summary>
public interface IAudioGenerator
{
    /// <summary>Generates samples and disposes all source resources before completing.</summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Token checked during source rendering.</param>
    /// <returns>An owned generated clip.</returns>
    Task<AudioClip> GenerateAsync(AudioGenerationRequest request, CancellationToken cancellationToken = default);
}

