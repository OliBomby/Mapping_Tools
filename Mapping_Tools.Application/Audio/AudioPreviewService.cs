using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Application.Audio;

/// <summary>Coordinates source decoding/generation with deterministic playback sessions.</summary>
public sealed class AudioPreviewService : ISpectrumService
{
    private readonly IAudioDecoder _decoder;
    private readonly IAudioGenerator _generator;
    private readonly IAudioPlaybackService _playback;
    private readonly ISpectrumCalculator _spectrum;

    /// <summary>Creates the reusable preview and spectrum orchestration service.</summary>
    /// <param name="decoder">The decoder port.</param>
    /// <param name="generator">The generation port.</param>
    /// <param name="playback">The playback port.</param>
    /// <param name="spectrum">The spectrum calculation port.</param>
    public AudioPreviewService(
        IAudioDecoder decoder,
        IAudioGenerator generator,
        IAudioPlaybackService playback,
        ISpectrumCalculator spectrum)
    {
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _playback = playback ?? throw new ArgumentNullException(nameof(playback));
        _spectrum = spectrum ?? throw new ArgumentNullException(nameof(spectrum));
    }

    /// <inheritdoc />
    public async Task<SpectrumFrame> CalculateFileAsync(
        AudioDecodeRequest request,
        SpectrumCalculationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var clip = await _decoder.DecodeAsync(request, cancellationToken).ConfigureAwait(false);
        return await _spectrum.CalculateAsync(clip, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Decodes a source file and starts playback owned by the returned session.</summary>
    /// <param name="request">The source file request.</param>
    /// <param name="options">Playback settings.</param>
    /// <param name="cancellationToken">Token shared by decoding and device startup.</param>
    /// <returns>The disposable playback session.</returns>
    public async Task<IAudioPlaybackSession> PreviewFileAsync(
        AudioDecodeRequest request,
        AudioPlaybackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var clip = await _decoder.DecodeAsync(request, cancellationToken).ConfigureAwait(false);
        return await _playback.PlayAsync(clip, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Generates a sample and starts playback owned by the returned session.</summary>
    /// <param name="request">The source and transformation request.</param>
    /// <param name="options">Playback settings.</param>
    /// <param name="cancellationToken">Token shared by generation and device startup.</param>
    /// <returns>The disposable playback session.</returns>
    public async Task<IAudioPlaybackSession> PreviewGeneratedAsync(
        AudioGenerationRequest request,
        AudioPlaybackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var clip = await _generator.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
        return await _playback.PlayAsync(clip, options, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Coordinates sample generation with output encoding.</summary>
public sealed class AudioExportService
{
    private readonly IAudioExporter _exporter;
    private readonly IAudioGenerator _generator;

    /// <summary>Creates the generated-sample export service.</summary>
    /// <param name="generator">The sample-generation port.</param>
    /// <param name="exporter">The file-encoding port.</param>
    public AudioExportService(IAudioGenerator generator, IAudioExporter exporter)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
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
        var clip = await _generator.GenerateAsync(generation, cancellationToken).ConfigureAwait(false);
        return await _exporter.ExportAsync(clip, export, cancellationToken).ConfigureAwait(false);
    }
}
