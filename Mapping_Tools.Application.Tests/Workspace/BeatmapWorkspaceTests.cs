using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Workspace;

[TestClass]
public sealed class BeatmapWorkspaceTests
{
    private static readonly DateTimeOffset fixedNow =
        new(2026, 7, 25, 14, 30, 0, TimeSpan.Zero);

    [TestMethod]
    public void SetSelection_WithMultiplePaths_PreservesOrderPromotesRecentsAndPublishes()
    {
        // Arrange
        ApplicationSettings settings = new();
        var workspace = CreateWorkspace(settings);
        BeatmapSelectionChangedEventArgs? notification = null;
        workspace.SelectionChanged += (_, args) => notification = args;

        // Act
        workspace.SetSelection(
            [@"C:\Maps\first.osu", @"C:\Maps\second.osb"],
            BeatmapSelectionSource.DragAndDrop);

        // Assert
        workspace.SelectedPaths.ToArray().Should().Equal(@"C:\Maps\first.osu", @"C:\Maps\second.osb");
        workspace.RecentMaps.Select(recent => recent.Path).ToArray().Should().Equal(@"C:\Maps\second.osb", @"C:\Maps\first.osu");
        workspace.RecentMaps.All(recent => recent.DisplayDate == fixedNow.DateTime.ToString()).Should().BeTrue();
        (notification?.Source).Should().Be(BeatmapSelectionSource.DragAndDrop);
        (notification?.Paths.ToArray()).Should().Equal(workspace.SelectedPaths.ToArray());
    }

    [TestMethod]
    public void SetSelection_WithExistingHistory_DeduplicatesCaseSensitivelyAndCapsAtTwenty()
    {
        // Arrange
        ApplicationSettings settings = new()
        {
            RecentMaps = Enumerable.Range(0, 20)
                .Select(index => new RecentBeatmap($"map-{index}.osu", $"date-{index}"))
                .ToList(),
        };
        var workspace = CreateWorkspace(settings);

        // Act
        workspace.SetSelection(["map-5.osu", "MAP-5.osu", "new.osu"]);

        // Assert
        workspace.RecentMaps.Count.Should().Be(20);
        workspace.RecentMaps.Take(3).Select(recent => recent.Path).ToArray().Should().Equal("new.osu", "MAP-5.osu", "map-5.osu");
        workspace.RecentMaps.Count(recent => recent.Path == "map-5.osu").Should().Be(1);
    }

    [TestMethod]
    public void RestoreMostRecent_WithLegacyJoinedEntry_RestoresAndRefreshesHistory()
    {
        // Arrange
        ApplicationSettings settings = new()
        {
            RecentMaps =
            [
                new RecentBeatmap("one.osu|two.osu", "legacy date"),
                new RecentBeatmap("older.osu", "older date"),
            ],
        };
        var workspace = CreateWorkspace(settings);
        BeatmapSelectionChangedEventArgs? notification = null;
        workspace.SelectionChanged += (_, args) => notification = args;

        // Act
        bool restored = workspace.RestoreMostRecent();

        // Assert
        restored.Should().BeTrue();
        workspace.SelectedPaths.ToArray().Should().Equal("one.osu", "two.osu");
        (notification?.Source).Should().Be(BeatmapSelectionSource.Startup);
    }

