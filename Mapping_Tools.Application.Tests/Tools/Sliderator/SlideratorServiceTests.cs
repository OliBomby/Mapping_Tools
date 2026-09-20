using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.Sliderator;

[TestClass]
public sealed class SlideratorServiceTests
{
    [TestMethod]
    public async Task ImportAsync_WithSelectedMode_RequiresLiveEditorAndFiltersCircles()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        SlideratorService service = new(gateway, new ApplicationSettings());

        // Act
        var result = await service.ImportAsync(
            "map.osu",
            HitObjectSelectionMode.Selected,
            null);

        // Assert
        gateway.OpenRequests[^1].Preference.Should().Be(LiveBeatmapPreference.RequireLive);
        result.UsedLiveEditor.Should().BeTrue();
        result.Sliders.Should().ContainSingle(item => item.IsSlider);
    }

    [TestMethod]
    [DataRow(HitObjectSelectionMode.Bookmarked)]
    [DataRow(HitObjectSelectionMode.Time)]
    [DataRow(HitObjectSelectionMode.Everything)]
    public async Task ImportAsync_WithNonSelectedMode_UsesPreferLiveAndFiltersSliders(
        HitObjectSelectionMode mode)
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.Disk));
        gateway.Session!.Beatmap.Bookmarks = [0];
        SlideratorService service = new(gateway, new ApplicationSettings());

        // Act
        var result = await service.ImportAsync(
            "map.osu",
            mode,
            mode == HitObjectSelectionMode.Time ? "00:00:000" : null);

        // Assert
        gateway.OpenRequests.Should().ContainSingle()
            .Which.Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        result.Sliders.Should().ContainSingle(item => item.IsSlider);
    }

    [TestMethod]
    public async Task RunAsync_WithLiveSession_SavesAndRequestsReload()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        SlideratorService service = new(gateway, new ApplicationSettings());
        var source = gateway.Session!.SelectedHitObjects[0];
        SlideratorServiceOptions project = new()
        {
            GlobalSv = 1.4,
            GraphBeats = 3,
            BeatsPerMinute = 180,
            PixelLength = 100,
            ExportTime = 1000,
            NewVelocity = 1 / 4.2,
            GraphState = SlideratorEngineOptions.CreatePositionGraph(3),
        };

        // Act
        var result = await service.RunAsync(
            "map.osu",
            project,
            source,
            true,
            preferLiveEditor: true);

        // Assert
        result.EditorReloaded.Should().BeTrue();
        gateway.SessionSaveRequests.Select(request => request.ReloadEditor)
            .Should().ContainSingle().Which.Should().BeTrue();
        gateway.Session.Beatmap.HitObjects.Should().HaveCount(3);
    }

    private static BeatmapEditingSession CreateSession(BeatmapEditingSource source)
    {
        List<string> lines =
        [
            "osu file format v14",
            "",
            "[General]",
            "Mode:0",
            "StackLeniency:0.7",
            "",
            "[Metadata]",
            "Version:Test",
            "",
            "[Difficulty]",
            "CircleSize:4",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "",
            "[TimingPoints]",
            "0,500,4,2,1,100,1,0",
            "",
            "[HitObjects]",
            "64,64,0,2,0,L|164:64,1,100",
            "128,128,500,1,0,0:0:0:0:",
        ];
        Beatmap beatmap = new(lines);
        var slider = beatmap.HitObjects[0];
        return new BeatmapEditingSession(
            beatmap,
            "",
            new NoOpTextFileStore { ReadResult = [] },
            source,
            [slider]);
    }
}
