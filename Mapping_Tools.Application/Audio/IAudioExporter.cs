using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Writes generated clips to supported audio file formats.</summary>
public interface IAudioExporter
{
    /// <summary>Exports a clip and returns only after the destination is closed.</summary>
    /// <param name="clip">The clip to encode.</param>
    /// <param name="request">The output request.</param>
    /// <param name="cancellationToken">Token checked while encoding.</param>
    /// <returns>The completed export result.</returns>
    Task<AudioExportResult> ExportAsync(
        AudioClip clip,
        AudioExportRequest request,
        CancellationToken cancellationToken = default);
}

