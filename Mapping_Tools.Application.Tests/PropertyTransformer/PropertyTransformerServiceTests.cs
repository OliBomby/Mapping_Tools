using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.PropertyTransformer;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.PropertyTransformer;

[TestClass]
public sealed class PropertyTransformerServiceTests
{
    [TestMethod]
    public async Task TransformAsync_WithBeatmapSelection_UsesLivePreferenceAndSaveBoundary()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        BeatmapEditor2 editor = new(
            File.ReadAllLines(fixture).ToList(),
            new FileSystemFileStore())
        {
            Path = fixture
        };
        RecordingGateway gateway = new(editor);
        PropertyTransformerService service = new(gateway);
        PropertyTransformerOptions options = new()
        {
            BookmarkTimeOffset = 5
        };
        double[] originalBookmarks = editor.Beatmap.GetBookmarks().ToArray();
        RecordingProgress progress = new();

        // Act
        PropertyTransformerResult result = await service.TransformAsync(
            [fixture],
            options,
            progress);

        // Assert
        result.ProcessedPaths.Should().Equal(fixture);
        gateway.Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.Saved.Should().BeSameAs(editor);
        editor.Beatmap.GetBookmarks().Should().Equal(
            originalBookmarks.Select(bookmark => bookmark + 5));
        progress.Values.Last().Should().Be(100);
    }

    private sealed class RecordingGateway(BeatmapEditor2 editor) : IBeatmapEditingGateway
    {
        public LiveBeatmapPreference? Preference { get; private set; }

        public Editor2? Saved { get; private set; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            Preference = livePreference;
            return Task.FromResult(new BeatmapEditingSession(
                editor,
                BeatmapEditingSource.LiveEditor,
                []));
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            Editor2 value,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            Saved = value;
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
}
