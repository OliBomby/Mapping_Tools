using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Infrastructure.Audio;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundStudio;

[TestClass]
public sealed class HitsoundStudioFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task ExportAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("hitsound-studio", fixtureName);
        var project = fixture.ReadProject<HitsoundStudioServiceOptions>();
        project.ExportFolder = Path.Combine(Path.GetDirectoryName(fixture.TargetPath)!, "hitsound-studio-export");
        RecordingProgress<double> progress = new();

        // Act
        var result = await CreateHitsoundStudioService(fixture.Gateway)
            .ExportAsync(project, progress, CancellationToken.None);
        FixtureExecutionResult actual = new([result.MapPath!], Progress: progress.Values);

        // Assert
        fixture.AssertAccepted("Hitsound Studio");
        fixture.AssertTextOutput(actual);
        actual.Progress.Should().Equal(0.1, 0.2, 0.3, 0.6, 0.7, 0.8, 0.99, 1);
    }

    private static HitsoundStudioService CreateHitsoundStudioService(FileBackedEditingGateway gateway)
    {
        NaudioAudioDecoder decoder = new();
        NaudioAudioGenerator generator = new(decoder, new NaudioSoundFontRenderer());
        return new HitsoundStudioService(
            gateway, new EmptyMapCleanerSampleService(), generator,
            new NaudioAudioExporter(), new NaudioAudioClipMixer(), new NaudioMidiService(),
            new PhysicalBeatmapsetFileSystem(), new NoopFileRevealService(), new HitsoundStudioEngine());
    }

    private sealed class EmptyMapCleanerSampleService : IMapCleanerSampleService
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

    private sealed class NoopFileRevealService : IFileRevealService
    {
        public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
