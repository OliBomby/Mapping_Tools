using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.SliderCompletionator;
using Mapping_Tools.Application.Tests.TestDoubles;
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
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        SliderCompletionatorService service = new(gateway);
        SliderCompletionatorOptions options = new();

        // Act
        var result = await service.CompleteAsync(
            ["selected.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("selected.osu");
        gateway.OpenRequests.Select(request => request.Preference).Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("selected.osu");
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
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
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
        gateway.OpenRequests.Select(request => request.Preference)
            .Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("one.osu", "two.osu");
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
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture, 1_000_000);
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
        gateway.OpenRequests.Select(request => request.Preference)
            .Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
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
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        SliderCompletionatorService service = new(gateway);

        // Act
        Func<Task> act = () => service.CompleteAsync([], new SliderCompletionatorOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenRequests.Should().BeEmpty();
    }

    private static RecordingBeatmapEditingGateway CreateGateway(
        string fixture,
        double? editorTime = null)
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
                return new BeatmapEditingSession(
                    editor,
                    BeatmapEditingSource.Disk,
                    [],
                    liveEditorTime: editorTime);
            },
        };
    }

}
