using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Core.Tools.SliderCompletionator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.SliderCompletionator;

[TestClass]
public sealed class SliderCompletionatorServiceTests
{
    [TestMethod]
    public async Task CompleteAsync_WithSelectedMode_RequiresLiveStateAndSavesChanges()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        SliderCompletionatorService service = new(gateway);
        SliderCompletionatorOptions options = new();

        // Act
        var result = await service.CompleteAsync(
            ["selected.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("selected.osu");
        gateway.OpenPreferences.Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SavedPaths.Should().Equal("selected.osu");
    }

    [TestMethod]
    public async Task CompleteAsync_WithEverythingMode_UsesPreferLiveForEachPath()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        SliderCompletionatorService service = new(gateway);
        SliderCompletionatorOptions options = new()
        {
            ImportModeSetting = SliderCompletionatorImportMode.Everything,
        };

        // Act
        var result = await service.CompleteAsync(
            ["one.osu", "two.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("one.osu", "two.osu");
        gateway.OpenPreferences.Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SavedPaths.Should().Equal("one.osu", "two.osu");
        result.SlidersCompleted.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task CompleteAsync_WithCurrentEditorTime_UsesPreferLiveAndCapturedTime()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture, 1_000_000);
        SliderCompletionatorService service = new(gateway);
        SliderCompletionatorOptions options = new()
        {
            ImportModeSetting = SliderCompletionatorImportMode.Everything,
            UseEndTime = true,
            UseCurrentEditorTime = true,
        };

        // Act
        var result = await service.CompleteAsync(
            ["current.osu", "other.osu"],
            options);

        // Assert
        gateway.OpenPreferences.Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        result.ProcessedPaths.Should().Equal("current.osu", "other.osu");
        result.SlidersCompleted.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task CompleteAsync_WithoutPaths_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        SliderCompletionatorService service = new(gateway);

        // Act
        Func<Task> act = () => service.CompleteAsync([], new SliderCompletionatorOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenPreferences.Should().BeEmpty();
    }

    private sealed class RecordingGateway(string fixture, double? editorTime = null) : IBeatmapEditingGateway
    {
        public List<LiveBeatmapPreference> OpenPreferences { get; } = [];

        public List<string> SavedPaths { get; } = [];

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenPreferences.Add(livePreference);
            BeatmapEditor editor = new(
                File.ReadAllLines(fixture).ToList(),
                new MemoryStore())
            {
                Path = path,
            };
            return Task.FromResult(new BeatmapEditingSession(
                editor,
                BeatmapEditingSource.Disk,
                [],
                liveEditorTime: editorTime));
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
