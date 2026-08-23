using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Sliderator;
using Mapping_Tools.Core.Tools.Sliderator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Sliderator;

[TestClass]
public sealed class SlideratorServiceTests
{
    [TestMethod]
    public async Task ImportAsync_WithSelectedModeRequiresLiveEditorAndFiltersCircles()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        SlideratorService service = new(gateway);

        // Act
        var result = await service.ImportAsync(
            "map.osu",
            SlideratorImportMode.Selected,
            null);

        // Assert
        gateway.LastPreference.Should().Be(LiveBeatmapPreference.RequireLive);
        result.UsedLiveEditor.Should().BeTrue();
        result.Sliders.Should().ContainSingle(item => item.IsSlider);
    }

    [TestMethod]
    public async Task ImportAsync_WithBookmarkedAndTimeModesReadsDiskObjects()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.Disk));
        gateway.Session.Editor.Beatmap.Bookmarks = [0];
        SlideratorService service = new(gateway);

        // Act
        var bookmarked = await service.ImportAsync(
            "map.osu",
            SlideratorImportMode.Bookmarked,
            null);
        var timed = await service.ImportAsync(
            "map.osu",
            SlideratorImportMode.Time,
            "00:00:000");

        // Assert
        gateway.LastPreference.Should().Be(LiveBeatmapPreference.DiskOnly);
        bookmarked.Sliders.Should().ContainSingle(item => item.IsSlider);
        timed.Sliders.Should().ContainSingle(item => item.IsSlider);
    }

    [TestMethod]
    public async Task RunAsync_WithLiveSessionSavesAndRequestsReload()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        SlideratorService service = new(gateway);
        var source = gateway.Session.SelectedHitObjects[0];
        SlideratorProject project = new()
        {
            GlobalSv = 1.4,
            GraphBeats = 3,
            BeatsPerMinute = 180,
            PixelLength = 100,
            ExportTime = 1000,
            NewVelocity = 1 / 4.2,
            GraphState = SlideratorOptions.CreatePositionGraph(3),
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
        gateway.SaveReloadRequests.Should().ContainSingle().Which.Should().BeTrue();
        gateway.Session.Editor.Beatmap.HitObjects.Should().HaveCount(3);
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
        BeatmapEditor2 editor = new(lines, new MemoryTextFileStore());
        var slider = editor.Beatmap.HitObjects[0];
        return new BeatmapEditingSession(editor, source, [slider]);
    }

    private sealed class FakeEditingGateway(BeatmapEditingSession session) : IBeatmapEditingGateway
    {
        public BeatmapEditingSession Session { get; } = session;

        public LiveBeatmapPreference? LastPreference { get; private set; }

        public List<bool> SaveReloadRequests { get; } = [];

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            LastPreference = livePreference;
            return Task.FromResult(Session);
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            Editor2 editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            SaveReloadRequests.Add(reloadEditor);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryTextFileStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            return [];
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
        }

        public void Delete(string path)
        {
        }

        public string GetParentFolder(string path)
        {
            return string.Empty;
        }

        public string CombinePath(string parent, string child)
        {
            return child;
        }
    }
}