    [TestMethod]
    public void RestoreMostRecent_WithEmptyHistory_DoesNotCreateSelection()
    {
        // Arrange
        ApplicationSettings settings = new();
        var workspace = CreateWorkspace(settings);

        // Act
        bool restored = workspace.RestoreMostRecent();
        workspace.SetSelection(["", "   "]);

        // Assert
        restored.Should().BeFalse();
        workspace.SelectedPaths.Count.Should().Be(0);
        workspace.RecentMaps.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task PickBeatmapsAsync_WhenCancelled_LeavesSelectionAndHistoryUnchanged()
    {
        // Arrange
        FakeFilePicker picker = new() { OpenPaths = [] };
        ApplicationSettings settings = new() { SongsPath = @"C:\osu!\Songs" };
        var workspace = CreateWorkspace(settings, picker);
        workspace.SetSelection([@"C:\Maps\selected.osu"]);
        var history = workspace.RecentMaps.ToArray();

        // Act
        bool selected = await workspace.PickBeatmapsAsync(true);

        // Assert
        selected.Should().BeFalse();
        workspace.SelectedPaths.ToArray().Should().Equal(@"C:\Maps\selected.osu");
        workspace.RecentMaps.ToArray().Should().Equal(history);
        (picker.LastOpenRequest?.SuggestedStartLocation).Should().Be(@"C:\Maps");
        (picker.LastOpenRequest?.AllowMultiple).Should().BeTrue();
        (picker.LastOpenRequest?.Filters.Single().Patterns.ToArray()).Should().Equal("*.osu", "*.osb");
    }

    [TestMethod]
    public async Task PickBeatmapsAsync_WithSelection_UsesPickerSourceAndSongsFallback()
    {
        // Arrange
        FakeFilePicker picker = new() { OpenPaths = [@"D:\Songs\picked.osu"] };
        ApplicationSettings settings = new() { SongsPath = @"D:\Songs" };
        var workspace = CreateWorkspace(settings, picker);
        BeatmapSelectionChangedEventArgs? notification = null;
        workspace.SelectionChanged += (_, args) => notification = args;

        // Act
        bool selected = await workspace.PickBeatmapsAsync(false);

        // Assert
        selected.Should().BeTrue();
        workspace.SelectedPaths.ToArray().Should().Equal(@"D:\Songs\picked.osu");
        (picker.LastOpenRequest?.SuggestedStartLocation).Should().Be(@"D:\Songs");
        (notification?.Source).Should().Be(BeatmapSelectionSource.FilePicker);
    }

    [TestMethod]
    public async Task PickBeatmapsAsync_WithCurrentFolderDisabled_OmitsStartLocation()
    {
        // Arrange
        FakeFilePicker picker = new() { OpenPaths = [] };
        ApplicationSettings settings = new()
        {
            SongsPath = @"D:\Songs",
            CurrentBeatmapDefaultFolder = false,
        };
        var workspace = CreateWorkspace(settings, picker);
        workspace.SetSelection([@"C:\Maps\selected.osu"]);

        // Act
        await workspace.PickBeatmapsAsync(false);

        // Assert
        (picker.LastOpenRequest?.SuggestedStartLocation).Should().BeNull();
    }

    [TestMethod]
    public void GetMissingSelectedPaths_WithMissingFiles_ReportsWithoutRemoval()
    {
        // Arrange
        FakeBeatmapFileSystem fileSystem = new();
        fileSystem.ExistingPaths.Add("present.osu");
        var workspace = CreateWorkspace(
            new ApplicationSettings(),
            fileSystem: fileSystem);
        workspace.SetSelection(["present.osu", "missing.osu"]);

        // Act
        var missing = workspace.GetMissingSelectedPaths();

        // Assert
        missing.ToArray().Should().Equal("missing.osu");
        workspace.SelectedPaths.ToArray().Should().Equal("present.osu", "missing.osu");
    }

    [TestMethod]
    public async Task SelectCurrentBeatmapAsync_WithLiveStatuses_DistinguishesOutcomes()
    {
        // Arrange
        FakeBeatmapFileSystem fileSystem = new();
        FakeCurrentBeatmapLocator locator = new();
        var workspace = CreateWorkspace(
            new ApplicationSettings(),
            fileSystem: fileSystem,
            locator: locator);
        workspace.SetSelection(["fallback.osu"]);

        // Act
        var unavailable =
            await workspace.SelectCurrentBeatmapAsync();
        locator.Path = "stale.osu";
        var missing =
            await workspace.SelectCurrentBeatmapAsync();
        locator.Path = "live.osu";
        fileSystem.ExistingPaths.Add("live.osu");
        var selected =
            await workspace.SelectCurrentBeatmapAsync();

        // Assert
        unavailable.Status.Should().Be(CurrentBeatmapSelectionStatus.Unavailable);
        missing.Status.Should().Be(CurrentBeatmapSelectionStatus.FileMissing);
        missing.Path.Should().Be("stale.osu");
        selected.Status.Should().Be(CurrentBeatmapSelectionStatus.Selected);
        workspace.SelectedPaths.ToArray().Should().Equal("live.osu");
    }

    [TestMethod]
    public void RemoveRecent_WithSelectedEntry_RemovesHistoryOnly()
    {
        // Arrange
        var workspace = CreateWorkspace(new ApplicationSettings());
        workspace.SetSelection(["keep-selected.osu", "forget.osu"]);

        // Act
        bool removed = workspace.RemoveRecent("forget.osu");

        // Assert
        removed.Should().BeTrue();
        workspace.RecentMaps.Any(recent => recent.Path == "forget.osu").Should().BeFalse();
        workspace.SelectedPaths.ToArray().Should().Equal("keep-selected.osu", "forget.osu");
    }

    private static BeatmapWorkspace CreateWorkspace(
        ApplicationSettings settings,
        FakeFilePicker? picker = null,
        FakeBeatmapFileSystem? fileSystem = null,
        FakeCurrentBeatmapLocator? locator = null)
    {
        return new BeatmapWorkspace(
            settings,
            picker ?? new FakeFilePicker(),
            fileSystem ?? new FakeBeatmapFileSystem(),
            locator ?? new FakeCurrentBeatmapLocator(),
            new FixedTimeProvider(fixedNow));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }

    private sealed class FakeBeatmapFileSystem : IBeatmapFileSystem
    {
        public HashSet<string> ExistingPaths { get; } = [];

        public bool FileExists(string path)
        {
            return ExistingPaths.Contains(path);
        }

        public string? GetParentDirectory(string filePath)
        {
            int separator = filePath.LastIndexOf('\\');
            return separator < 0 ? null : filePath[..separator];
        }
    }

    private sealed class FakeCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        public string? Path { get; set; }

        public Task<string?> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Path);
        }
    }

    private sealed class FakeFilePicker : IFilePicker
    {
        public IReadOnlyList<string> OpenPaths { get; init; } = [];

        public OpenFilePickerRequest? LastOpenRequest { get; private set; }
        public bool CanOpenFiles => true;

        public bool CanSaveFiles => false;

        public bool CanPickFolders => false;

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastOpenRequest = request;
            return Task.FromResult(OpenPaths);
        }

        public Task<string?> PickSaveFileAsync(
            SaveFilePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<string>> PickFoldersAsync(
            OpenFolderPickerRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
