using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundPreviewHelper;

[TestClass]
public sealed class HitsoundPreviewHelperServiceTests
{
    [TestMethod]
    public async Task ApplyAsync_AlwaysProcessesEveryObjectAndLeavesReloadToExecutionHost()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway(1);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperServiceOptions options = new()
        {
            Items = [new HitsoundZone { Hitsound = Hitsound.Clap, CustomIndex = 2 }],
        };

        // Act
        var result = await service.ApplyAsync(
            ["selected.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("selected.osu");
        result.UpdatedEventCount.Should().Be(2);
        gateway.OpenRequests.Select(request => request.Preference).Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Should().ContainSingle()
            .Which.ReloadEditor.Should().BeFalse();
        gateway.LastOpenedSession!.Editor.Beatmap.HitObjects[0].Hitsounds.Should().Be(8);
        gateway.LastOpenedSession.Editor.Beatmap.HitObjects[0].CustomIndex.Should().Be(2);
        gateway.LastOpenedSession.Editor.Beatmap.HitObjects[1].Hitsounds.Should().Be(8);
    }

    [TestMethod]
    public async Task ApplyAsync_ProcessesEveryObjectInEveryInputMap()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway(0);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperServiceOptions options = new()
        {
            Items = [new HitsoundZone { Hitsound = Hitsound.Whistle }],
        };

        // Act
        var result = await service.ApplyAsync(
            ["first.osu", "second.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        result.UpdatedEventCount.Should().Be(4);
        gateway.OpenRequests.Select(request => request.Preference).Should().Equal(
            LiveBeatmapPreference.PreferLive,
            LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("first.osu", "second.osu");
    }

    [TestMethod]
    public async Task ApplyAsync_WithoutZones_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = CreateGateway(0);
        HitsoundPreviewHelperService service = new(gateway);

        // Act
        Func<Task> act = () => service.ApplyAsync(
            ["map.osu"],
            new HitsoundPreviewHelperServiceOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*There are no zones!*");
        gateway.OpenRequests.Should().BeEmpty();
    }

    private static RecordingBeatmapEditingGateway CreateGateway(int selectedObjectCount)
    {
        Beatmap source = new(
            new List<HitObject>
            {
                new("64,96,1000,1,0,0:0:0:0:"),
                new("400,96,2000,1,0,0:0:0:0:"),
            },
            [],
            globalSv: 1.4);
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, livePreference) =>
            {
                BeatmapEditor editor = new(
                    source.GetLines(),
                    new NoOpTextFileStore())
                {
                    Path = path,
                };
                IReadOnlyList<HitObject> selected = editor.Beatmap.HitObjects
                    .Take(selectedObjectCount)
                    .ToArray();
                return new BeatmapEditingSession(
                    editor,
                    livePreference == LiveBeatmapPreference.RequireLive
                        ? BeatmapEditingSource.LiveEditor
                        : BeatmapEditingSource.Disk,
                    selected);
            },
        };
    }
}
