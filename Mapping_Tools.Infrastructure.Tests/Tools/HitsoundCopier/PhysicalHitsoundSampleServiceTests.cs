using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Infrastructure.Tools.HitsoundCopier;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Tools.HitsoundCopier;

[TestClass]
public sealed class PhysicalHitsoundSampleServiceTests
{
    [TestMethod]
    public void TryCreateAssignment_WithNestedSourcePathAndExistingSchema_PreservesPathAndAllocatesNextIndex()
    {
        // Arrange
        using TestDirectory directory = new();
        string firstPath = Path.Combine(directory.Root, "samples", "kick.wav");
        string secondPath = Path.Combine(directory.Root, "samples", "snare.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(firstPath)!);
        File.WriteAllBytes(firstPath, [1]);
        File.WriteAllBytes(secondPath, [2]);
        PhysicalHitsoundSampleService service = CreateService(directory.Root);
        SampleSchema schema = new();
        IReadOnlyDictionary<string, string> firstSamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(directory.Root, "samples", "kick")] = firstPath,
            [Path.Combine(directory.Root, "samples", "snare")] = secondPath,
        };

        // Act
        var first = service.TryCreateAssignment(
            directory.Root,
            ["samples/kick.wav"],
            firstSamples,
            "slidertick",
            SampleSet.Normal,
            100,
            schema);
        var second = service.TryCreateAssignment(
            directory.Root,
            ["samples/snare.wav"],
            firstSamples,
            "slidertick",
            SampleSet.Normal,
            100,
            schema);

        // Assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.Index.Should().Be(100);
        second!.Index.Should().Be(101);
        schema.Should().ContainKey("normal-slidertick100");
        schema.Should().ContainKey("normal-slidertick101");
        schema["normal-slidertick100"].Should().ContainSingle().Which.Path.Should().Be(firstPath);
        schema["normal-slidertick101"].Should().ContainSingle().Which.Path.Should().Be(secondPath);
    }

    [TestMethod]
    public async Task ExportAsync_WithMixedSchema_ExportsFloatWaveformThroughAudioPipeline()
    {
        // Arrange
        using TestDirectory directory = new();
        string firstPath = Path.Combine(directory.Root, "kick.wav");
        string secondPath = Path.Combine(directory.Root, "snare.wav");
        File.WriteAllBytes(firstPath, [1]);
        File.WriteAllBytes(secondPath, [2]);
        RecordingAudioGenerator generator = new();
        RecordingAudioClipMixer mixer = new();
        RecordingAudioExporter exporter = new();
        PhysicalHitsoundSampleService service = CreateService(
            directory.Root,
            generator,
            exporter,
            mixer);
        SampleSchema schema = new()
        {
            ["normal-slidertick100"] =
            [new SampleGeneratingArgs(firstPath), new SampleGeneratingArgs(secondPath)]
        };

        // Act
        await service.ExportAsync(schema);

        // Assert
        generator.Requests.Should().HaveCount(2);
        mixer.Input.Should().HaveCount(2);
        exporter.Clip.Should().BeSameAs(mixer.Result);
        exporter.Request.Should().NotBeNull();
        exporter.Request!.Path.Should().Be(
            Path.Combine(directory.Root, "Mapping Tools", "Exports", "normal-slidertick100.wav"));
        exporter.Request.Format.Should().Be(AudioExportFormat.WaveIeeeFloat);
    }

    [TestMethod]
    public async Task ExportAsync_WithUnmodifiedSourceAndStaleExport_CopiesSourceAndClearsStaleFiles()
    {
        // Arrange
        using TestDirectory directory = new();
        string sourcePath = Path.Combine(directory.Root, "source.wav");
        string stalePath = Path.Combine(
            directory.Root,
            "Mapping Tools",
            "Exports",
            "stale.wav");
        byte[] sourceBytes = [1, 2, 3];
        File.WriteAllBytes(sourcePath, sourceBytes);
        Directory.CreateDirectory(Path.GetDirectoryName(stalePath)!);
        File.WriteAllBytes(stalePath, [9]);
        RecordingAudioExporter exporter = new();
        PhysicalHitsoundSampleService service = CreateService(directory.Root, exporter: exporter);
        SampleSchema schema = new()
        {
            ["normal-slidertick100"] = [new SampleGeneratingArgs(sourcePath)]
        };

        // Act
        await service.ExportAsync(schema);

        // Assert
        string destination = Path.Combine(
            directory.Root,
            "Mapping Tools",
            "Exports",
            "normal-slidertick100.wav");
        File.ReadAllBytes(destination).Should().Equal(sourceBytes);
        File.Exists(stalePath).Should().BeFalse();
        exporter.Request.Should().BeNull();
    }

    private static PhysicalHitsoundSampleService CreateService(
        string localApplicationData,
        IAudioGenerator? generator = null,
        IAudioExporter? exporter = null,
        IAudioClipMixer? mixer = null)
    {
        return new PhysicalHitsoundSampleService(
            new StubMapCleanerSampleService(),
            new ApplicationDirectories(localApplicationData),
            generator ?? new RecordingAudioGenerator(),
            exporter ?? new RecordingAudioExporter(),
            mixer ?? new RecordingAudioClipMixer());
    }

    private sealed class StubMapCleanerSampleService : IMapCleanerSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory,
            bool detectDuplicates,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public Task<int> MoveUnusedToRecoveryAsync(
            string directory,
            string currentBeatmapPath,
            Beatmap currentBeatmap,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class RecordingAudioGenerator : IAudioGenerator
    {
        public List<AudioGenerationRequest> Requests { get; } = [];

        public Task<AudioClip> GenerateAsync(
            AudioGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(new AudioClip(new AudioFormat(8000, 1), [0.25f]));
        }
    }

    private sealed class RecordingAudioClipMixer : IAudioClipMixer
    {
        public IReadOnlyList<AudioClip>? Input { get; private set; }

        public AudioClip Result { get; } = new(new AudioFormat(8000, 1), [0.5f]);

        public Task<AudioClip> MixAsync(
            IReadOnlyList<AudioClip> clips,
            CancellationToken cancellationToken = default)
        {
            Input = clips;
            return Task.FromResult(Result);
        }
    }

    private sealed class RecordingAudioExporter : IAudioExporter
    {
        public AudioClip? Clip { get; private set; }

        public AudioExportRequest? Request { get; private set; }

        public Task<AudioExportResult> ExportAsync(
            AudioClip clip,
            AudioExportRequest request,
            CancellationToken cancellationToken = default)
        {
            Clip = clip;
            Request = request;
            return Task.FromResult(new AudioExportResult(request.Path, request.Format, 1));
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "MappingToolsHitsoundCopier", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
