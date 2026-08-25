using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.TimingHelper;
using Mapping_Tools.Core.Tools.TimingHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.TimingHelper;

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
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        TimingHelperService service = new(gateway);
        TimingHelperOptions options = new()
        {
            Objects = false,
            Bookmarks = false,
            Greenlines = false,
            Redlines = false,
        };
        RecordingProgress<double> progress = new();

        // Act
        var result = await service.AdjustAsync(
            ["first.osu", "second.osu"],
            options,
            progress);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenRequests.Select(request => request.Path)
            .Should().Equal("first.osu", "second.osu");
        gateway.OpenRequests.Select(request => request.Preference)
            .Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("first.osu", "second.osu");
        progress.Values.Last().Should().Be(1);
    }

    private static RecordingBeatmapEditingGateway CreateGateway(string fixture)
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) =>
            {
                BeatmapEditor editor = new(
                    File.ReadAllLines(fixture).ToList(),
                    new NoOpTextFileStore())
                {
                    Path = path,
                };
                return new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []);
            },
        };
    }

}
