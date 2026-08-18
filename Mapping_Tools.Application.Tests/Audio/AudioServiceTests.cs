using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Spectrum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Audio;

[TestClass]
public sealed class AudioServiceTests
{
    [TestMethod]
    public async Task PreviewGeneratedAsync_PassesGeneratedClipToPlayback()
    {
        // Arrange
        var clip = new AudioClip(new AudioFormat(8000, 1), [0.1f, 0.2f]);
        var generator = new StubGenerator(clip);
        var playback = new StubPlaybackService();
        var service = new AudioPreviewService(
            new StubDecoder(clip),
            generator,
            playback,
            new StubSpectrumCalculator());
        var request = new AudioGenerationRequest(new SampleGeneratingArgs("sample.wav"));

        // Act
        IAudioPlaybackSession result = await service.PreviewGeneratedAsync(
            request,
            new AudioPlaybackOptions { Loop = true });

        // Assert
        result.Should().BeSameAs(playback.Session);
        generator.LastRequest.Should().BeSameAs(request);
        playback.LastClip.Should().BeSameAs(clip);
        playback.LastOptions!.Loop.Should().BeTrue();
    }

    [TestMethod]
    public async Task CalculateFileAsync_DelegatesDecodedClipAndOptionsToSpectrum()
    {
        // Arrange
        var clip = new AudioClip(new AudioFormat(8000, 1), [0.1f, 0.2f]);
        var decoder = new StubDecoder(clip);
        var spectrum = new StubSpectrumCalculator();
        var service = new AudioPreviewService(
            decoder,
            new StubGenerator(clip),
            new StubPlaybackService(),
            spectrum);
        var options = new SpectrumCalculationOptions { FftSize = 8, StartFrame = 1, FrameCount = 2 };
        var request = new AudioDecodeRequest("sample.wav");

        // Act
        SpectrumFrame result = await service.CalculateFileAsync(request, options);

        // Assert
        result.Should().BeSameAs(spectrum.Result);
        decoder.LastRequest.Should().BeSameAs(request);
        spectrum.LastClip.Should().BeSameAs(clip);
        spectrum.LastOptions.Should().BeSameAs(options);
    }

    [TestMethod]
    public async Task AudioExportService_ExportsTheGeneratedClip()
    {
        // Arrange
        var clip = new AudioClip(new AudioFormat(8000, 1), [0.1f]);
        var exporter = new StubExporter();
        var service = new AudioExportService(new StubGenerator(clip), exporter);
        var generation = new AudioGenerationRequest(new SampleGeneratingArgs("sample.wav"));
        var export = new AudioExportRequest("out.wav", AudioExportFormat.WaveIeeeFloat);

        // Act
        AudioExportResult result = await service.ExportGeneratedAsync(generation, export);

        // Assert
        result.Should().BeSameAs(exporter.Result);
        exporter.LastClip.Should().BeSameAs(clip);
        exporter.LastRequest.Should().BeSameAs(export);
    }

    private sealed class StubDecoder(AudioClip clip) : IAudioDecoder
    {
        public AudioDecodeRequest? LastRequest { get; private set; }

        public Task<AudioClip> DecodeAsync(AudioDecodeRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(clip);
        }
    }

    private sealed class StubGenerator(AudioClip clip) : IAudioGenerator
    {
        public AudioGenerationRequest? LastRequest { get; private set; }

        public Task<AudioClip> GenerateAsync(AudioGenerationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(clip);
        }
    }

    private sealed class StubSpectrumCalculator : ISpectrumCalculator
    {
        public SpectrumFrame Result { get; } = new(8000, 8, [1]);
        public AudioClip? LastClip { get; private set; }
        public SpectrumCalculationOptions? LastOptions { get; private set; }

        public Task<SpectrumFrame> CalculateAsync(
            AudioClip clip,
            SpectrumCalculationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastClip = clip;
            LastOptions = options;
            return Task.FromResult(Result);
        }
    }

    private sealed class StubPlaybackService : IAudioPlaybackService
    {
        public StubPlaybackSession Session { get; } = new();
        public AudioClip? LastClip { get; private set; }
        public AudioPlaybackOptions? LastOptions { get; private set; }

        public Task<IAudioPlaybackSession> PlayAsync(
            AudioClip clip,
            AudioPlaybackOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastClip = clip;
            LastOptions = options;
            return Task.FromResult<IAudioPlaybackSession>(Session);
        }
    }

    private sealed class StubPlaybackSession : IAudioPlaybackSession
    {
        public AudioPlaybackState State => AudioPlaybackState.Playing;
        public TimeSpan Position => TimeSpan.Zero;
        public Task Completion => Task.CompletedTask;
        public void Pause() { }
        public void Resume() { }
        public ValueTask StopAsync() => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class StubExporter : IAudioExporter
    {
        public AudioExportResult Result { get; } = new("out.wav", AudioExportFormat.WaveIeeeFloat, 4);
        public AudioClip? LastClip { get; private set; }
        public AudioExportRequest? LastRequest { get; private set; }

        public Task<AudioExportResult> ExportAsync(
            AudioClip clip,
            AudioExportRequest request,
            CancellationToken cancellationToken = default)
        {
            LastClip = clip;
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }
}
