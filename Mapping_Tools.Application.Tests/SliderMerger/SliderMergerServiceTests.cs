using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.SliderMerger;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.SliderMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.SliderMerger;

[TestClass]
public sealed class SliderMergerServiceTests
{
    [TestMethod]
    public async Task MergeAsync_WithSelectedModeRequiresLiveStateAndSavesChanges()
    {
        // Arrange
        RecordingGateway gateway = new();
        SliderMergerService service = new(gateway);

        // Act
        var result = await service.MergeAsync(
            ["selected.osu"],
            new SliderMergerOptions());

        // Assert
        result.ProcessedPaths.Should().Equal("selected.osu");
        gateway.OpenPreferences.Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SavedPaths.Should().Equal("selected.osu");
    }

    [TestMethod]
    public async Task MergeAsync_WithEverythingModeUsesPreferLiveForEachPath()
    {
        // Arrange
        RecordingGateway gateway = new();
        SliderMergerService service = new(gateway);
        SliderMergerOptions options = new()
        {
            ImportModeSetting = SliderMergerImportMode.Everything,
        };

        // Act
        var result = await service.MergeAsync(
            ["one.osu", "two.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("one.osu", "two.osu");
        gateway.OpenPreferences.Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SavedPaths.Should().Equal("one.osu", "two.osu");
        result.ObjectsMerged.Should().Be(4);
    }

    [TestMethod]
    public async Task MergeAsync_WithBookmarkedModeUsesBookmarkedObjects()
    {
        // Arrange
        RecordingGateway gateway = new();
        SliderMergerService service = new(gateway);
        SliderMergerOptions options = new()
        {
            ImportModeSetting = SliderMergerImportMode.Bookmarked,
            Leniency = 100,
        };

        // Act
        var result = await service.MergeAsync(["bookmarked.osu"], options);

        // Assert
        result.ObjectsMerged.Should().Be(2);
        gateway.OpenPreferences.Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.PreferLive);
    }

    [TestMethod]
    public async Task MergeAsync_WithTimeModeUsesTimeCodeObjects()
    {
        // Arrange
        RecordingGateway gateway = new();
        SliderMergerService service = new(gateway);
        SliderMergerOptions options = new()
        {
            ImportModeSetting = SliderMergerImportMode.Time,
            TimeCode = "00:00:000 (1,2)",
            Leniency = 100,
        };

        // Act
        var result = await service.MergeAsync(["time.osu"], options);

        // Assert
        result.ObjectsMerged.Should().Be(2);
        gateway.OpenPreferences.Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.PreferLive);
    }

    [TestMethod]
    public async Task MergeAsync_WithoutPathsThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        RecordingGateway gateway = new();
        SliderMergerService service = new(gateway);

        // Act
        Func<Task> act = () => service.MergeAsync([], new SliderMergerOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenPreferences.Should().BeEmpty();
    }

    private sealed class RecordingGateway : IBeatmapEditingGateway
    {
        public List<LiveBeatmapPreference> OpenPreferences { get; } = [];

        public List<string> SavedPaths { get; } = [];

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenPreferences.Add(livePreference);
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
                new MemoryStore())
            {
                Path = path,
            };
            editor.Beatmap.CalculateHitObjectComboStuff();
            editor.Beatmap.SetBookmarks([0, 100]);
            return Task.FromResult(new BeatmapEditingSession(
                editor,
                BeatmapEditingSource.Disk,
                [editor.Beatmap.HitObjects[0]],
                liveEditorTime: null));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            Editor editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            SavedPaths.Add(editor.Path);
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync(session.Editor, reloadEditor, cancellationToken);
        }

        private sealed class MemoryStore : ITextFileStore
        {
            public IReadOnlyList<string> ReadAllLines(string path)
            {
                throw new NotSupportedException();
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
}
