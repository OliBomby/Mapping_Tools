using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.TimingCopier;
using Mapping_Tools.Core.Tools.TimingCopier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.TimingCopier;

[TestClass]
public sealed class TimingCopierServiceTests
{
    [TestMethod]
    public async Task CopyAsync_WithMultipleTargets_UsesLivePreferenceAndSavesEachTarget()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        TimingCopierService service = new(gateway);
        TimingCopierOptions options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "first.osu|second.osu",
            ResnapMode = TimingCopierResnapMode.KeepObjectsFixed,
        };
        RecordingProgress progress = new();

        // Act
        var result = await service.CopyAsync(options, progress);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenedPaths.Should().Equal("source.osu", "first.osu", "second.osu");
        gateway.OpenPreferences.Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SavedPaths.Should().Equal("first.osu", "second.osu");
        progress.Values.Last().Should().Be(100);
    }

    [TestMethod]
    public async Task CopyAsync_WhenSecondTargetSaveFails_LeavesFirstTargetSaved()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture) { FailOnSaveNumber = 2 };
        TimingCopierService service = new(gateway);
        TimingCopierOptions options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "first.osu|second.osu",
            ResnapMode = TimingCopierResnapMode.KeepObjectsFixed,
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<IOException>();
        gateway.SavedPaths.Should().ContainSingle().Which.Should().Be("first.osu");
    }

    [TestMethod]
    public async Task CopyAsync_WithEmptyBeatDivisors_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        TimingCopierService service = new(gateway);
        TimingCopierOptions options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "target.osu",
            BeatDivisors = [],
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenedPaths.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CopyAsync_WithUndefinedResnapMode_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        TimingCopierService service = new(gateway);
        TimingCopierOptions options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "target.osu",
            ResnapMode = (TimingCopierResnapMode)999,
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenedPaths.Should().BeEmpty();
    }

    private sealed class RecordingGateway : IBeatmapEditingGateway
    {
        private readonly string fixture;
        private int saveCount;

        public RecordingGateway(string fixture)
        {
            this.fixture = fixture;
        }

        public List<string> OpenedPaths { get; } = [];

        public List<LiveBeatmapPreference> OpenPreferences { get; } = [];

        public List<string> SavedPaths { get; } = [];

        public int FailOnSaveNumber { get; init; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenedPaths.Add(path);
            OpenPreferences.Add(livePreference);
            BeatmapEditor editor = new(
                File.ReadAllLines(fixture).ToList(),
                new MemoryStore())
            {
                Path = path,
            };
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
            saveCount++;
            if (saveCount == FailOnSaveNumber) throw new IOException("The test target could not be written.");

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

    private sealed class RecordingProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];

        public void Report(double value)
        {
            Values.Add(value);
        }
    }

    private sealed class MemoryStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            throw new NotSupportedException();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
        }

        public void Delete(string path)
        {
        }

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
