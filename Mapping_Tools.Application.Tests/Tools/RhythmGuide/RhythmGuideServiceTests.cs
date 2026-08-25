using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.RhythmGuide;

[TestClass]
public sealed class RhythmGuideServiceTests
{
    [TestMethod]
    public async Task GenerateAsync_AddToMap_OpensLiveAwareSourcesAndSavesActualTarget()
    {
        // Arrange
        RecordingTextFileStore files = new();
        Dictionary<string, BeatmapEditingSession> sessions = [];
        RecordingBeatmapEditingGateway gateway = CreateGateway(sessions);
        TestBeatmapBackupService backups = new();
        sessions["source.osu"] = CreateSession(CreateEditor("source.osu", files, true));
        sessions["target.osu"] = CreateSession(CreateEditor("target.osu", files, false));
        RhythmGuideService service = new(gateway, backups, new RecordingBeatmapFileSystem(), files);
        RhythmGuideProject.RhythmGuideProjectOptions options = new()
        {
            Paths = ["source.osu"],
            ExportPath = "target.osu",
            ExportMode = RhythmGuideExportMode.AddToMap,
            SelectionMode = RhythmGuideSelectionMode.AllEvents,
        };

        // Act
        var result = await service.GenerateAsync(options);

        // Assert
        result.AddedObjectCount.Should().Be(1);
        gateway.OpenRequests.Should().Equal(
            ("source.osu", LiveBeatmapPreference.PreferLive),
            ("target.osu", LiveBeatmapPreference.PreferLive));
        gateway.SessionSaveRequests.Single().Session.Editor
            .Should().BeSameAs(sessions["target.osu"].Editor);
        backups.CreateRequests.Should().ContainSingle();
        backups.CreateRequests[0].Paths.Should().Equal("source.osu");
        backups.CreateRequests[0].Reason.Should().Be(BeatmapBackupReason.Automatic);
        backups.CreateRequests[0].Force.Should().BeFalse();
    }

    [TestMethod]
    public async Task GenerateAsync_NewMapWithoutExistingDestination_WritesWithoutBackupGateway()
    {
        // Arrange
        RecordingTextFileStore files = new();
        Dictionary<string, BeatmapEditingSession> sessions = [];
        RecordingBeatmapEditingGateway gateway = CreateGateway(sessions);
        sessions["source.osu"] = CreateSession(CreateEditor("source.osu", files, true));
        RhythmGuideService service = new(
            gateway,
            new TestBeatmapBackupService(),
            new RecordingBeatmapFileSystem(),
            files);
        RhythmGuideProject.RhythmGuideProjectOptions options = new()
        {
            Paths = ["source.osu"],
            ExportPath = "new.osu",
            ExportMode = RhythmGuideExportMode.NewMap,
            SelectionMode = RhythmGuideSelectionMode.AllEvents,
        };

        // Act
        var result = await service.GenerateAsync(options);

        // Assert
        result.AddedObjectCount.Should().Be(1);
        gateway.SessionSaveRequests.Should().BeEmpty();
        files.Files.Should().ContainKey("new.osu");
    }

    [TestMethod]
    public async Task GenerateAsync_NewMapOverExistingDestination_SavesThroughBackupGateway()
    {
        // Arrange
        RecordingTextFileStore files = new();
        Dictionary<string, BeatmapEditingSession> sessions = [];
        RecordingBeatmapEditingGateway gateway = CreateGateway(sessions);
        sessions["source.osu"] = CreateSession(CreateEditor("source.osu", files, true));
        RhythmGuideService service = new(
            gateway,
            new TestBeatmapBackupService(),
            new RecordingBeatmapFileSystem
            {
                ExistingPaths = { "existing.osu" },
            },
            files);
        RhythmGuideProject.RhythmGuideProjectOptions options = new()
        {
            Paths = ["source.osu"],
            ExportPath = "existing.osu",
            ExportMode = RhythmGuideExportMode.NewMap,
            SelectionMode = RhythmGuideSelectionMode.AllEvents,
        };

        // Act
        await service.GenerateAsync(options);

        // Assert
        gateway.EditorSaveRequests.Should().ContainSingle();
        gateway.EditorSaveRequests.Single().Editor.Path.Should().Be("existing.osu");
    }

    private static BeatmapEditor CreateEditor(
        string path,
        ITextFileStore files,
        bool includeObject)
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
        ];
        if (includeObject) lines.Add("256,192,1000,1,0,0:0:0:0:");
        return new BeatmapEditor(lines, files) { Path = path };
    }

    private static BeatmapEditingSession CreateSession(BeatmapEditor editor)
    {
        return new BeatmapEditingSession(
            editor,
            BeatmapEditingSource.Disk,
            []);
    }

    private static RecordingBeatmapEditingGateway CreateGateway(
        Dictionary<string, BeatmapEditingSession> sessions)
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) => sessions[path],
        };
    }

}
