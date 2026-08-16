using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.RhythmGuide;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests;

[TestClass]
public sealed class RhythmGuideServiceTests
{
    [TestMethod]
    public async Task GenerateAsync_AddToMap_OpensLiveAwareSourcesAndSavesActualTarget()
    {
        // Arrange
        MemoryTextFileStore files = new();
        RecordingEditingGateway gateway = new(files);
        TestBeatmapBackupService backups = new();
        gateway.Add("source.osu", CreateEditor("source.osu", files, includeObject: true));
        gateway.Add("target.osu", CreateEditor("target.osu", files, includeObject: false));
        RhythmGuideService service = new(gateway, backups, new StubBeatmapFileSystem(), files);
        RhythmGuideOptions options = new()
        {
            Paths = ["source.osu"],
            ExportPath = "target.osu",
            ExportMode = RhythmGuideExportMode.AddToMap,
            SelectionMode = RhythmGuideSelectionMode.AllEvents
        };

        // Act
        RhythmGuideResult result = await service.GenerateAsync(options);

        // Assert
        result.AddedObjectCount.Should().Be(1);
        gateway.OpenRequests.Should().Equal(
            ("source.osu", LiveBeatmapPreference.PreferLive),
            ("target.osu", LiveBeatmapPreference.PreferLive));
        gateway.SavedEditor.Should().BeSameAs(gateway.Sessions["target.osu"].Editor);
        backups.CreateRequests.Should().ContainSingle();
        backups.CreateRequests[0].Paths.Should().Equal("source.osu");
        backups.CreateRequests[0].Reason.Should().Be(BeatmapBackupReason.Automatic);
        backups.CreateRequests[0].Force.Should().BeFalse();
    }

    [TestMethod]
    public async Task GenerateAsync_NewMapWithoutExistingDestination_WritesWithoutBackupGateway()
    {
        // Arrange
        MemoryTextFileStore files = new();
        RecordingEditingGateway gateway = new(files);
        gateway.Add("source.osu", CreateEditor("source.osu", files, includeObject: true));
        RhythmGuideService service = new(
            gateway,
            new TestBeatmapBackupService(),
            new StubBeatmapFileSystem(),
            files);
        RhythmGuideOptions options = new()
        {
            Paths = ["source.osu"],
            ExportPath = "new.osu",
            ExportMode = RhythmGuideExportMode.NewMap,
            SelectionMode = RhythmGuideSelectionMode.AllEvents
        };

        // Act
        RhythmGuideResult result = await service.GenerateAsync(options);

        // Assert
        result.AddedObjectCount.Should().Be(1);
        gateway.SavedEditor.Should().BeNull();
        files.Writes.Should().ContainKey("new.osu");
    }

    [TestMethod]
    public async Task GenerateAsync_NewMapOverExistingDestination_SavesThroughBackupGateway()
    {
        // Arrange
        MemoryTextFileStore files = new();
        RecordingEditingGateway gateway = new(files);
        gateway.Add("source.osu", CreateEditor("source.osu", files, includeObject: true));
        RhythmGuideService service = new(
            gateway,
            new TestBeatmapBackupService(),
            new StubBeatmapFileSystem { Existing = ["existing.osu"] },
            files);
        RhythmGuideOptions options = new()
        {
            Paths = ["source.osu"],
            ExportPath = "existing.osu",
            ExportMode = RhythmGuideExportMode.NewMap,
            SelectionMode = RhythmGuideSelectionMode.AllEvents
        };

        // Act
        await service.GenerateAsync(options);

        // Assert
        gateway.SavedEditor.Should().NotBeNull();
        gateway.SavedEditor!.Path.Should().Be("existing.osu");
    }

    private static BeatmapEditor2 CreateEditor(
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
            "[HitObjects]"
        ];
        if (includeObject)
        {
            lines.Add("256,192,1000,1,0,0:0:0:0:");
        }
        return new BeatmapEditor2(lines, files) { Path = path };
    }

    private sealed class RecordingEditingGateway : IBeatmapEditingGateway
    {
        public RecordingEditingGateway(ITextFileStore files)
        {
        }

        public Dictionary<string, BeatmapEditingSession> Sessions { get; } = [];

        public List<(string Path, LiveBeatmapPreference Preference)> OpenRequests { get; } = [];

        public Editor2? SavedEditor { get; private set; }

        public void Add(string path, BeatmapEditor2 editor) => Sessions[path] = new(
            editor,
            BeatmapEditingSource.Disk,
            []);

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            OpenRequests.Add((path, livePreference));
            return Task.FromResult(Sessions[path]);
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
            SavedEditor = editor;
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            SaveAsync(session.Editor, reloadEditor, cancellationToken);
    }

    private sealed class StubBeatmapFileSystem : IBeatmapFileSystem
    {
        public HashSet<string> Existing { get; init; } = [];

        public bool FileExists(string path) => Existing.Contains(path);

        public string? GetParentDirectory(string filePath) => null;
    }

    private sealed class MemoryTextFileStore : ITextFileStore
    {
        public Dictionary<string, IReadOnlyList<string>> Writes { get; } = [];

        public IReadOnlyList<string> ReadAllLines(string path) => Writes[path];

        public void WriteAllLines(string path, IEnumerable<string> lines) =>
            Writes[path] = lines.ToArray();

        public void Delete(string path) => Writes.Remove(path);

        public string GetParentFolder(string path) => string.Empty;

        public string CombinePath(string parent, string child) => child;
    }
}
