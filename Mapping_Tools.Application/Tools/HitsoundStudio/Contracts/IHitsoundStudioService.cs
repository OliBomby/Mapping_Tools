using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;

/// <summary>Provides the feature operations required by the Hitsound Studio presentation.</summary>
public interface IHitsoundStudioService
{
    /// <summary>Imports layers from a selected source while preserving its reload metadata.</summary>
    /// <param name="request">The source, filters, and layer settings to import.</param>
    /// <param name="cancellationToken">Stops source parsing before the next layer is created.</param>
    Task<IReadOnlyList<HitsoundLayer>> ImportAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Reimports every selected layer grouped by compatible source metadata.</summary>
    /// <param name="layers">The selected layers whose import metadata is re-run in place.</param>
    /// <param name="cancellationToken">Stops reloading between source groups.</param>
    Task<IReadOnlyList<HitsoundLayer>> ReloadAsync(
        IReadOnlyList<HitsoundLayer> layers,
        CancellationToken cancellationToken = default);

    /// <summary>Validates source paths and SoundFont notes without leaking decoder types.</summary>
    /// <param name="samples">The distinct source specifications to validate.</param>
    /// <param name="cancellationToken">Stops validation before the next source is decoded.</param>
    Task<IReadOnlyDictionary<SampleGeneratingArgs, Exception>> ValidateSamplesAsync(
        IReadOnlyList<SampleGeneratingArgs> samples,
        CancellationToken cancellationToken = default);

    /// <summary>Previews one generated source and returns its owned playback session.</summary>
    /// <param name="sample">The source and SoundFont parameters to render.</param>
    /// <param name="cancellationToken">Stops generation or playback startup.</param>
    Task<IAudioPlaybackSession> PreviewAsync(
        SampleGeneratingArgs sample,
        CancellationToken cancellationToken = default);

    /// <summary>Builds and writes the requested map/package with cooperative cancellation.</summary>
    /// <param name="project">An independent export snapshot and its output options.</param>
    /// <param name="progress">Receives monotonically increasing normalized major-phase progress.</param>
    /// <param name="cancellationToken">Stops generation, encoding, or writing at the next safe boundary.</param>
    Task<HitsoundStudioExportResult> ExportAsync(
        HitsoundStudioProject project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
