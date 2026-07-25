using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class BeatmapWorkspaceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 7, 25, 14, 30, 0, TimeSpan.Zero);

    [TestMethod]
    public void SelectionPreservesOrderPromotesRecentsAndPublishesSource()
    {
        ApplicationSettings settings = new();
        BeatmapWorkspace workspace = CreateWorkspace(settings);
        BeatmapSelectionChangedEventArgs? notification = null;
        workspace.SelectionChanged += (_, args) => notification = args;

        workspace.SetSelection(
            [@"C:\Maps\first.osu", @"C:\Maps\second.osb"],
            BeatmapSelectionSource.DragAndDrop);

        CollectionAssert.AreEqual(
            new[] { @"C:\Maps\first.osu", @"C:\Maps\second.osb" },
            workspace.SelectedPaths.ToArray());
        CollectionAssert.AreEqual(
            new[] { @"C:\Maps\second.osb", @"C:\Maps\first.osu" },
            workspace.RecentMaps.Select(recent => recent.Path).ToArray());
        Assert.IsTrue(workspace.RecentMaps.All(
            recent => recent.DisplayDate == FixedNow.DateTime.ToString()));
        Assert.AreEqual(BeatmapSelectionSource.DragAndDrop, notification?.Source);
        CollectionAssert.AreEqual(
            workspace.SelectedPaths.ToArray(),
            notification?.Paths.ToArray());
    }

    [TestMethod]
    public void RecentHistoryIsCaseSensitiveDeduplicatedAndCappedAtTwenty()
    {
        ApplicationSettings settings = new()
        {
            RecentMaps = Enumerable.Range(0, 20)
                .Select(index => new RecentBeatmap($"map-{index}.osu", $"date-{index}"))
                .ToList()
        };
        BeatmapWorkspace workspace = CreateWorkspace(settings);

        workspace.SetSelection(["map-5.osu", "MAP-5.osu", "new.osu"]);

        Assert.AreEqual(20, workspace.RecentMaps.Count);
        CollectionAssert.AreEqual(
            new[] { "new.osu", "MAP-5.osu", "map-5.osu" },
            workspace.RecentMaps.Take(3).Select(recent => recent.Path).ToArray());
        Assert.AreEqual(
            1,
            workspace.RecentMaps.Count(recent => recent.Path == "map-5.osu"));
    }

    [TestMethod]
    public void StartupRestoresLegacyJoinedEntryAndRefreshesHistory()
    {
        ApplicationSettings settings = new()
        {
            RecentMaps =
            [
                new RecentBeatmap("one.osu|two.osu", "legacy date"),
                new RecentBeatmap("older.osu", "older date")
            ]
        };
        BeatmapWorkspace workspace = CreateWorkspace(settings);
        BeatmapSelectionChangedEventArgs? notification = null;
        workspace.SelectionChanged += (_, args) => notification = args;

        bool restored = workspace.RestoreMostRecent();

        Assert.IsTrue(restored);
        CollectionAssert.AreEqual(
            new[] { "one.osu", "two.osu" },
            workspace.SelectedPaths.ToArray());
        Assert.AreEqual(BeatmapSelectionSource.Startup, notification?.Source);
    }

    [TestMethod]
    public void EmptyHistoryDoesNotCreateBlankSelectionOrRecentEntry()
    {
        ApplicationSettings settings = new();
        BeatmapWorkspace workspace = CreateWorkspace(settings);

        bool restored = workspace.RestoreMostRecent();
        workspace.SetSelection(["", "   "]);

        Assert.IsFalse(restored);
        Assert.AreEqual(0, workspace.SelectedPaths.Count);
        Assert.AreEqual(0, workspace.RecentMaps.Count);
    }

    [TestMethod]
    public async Task PickerCancellationLeavesSelectionAndHistoryUnchanged()
    {
        FakeFilePicker picker = new() { OpenPaths = [] };
        ApplicationSettings settings = new() { SongsPath = @"C:\osu!\Songs" };
        BeatmapWorkspace workspace = CreateWorkspace(settings, picker: picker);
        workspace.SetSelection([@"C:\Maps\selected.osu"]);
        RecentBeatmap[] history = workspace.RecentMaps.ToArray();

        bool selected = await workspace.PickBeatmapsAsync(allowMultiple: true);

        Assert.IsFalse(selected);
        CollectionAssert.AreEqual(
            new[] { @"C:\Maps\selected.osu" },
            workspace.SelectedPaths.ToArray());
        CollectionAssert.AreEqual(history, workspace.RecentMaps.ToArray());
        Assert.AreEqual(@"C:\Maps", picker.LastOpenRequest?.SuggestedStartLocation);
        Assert.IsTrue(picker.LastOpenRequest?.AllowMultiple);
        CollectionAssert.AreEqual(
            new[] { "*.osu", "*.osb" },
            picker.LastOpenRequest?.Filters.Single().Patterns.ToArray());
    }

    [TestMethod]
    public async Task PickerSelectionUsesFilePickerSourceAndSongsFallback()
    {
        FakeFilePicker picker = new() { OpenPaths = [@"D:\Songs\picked.osu"] };
        ApplicationSettings settings = new() { SongsPath = @"D:\Songs" };
        BeatmapWorkspace workspace = CreateWorkspace(settings, picker: picker);
        BeatmapSelectionChangedEventArgs? notification = null;
        workspace.SelectionChanged += (_, args) => notification = args;

        bool selected = await workspace.PickBeatmapsAsync(allowMultiple: false);

        Assert.IsTrue(selected);
        CollectionAssert.AreEqual(
            new[] { @"D:\Songs\picked.osu" },
            workspace.SelectedPaths.ToArray());
        Assert.AreEqual(@"D:\Songs", picker.LastOpenRequest?.SuggestedStartLocation);
        Assert.AreEqual(BeatmapSelectionSource.FilePicker, notification?.Source);
    }

    [TestMethod]
    public async Task DisabledCurrentFolderPreferenceOmitsPickerStartLocation()
    {
        FakeFilePicker picker = new() { OpenPaths = [] };
        ApplicationSettings settings = new()
        {
            SongsPath = @"D:\Songs",
            CurrentBeatmapDefaultFolder = false
        };
        BeatmapWorkspace workspace = CreateWorkspace(settings, picker: picker);
        workspace.SetSelection([@"C:\Maps\selected.osu"]);

        await workspace.PickBeatmapsAsync(allowMultiple: false);

        Assert.IsNull(picker.LastOpenRequest?.SuggestedStartLocation);
    }

    [TestMethod]
    public void MissingSelectionsAreReportedWithoutBeingRemoved()
    {
        FakeBeatmapFileSystem fileSystem = new();
        fileSystem.ExistingPaths.Add("present.osu");
        BeatmapWorkspace workspace = CreateWorkspace(
            new ApplicationSettings(),
            fileSystem: fileSystem);
        workspace.SetSelection(["present.osu", "missing.osu"]);

        IReadOnlyList<string> missing = workspace.GetMissingSelectedPaths();

        CollectionAssert.AreEqual(new[] { "missing.osu" }, missing.ToArray());
        CollectionAssert.AreEqual(
            new[] { "present.osu", "missing.osu" },
            workspace.SelectedPaths.ToArray());
    }

    [TestMethod]
    public async Task LiveLookupDistinguishesUnavailableMissingAndSelectedPaths()
    {
        FakeBeatmapFileSystem fileSystem = new();
        FakeCurrentBeatmapLocator locator = new();
        BeatmapWorkspace workspace = CreateWorkspace(
            new ApplicationSettings(),
            fileSystem: fileSystem,
            locator: locator);
        workspace.SetSelection(["fallback.osu"]);

        CurrentBeatmapSelectionResult unavailable =
            await workspace.SelectCurrentBeatmapAsync();
        locator.Path = "stale.osu";
        CurrentBeatmapSelectionResult missing =
            await workspace.SelectCurrentBeatmapAsync();
        locator.Path = "live.osu";
        fileSystem.ExistingPaths.Add("live.osu");
        CurrentBeatmapSelectionResult selected =
            await workspace.SelectCurrentBeatmapAsync();

        Assert.AreEqual(CurrentBeatmapSelectionStatus.Unavailable, unavailable.Status);
        Assert.AreEqual(CurrentBeatmapSelectionStatus.FileMissing, missing.Status);
        Assert.AreEqual("stale.osu", missing.Path);
        Assert.AreEqual(CurrentBeatmapSelectionStatus.Selected, selected.Status);
        CollectionAssert.AreEqual(
            new[] { "live.osu" },
            workspace.SelectedPaths.ToArray());
    }

    [TestMethod]
    public void RecentEntryCanBeForgottenWithoutChangingSelection()
    {
        BeatmapWorkspace workspace = CreateWorkspace(new ApplicationSettings());
        workspace.SetSelection(["keep-selected.osu", "forget.osu"]);

        bool removed = workspace.RemoveRecent("forget.osu");

        Assert.IsTrue(removed);
        Assert.IsFalse(workspace.RecentMaps.Any(recent => recent.Path == "forget.osu"));
        CollectionAssert.AreEqual(
            new[] { "keep-selected.osu", "forget.osu" },
            workspace.SelectedPaths.ToArray());
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
            new FixedTimeProvider(FixedNow));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeBeatmapFileSystem : IBeatmapFileSystem
    {
        public HashSet<string> ExistingPaths { get; } = [];

        public bool FileExists(string path) => ExistingPaths.Contains(path);

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
        public bool CanOpenFiles => true;

        public bool CanSaveFiles => false;

        public bool CanPickFolders => false;

        public IReadOnlyList<string> OpenPaths { get; init; } = [];

        public OpenFilePickerRequest? LastOpenRequest { get; private set; }

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
