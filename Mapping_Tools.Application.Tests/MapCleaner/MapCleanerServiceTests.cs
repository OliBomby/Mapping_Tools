using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
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
            new MemoryStore()) { Path = @"C:\set\map.osu" };
        RecordingGateway gateway = new(editor);
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
        gateway.Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.Saved.Should().BeSameAs(editor);
        samples.AnalyzedDirectory.Should().Be(@"C:\set");
    }

    private sealed class RecordingGateway(BeatmapEditor editor) : IBeatmapEditingGateway
    {
        public LiveBeatmapPreference? Preference { get; private set; }
        public Editor? Saved { get; private set; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(string path, LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            Preference = livePreference;
            return Task.FromResult(new BeatmapEditingSession(editor, BeatmapEditingSource.LiveEditor, []));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(string path, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(Editor value, bool reloadEditor = false, CancellationToken cancellationToken = default)
        {
            Saved = value;
            return Task.CompletedTask;
        }

        public Task SaveAsync(BeatmapEditingSession session, bool reloadEditor = false, CancellationToken cancellationToken = default)
        {
            return SaveAsync(session.Editor, reloadEditor, cancellationToken);
        }
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

    private sealed class MemoryStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            throw new NotSupportedException();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }

        public string GetParentFolder(string path)
        {
            return @"C:\set";
        }

        public string CombinePath(string parent, string child)
        {
            return Path.Combine(parent, child);
        }
    }
}
