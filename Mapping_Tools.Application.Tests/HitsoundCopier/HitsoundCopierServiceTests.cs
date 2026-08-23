using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.HitsoundCopier;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.HitsoundCopier;

[TestClass]
public sealed class HitsoundCopierServiceTests
{
    [TestMethod]
    public async Task CopyAsync_WithMultipleTargets_UsesSourceSelectionAndSavesEveryTarget()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        HitsoundCopierService service = new(gateway, new StubSampleService(), new ApplicationSettings());
        HitsoundCopierOptions options = new()
        {
            PathFrom = "source.osu",
            PathTo = "first.osu|second.osu",
            SourceSelectionMode = HitsoundCopierSelectionMode.Everything,
        };

        // Act
        var result = await service.CopyAsync(options);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenedPaths.Should().Equal("source.osu", "first.osu", "second.osu");
        gateway.SavedPaths.Should().Equal("first.osu", "second.osu");
    }

    [TestMethod]
    public async Task CopyAsync_TimeSelectionWithoutCode_RejectsBeforeOpeningMaps()
    {
        // Arrange
        RecordingGateway gateway = new(Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu"));
        HitsoundCopierService service = new(gateway, new StubSampleService(), new ApplicationSettings());
        HitsoundCopierOptions options = new()
        {
            PathTo = "target.osu",
            SourceSelectionMode = HitsoundCopierSelectionMode.Time,
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenedPaths.Should().BeEmpty();
    }

    private sealed class StubSampleService : IHitsoundSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public HitsoundSampleAssignment? TryCreateAssignment(
            string directory,
            IReadOnlyList<string> sourceFilenames,
            IReadOnlyDictionary<string, string> firstSamples,
            string role,
            SampleSet sampleSet,
            int startIndex,
            SampleSchema existingSchema)
        {
            return null;
        }

        public Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingGateway : IBeatmapEditingGateway
    {
        private readonly string fixture;

        public RecordingGateway(string fixture)
        {
            this.fixture = fixture;
        }

        public List<string> OpenedPaths { get; } = [];
        public List<string> SavedPaths { get; } = [];

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            OpenedPaths.Add(path);
            BeatmapEditor editor = new(File.ReadAllLines(fixture).ToList(), new MemoryStore()) { Path = path };
            return Task.FromResult(new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            Editor editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            SavedPaths.Add(editor.Path);
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync(session.Editor, reloadEditor, cancellationToken);
        }
    }

    private sealed class MemoryStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            return [];
        }

        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }

        public string GetParentFolder(string path)
        {
            return string.Empty;
        }

        public string CombinePath(string parent, string child)
        {
            return child;
        }
    }
}
