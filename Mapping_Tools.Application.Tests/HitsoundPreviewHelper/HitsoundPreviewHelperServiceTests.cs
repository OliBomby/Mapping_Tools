using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.HitsoundPreviewHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.HitsoundPreviewHelper;

[TestClass]
public sealed class HitsoundPreviewHelperServiceTests
{
    [TestMethod]
    public async Task ApplyAsync_WithSelectedMode_RequiresLiveStateAndLeavesReloadToExecutionHost()
    {
        // Arrange
        RecordingGateway gateway = new(1);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperOptions options = new()
        {
            ImportModeSetting = HitsoundPreviewHelperImportMode.Selected,
            Items = [new HitsoundZone { Hitsound = Hitsound.Clap, CustomIndex = 2 }],
        };

        // Act
        var result = await service.ApplyAsync(
            ["selected.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("selected.osu");
        result.UpdatedEventCount.Should().Be(1);
        gateway.OpenPreferences.Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.RequireLive);
        gateway.SaveRequests.Should().ContainSingle()
            .Which.ReloadEditor.Should().BeFalse();
        gateway.LastBeatmap!.HitObjects[0].Hitsounds.Should().Be(8);
        gateway.LastBeatmap.HitObjects[0].CustomIndex.Should().Be(2);
    }

    [TestMethod]
    public async Task ApplyAsync_WithTimeMode_UsesTimeCodeAndPreferLive()
    {
        // Arrange
        RecordingGateway gateway = new(0);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperOptions options = new()
        {
            ImportModeSetting = HitsoundPreviewHelperImportMode.Time,
            TimeCode = "00:02:000",
            Items = [new HitsoundZone { Hitsound = Hitsound.Finish }],
        };

        // Act
        var result = await service.ApplyAsync(
            ["time.osu"],
            options);

        // Assert
        result.UpdatedEventCount.Should().Be(1);
        gateway.OpenPreferences.Should().ContainSingle()
            .Which.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.SaveRequests.Should().ContainSingle()
            .Which.ReloadEditor.Should().BeFalse();
        gateway.LastBeatmap!.HitObjects[0].Hitsounds.Should().Be(0);
        gateway.LastBeatmap.HitObjects[1].Hitsounds.Should().Be(4);
    }

    [TestMethod]
    public async Task ApplyAsync_WithBookmarkedMode_UsesOnlyBookmarkedObjects()
    {
        // Arrange
        RecordingGateway gateway = new(0, true);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperOptions options = new()
        {
            ImportModeSetting = HitsoundPreviewHelperImportMode.Bookmarked,
            Items = [new HitsoundZone { Hitsound = Hitsound.Clap }],
        };

        // Act
        var result = await service.ApplyAsync(
            ["bookmarked.osu"],
            options);

        // Assert
        result.UpdatedEventCount.Should().Be(1);
        gateway.LastBeatmap!.HitObjects[0].Hitsounds.Should().Be(0);
        gateway.LastBeatmap.HitObjects[1].Hitsounds.Should().Be(8);
    }

    [TestMethod]
    public async Task ApplyAsync_WithEverythingMode_ProcessesEveryObjectInInputOrder()
    {
        // Arrange
        RecordingGateway gateway = new(0);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperOptions options = new()
        {
            ImportModeSetting = HitsoundPreviewHelperImportMode.Everything,
            Items = [new HitsoundZone { Hitsound = Hitsound.Whistle }],
        };

        // Act
        var result = await service.ApplyAsync(
            ["first.osu", "second.osu"],
            options);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        result.UpdatedEventCount.Should().Be(4);
        gateway.OpenPreferences.Should().Equal(
            LiveBeatmapPreference.PreferLive,
            LiveBeatmapPreference.PreferLive);
        gateway.SaveRequests.Select(request => request.Path)
            .Should().Equal("first.osu", "second.osu");
    }

    [TestMethod]
    public async Task ApplyAsync_WithTimeModeAndBlankTimeCode_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        RecordingGateway gateway = new(0);
        HitsoundPreviewHelperService service = new(gateway);
        HitsoundPreviewHelperOptions options = new()
        {
            ImportModeSetting = HitsoundPreviewHelperImportMode.Time,
            Items = [new HitsoundZone()],
        };

        // Act
        Func<Task> act = () => service.ApplyAsync(["map.osu"], options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*time code*");
        gateway.OpenPreferences.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_WithoutZones_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        RecordingGateway gateway = new(0);
        HitsoundPreviewHelperService service = new(gateway);

        // Act
        Func<Task> act = () => service.ApplyAsync(
            ["map.osu"],
            new HitsoundPreviewHelperOptions());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*There are no zones!*");
        gateway.OpenPreferences.Should().BeEmpty();
    }

    private sealed class RecordingGateway : IBeatmapEditingGateway
    {
        private readonly int selectedObjectCount;
        private readonly Beatmap source;

        public RecordingGateway(int selectedObjectCount, bool bookmarkSecondObject = false)
        {
            this.selectedObjectCount = selectedObjectCount;
            source = new Beatmap(
                new List<HitObject>
                {
                    new("64,96,1000,1,0,0:0:0:0:"),
                    new("400,96,2000,1,0,0:0:0:0:"),
                },
                [],
                globalSv: 1.4);
            if (bookmarkSecondObject) source.SetBookmarks([2000]);
        }

        public List<LiveBeatmapPreference> OpenPreferences { get; } = [];

        public List<(string Path, bool ReloadEditor)> SaveRequests { get; } = [];

        public Beatmap? LastBeatmap { get; private set; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenPreferences.Add(livePreference);
            BeatmapEditor2 editor = new(
                source.GetLines(),
                new MemoryStore())
            {
                Path = path,
            };
            LastBeatmap = editor.Beatmap;
            IReadOnlyList<HitObject> selected = editor.Beatmap.HitObjects
                .Take(selectedObjectCount)
                .ToArray();
            return Task.FromResult(new BeatmapEditingSession(
                editor,
                livePreference == LiveBeatmapPreference.RequireLive
                    ? BeatmapEditingSource.LiveEditor
                    : BeatmapEditingSource.Disk,
                selected));
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
            SaveRequests.Add((editor.Path, reloadEditor));
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
