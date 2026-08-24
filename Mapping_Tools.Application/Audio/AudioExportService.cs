using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Application.Audio;

/// <summary>Coordinates sample generation with output encoding.</summary>
public sealed class AudioExportService
{
    private readonly IAudioExporter exporter;
    private readonly IAudioGenerator generator;

    /// <summary>Creates the generated-sample export service.</summary>
    /// <param name="generator">The sample-generation port.</param>
    /// <param name="exporter">The file-encoding port.</param>
    public AudioExportService(IAudioGenerator generator, IAudioExporter exporter)
    {
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
    }

    /// <summary>Generates a sample and exports it after all source resources are closed.</summary>
    /// <param name="generation">The sample-generation request.</param>
    /// <param name="export">The destination encoding request.</param>
    /// <param name="cancellationToken">Token shared by generation and encoding.</param>
    /// <returns>The completed export result.</returns>
    public async Task<AudioExportResult> ExportGeneratedAsync(
        AudioGenerationRequest generation,
        AudioExportRequest export,
        CancellationToken cancellationToken = default)
    {
        var clip = await generator.GenerateAsync(generation, cancellationToken).ConfigureAwait(false);
        return await exporter.ExportAsync(clip, export, cancellationToken).ConfigureAwait(false);
    }
}
