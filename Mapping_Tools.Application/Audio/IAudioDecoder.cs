using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Provides fully decoded audio without leaking decoder-owned resources.</summary>
public interface IAudioDecoder
{
    /// <summary>Decodes a supported audio file into an owned floating-point clip.</summary>
    /// <param name="request">The source file request.</param>
    /// <param name="cancellationToken">Token checked while reading frames.</param>
    /// <returns>The decoded clip.</returns>
    Task<AudioClip> DecodeAsync(AudioDecodeRequest request, CancellationToken cancellationToken = default);
}

