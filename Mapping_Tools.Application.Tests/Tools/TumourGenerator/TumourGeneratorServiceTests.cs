using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.TumourGenerator;

[TestClass]
public sealed class TumourGeneratorServiceTests
{
    [TestMethod]
    public async Task ImportAsync_WithSelectedMode_RequiresLiveEditorAndReturnsSelectedSliders()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        TumourGeneratorService service = new(gateway);

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
    public async Task ImportAsync_WhenSelectionContainsNoSliders_ReturnsEmptyState()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor, false));
        TumourGeneratorService service = new(gateway);

        // Act
        var result = await service.ImportAsync(
            "map.osu",
            HitObjectSelectionMode.Selected,
            null);

        // Assert
        result.Sliders.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RunAsync_WithLiveSession_SavesAndRequestsEditorReloadWithProgress()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        TumourGeneratorService service = new(gateway);
        TumourGeneratorServiceOptions project = new();
        project.TumourLayers[0].TumourCount = 1;
        List<double> progress = [];

        // Act
        var result = await service.RunAsync(
            ["map.osu"],
            project,
            true,
            new Progress<double>(progress.Add));

        // Assert
        result.Paths.Should().Equal("map.osu");
        result.SlidersTumourated.Should().Be(1);
        result.EditorReloaded.Should().BeTrue();
        gateway.SessionSaveRequests.Select(request => request.ReloadEditor)
            .Should().ContainSingle().Which.Should().BeTrue();
        progress.Should().Contain(1);
    }

    [TestMethod]
    public async Task RunAsync_WithDiskSession_SavesWithoutEditorReload()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.Disk));
        TumourGeneratorService service = new(gateway);

        // Act
        var result = await service.RunAsync(
            ["map.osu"],
            new TumourGeneratorServiceOptions(),
            true);

        // Assert
        result.EditorReloaded.Should().BeFalse();
        gateway.SessionSaveRequests.Select(request => request.ReloadEditor)
            .Should().ContainSingle().Which.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunAsync_WhenCancelledBeforeOpening_StopsWithoutSaving()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(CreateSession(BeatmapEditingSource.Disk));
        TumourGeneratorService service = new(gateway);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => service.RunAsync(
            ["map.osu"],
            new TumourGeneratorServiceOptions(),
            false,
            cancellationToken: cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        gateway.SessionSaveRequests.Should().BeEmpty();
    }

    private static BeatmapEditingSession CreateSession(
        BeatmapEditingSource source,
        bool selectedSlider = true)
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
        BeatmapEditor editor = new(lines, new NoOpTextFileStore { ReadResult = [] });
        var slider = editor.Beatmap.HitObjects[0];
        IReadOnlyList<HitObject> selected = selectedSlider ? [slider] : [editor.Beatmap.HitObjects[1]];
        return new BeatmapEditingSession(editor, source, selected);
    }

}
