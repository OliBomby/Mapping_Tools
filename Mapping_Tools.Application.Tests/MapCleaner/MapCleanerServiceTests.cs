using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.MapCleaner;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.MapCleaner;

[TestClass]
public sealed class MapCleanerServiceTests
{
    [TestMethod]
    public async Task CleanAsync_WithAcceptedFixture_UsesLiveStateAndBackupSaveBoundary()
    {
        // Arrange
        BeatmapEditor editor = new(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu")).ToList(),
            new NoOpTextFileStore
            {
                ParentFolderResolver = _ => @"C:\set",
                CombinePathResolver = Path.Combine,
            }) { Path = @"C:\set\map.osu" };
        RecordingBeatmapEditingGateway gateway = new(
            new BeatmapEditingSession(editor, BeatmapEditingSource.LiveEditor, []));
        RecordingSamples samples = new();
        MapCleanerService service = new(gateway, new StubFileSystem(), samples);
        MapCleanerOptions options = new()
        {
            SampleSetSliders = false,
            ResnapBookmarks = true,
            AnalyzeSamples = false,
            BeatDivisors = [new RationalBeatDivisor(12), new RationalBeatDivisor(16)],
        };

        // Act
        var result = await service.CleanAsync([editor.Path], options);

        // Assert
        result.TimingPointsRemoved.Should().Be(16);
        result.ObjectsResnapped.Should().Be(20);
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Single().Session.Editor.Should().BeSameAs(editor);
        samples.AnalyzedDirectory.Should().Be(@"C:\set");
    }

    private sealed class RecordingSamples : IMapCleanerSampleService
    {
        public string? AnalyzedDirectory { get; private set; }

        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(string directory, bool detectDuplicates, CancellationToken cancellationToken = default)
        {
            AnalyzedDirectory = directory;
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
        }

        public Task<int> MoveUnusedToRecoveryAsync(string directory, string currentBeatmapPath, Beatmap currentBeatmap, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class StubFileSystem : IBeatmapFileSystem
    {
        public bool FileExists(string path)
        {
            return true;
        }

        public string? GetParentDirectory(string filePath)
        {
            return @"C:\set";
        }
    }

}
