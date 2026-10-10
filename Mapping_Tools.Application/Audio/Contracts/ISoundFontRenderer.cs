using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;

namespace Mapping_Tools.Application.Audio.Contracts;

/// <summary>Renders selected SoundFont notes without exposing audio-library SoundFont types.</summary>
public interface ISoundFontRenderer
{
    /// <summary>Renders a SoundFont note into owned floating-point samples.</summary>
    /// <param name="request">The SoundFont note request.</param>
    /// <param name="cancellationToken">Token checked during rendering.</param>
    /// <returns>The generated clip, or <see langword="null" /> when no zone matches.</returns>
    Task<AudioClip?> RenderAsync(SoundFontNoteRequest request, CancellationToken cancellationToken = default);
}
