using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.SliderMerger;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.SliderMerger;

[TestClass]
public sealed class SliderMergerServiceTests
{
    [TestMethod]
    public async Task MergeAsync_WithSelectedModeRequiresLiveStateAndSavesChanges()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway();
        SliderMergerService service = new(gateway);

        // Act
        var result = await service.MergeAsync(
            ["selected.osu"],
            new SliderMergerServiceOptions());

        // Assert
        result.ProcessedPaths.Should().Equal("selected.osu");
        gateway.OpenRequests.Select(request => request.Preference).Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("selected.osu");
    }

    [TestMethod]
    public async Task MergeAsync_WithEverythingModeUsesPreferLiveForEachPath()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway();
        SliderMergerService service = new(gateway);
        SliderMergerServiceOptions options = new()
        {
            ImportModeSetting = HitObjectSelectionMode.Everything,
        };

        // Act
        var result = await service.MergeAsync(
            ["one.osu", "two.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("one.osu", "two.osu");
        gateway.OpenRequests.Select(request => request.Preference)
            .Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("one.osu", "two.osu");
        result.ObjectsMerged.Should().Be(4);
    }

    [TestMethod]
    public async Task MergeAsync_WithBookmarkedModeUsesBookmarkedObjects()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway();
        SliderMergerService service = new(gateway);
        SliderMergerServiceOptions options = new()
        {
            ImportModeSetting = HitObjectSelectionMode.Bookmarked,
            Leniency = 100,
        };

        // Act
        var result = await service.MergeAsync(["bookmarked.osu"], options);

        // Assert
        result.ObjectsMerged.Should().Be(2);
        gateway.OpenRequests.Select(request => request.Preference).Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.PreferLive);
    }

    [TestMethod]
    public async Task MergeAsync_WithTimeModeUsesTimeCodeObjects()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway();
        SliderMergerService service = new(gateway);
        SliderMergerServiceOptions options = new()
        {
            ImportModeSetting = HitObjectSelectionMode.Time,
            TimeCode = "00:00:000 (1,2)",
            Leniency = 100,
        };

        // Act
        var result = await service.MergeAsync(["time.osu"], options);

        // Assert
        result.ObjectsMerged.Should().Be(2);
        gateway.OpenRequests.Select(request => request.Preference).Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.PreferLive);
    }

    [TestMethod]
    public async Task MergeAsync_WithoutPathsThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway();
        SliderMergerService service = new(gateway);

        // Act
        Func<Task> act = () => service.MergeAsync([], new SliderMergerServiceOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenRequests.Should().BeEmpty();
    }

    private static RecordingBeatmapEditingGateway CreateGateway()
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) =>
            {
                HitObject first = new("64,64,0,1,0");
                HitObject second = new("164,64,100,1,0");
                TimingPoint redline = new(
                    0,
                    500,
                    4,
                    SampleSet.Normal,
                    0,
                    100,
                    true,
                    false,
                    false);
                BeatmapEditor editor = new(
                    new Beatmap([first, second], [redline], redline).GetLines(),
                    new NoOpTextFileStore())
                {
                    Path = path,
                };
                editor.Beatmap.CalculateHitObjectComboStuff();
                editor.Beatmap.SetBookmarks([0, 100]);
                return new BeatmapEditingSession(
                    editor,
                    BeatmapEditingSource.Disk,
                    [editor.Beatmap.HitObjects[0]],
                    liveEditorTime: null);
            },
        };
    }
}
