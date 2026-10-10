using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Audio;

[TestClass]
public sealed class AudioExportServiceTests
{
    [TestMethod]
    public async Task ExportGeneratedAsync_ExportsTheGeneratedClip()
    {
        // Arrange
        var clip = new AudioClip(new AudioFormat(8000, 1), [0.1f]);
        var exporter = new StubExporter();
        var service = new AudioExportService(new StubGenerator(clip), exporter);
        var generation = new AudioGenerationRequest(new SampleGeneratingArgs("sample.wav"));
        var export = new AudioExportRequest("out.wav", AudioExportFormat.WaveIeeeFloat);

        // Act
        var result = await service.ExportGeneratedAsync(generation, export);

        // Assert
        result.Should().BeSameAs(exporter.Result);
        exporter.LastClip.Should().BeSameAs(clip);
        exporter.LastRequest.Should().BeSameAs(export);
    }

    private sealed class StubGenerator(AudioClip clip) : IAudioGenerator
    {
        public Task<AudioClip> GenerateAsync(
            AudioGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(clip);
        }
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
