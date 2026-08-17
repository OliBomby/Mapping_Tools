using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.TimingHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.TimingHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.TimingHelper;

[TestClass]
public sealed class TimingHelperServiceTests
{
    [TestMethod]
    public async Task AdjustAsync_WithMultiplePaths_UsesLivePreferenceAndSavesEachPath()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingGateway gateway = new(fixture);
        TimingHelperService service = new(gateway);
        TimingHelperOptions options = new()
        {
            Objects = false,
            Bookmarks = false,
            Greenlines = false,
            Redlines = false
        };
        RecordingProgress progress = new();

        // Act
        TimingHelperResult result = await service.AdjustAsync(
            ["first.osu", "second.osu"],
            options,
            progress);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenPreferences.Should().OnlyContain(
            preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SavedPaths.Should().Equal("first.osu", "second.osu");
        progress.Values.Last().Should().Be(100);
    }

    private sealed class RecordingGateway(string fixture) : IBeatmapEditingGateway
    {
        public List<string> OpenedPaths { get; } = [];

        public List<LiveBeatmapPreference> OpenPreferences { get; } = [];

        public List<string> SavedPaths { get; } = [];

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenedPaths.Add(path);
            OpenPreferences.Add(livePreference);
            BeatmapEditor2 editor = new(
                File.ReadAllLines(fixture).ToList(),
                new MemoryStore())
            {
                Path = path
            };
            return Task.FromResult(new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []));
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            Editor2 editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            SavedPaths.Add(editor.Path);
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            SaveAsync(session.Editor, reloadEditor, cancellationToken);
    }

    private sealed class RecordingProgress : IProgress<double>
    {
        public List<double> Values { get; } = [];

        public void Report(double value) => Values.Add(value);
    }

    private sealed class MemoryStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path) => throw new NotSupportedException();

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
        }

        public void Delete(string path)
        {
        }

        public string GetParentFolder(string path) => string.Empty;

        public string CombinePath(string parent, string child) => child;
    }
}
