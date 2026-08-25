using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.PropertyTransformer;

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
        BeatmapEditor editor = new(
            File.ReadAllLines(fixture).ToList(),
            new FileSystemFileStore())
        {
            Path = fixture,
        };
        RecordingBeatmapEditingGateway gateway = new(
            new BeatmapEditingSession(editor, BeatmapEditingSource.LiveEditor, []));
        PropertyTransformerService service = new(gateway);
        PropertyTransformerOptions options = new()
        {
            BookmarkTimeOffset = 5,
        };
        double[] originalBookmarks = editor.Beatmap.GetBookmarks().ToArray();
        RecordingProgress<double> progress = new();

        // Act
        var result = await service.TransformAsync(
            [fixture],
            options,
            progress);

        // Assert
        result.ProcessedPaths.Should().Equal(fixture);
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Single().Session.Editor.Should().BeSameAs(editor);
        editor.Beatmap.GetBookmarks().Should().Equal(
            originalBookmarks.Select(bookmark => bookmark + 5));
        progress.Values.Last().Should().Be(1);
    }

}
