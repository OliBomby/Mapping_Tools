using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class BeatmapBackupServiceTests
{
    private const string BackupDirectory = @"C:\Backups";
    private const string MapPath = @"C:\osu!\Songs\set\map.osu";
    private static readonly DateTimeOffset Now =
        new(2026, 7, 25, 14, 5, 6, TimeSpan.FromHours(2));

    [TestMethod]
    public async Task UserBackupUsesLegacyNameAndPrunesOldestUnprotectedFile()
    {
        MemoryBackupStore store = CreateStore();
        store.AddFile(
            Path.Combine(BackupDirectory, "old-1.osu"),
            ["old"],
            DateTimeOffset.UnixEpoch);
        store.AddFile(
            Path.Combine(BackupDirectory, "old-2.osu"),
            ["old"],
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        ApplicationSettings settings = CreateSettings();
        settings.MaxBackupFiles = 2;
        BeatmapBackupService service = CreateService(store, settings);

        BeatmapBackupResult result = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.User,
            force: true);

        string expected = Path.Combine(
            BackupDirectory,
            "2026-07-25 14-05-06_UB__map.osu");
        Assert.AreEqual(expected, result.Artifacts.Single().Path);
        Assert.IsTrue(store.Files.ContainsKey(expected));
        Assert.IsFalse(store.Files.ContainsKey(
            Path.Combine(BackupDirectory, "old-1.osu")));
        Assert.IsTrue(store.Files.ContainsKey(
            Path.Combine(BackupDirectory, "old-2.osu")));
    }

    [TestMethod]
    public async Task AutomaticBackupCanRespectPreferenceOrBeForcedByDestructiveSave()
    {
        MemoryBackupStore store = CreateStore();
        ApplicationSettings settings = CreateSettings();
        settings.MakeBackups = false;
        BeatmapBackupService service = CreateService(store, settings);

        BeatmapBackupResult skipped = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic);
        BeatmapBackupResult forced = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);

        Assert.IsTrue(skipped.SkippedByPreference);
        Assert.AreEqual(0, skipped.Artifacts.Count);
        Assert.IsFalse(forced.SkippedByPreference);
        Assert.AreEqual(1, forced.Artifacts.Count);
    }

    [TestMethod]
    public async Task SameSecondBackupsDoNotOverwriteAnEarlierSafetyCopy()
    {
        MemoryBackupStore store = CreateStore();
        BeatmapBackupService service = CreateService(store, CreateSettings());

        BeatmapBackupResult first = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);
        BeatmapBackupResult second = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);

        Assert.AreNotEqual(
            first.Artifacts.Single().Path,
            second.Artifacts.Single().Path);
        StringAssert.Contains(second.Artifacts.Single().Path, "_C2_map.osu");
        Assert.IsTrue(store.Files.ContainsKey(first.Artifacts.Single().Path));
        Assert.IsTrue(store.Files.ContainsKey(second.Artifacts.Single().Path));
    }

    [TestMethod]
    public async Task MissingBackupDirectoryStopsRequestBeforeSourceCopy()
    {
        MemoryBackupStore store = CreateStore();
        store.Directories.Clear();
        BeatmapBackupService service = CreateService(store, CreateSettings());

        await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(
            () => service.CreateAsync(
                [MapPath],
                BeatmapBackupReason.Automatic,
                force: true));

        Assert.AreEqual(0, store.CopyOperations.Count);
    }

    [TestMethod]
    public async Task LiveSessionCreatesDiskAndUnsavedCompanionSnapshots()
    {
        MemoryBackupStore store = CreateStore();
        BeatmapEditor2 editor = new(MapPath, store);
        editor.Beatmap.Metadata["Version"] = new TValue("Unsaved");
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.LiveEditor,
            []);
        BeatmapBackupService service = CreateService(store, CreateSettings());

        BeatmapBackupResult result = await service.CreateAsync(
            session,
            BeatmapBackupReason.User,
            force: true);

        Assert.AreEqual(2, result.Artifacts.Count);
        Assert.IsFalse(result.Artifacts[0].ContainsUnsavedState);
        Assert.IsTrue(result.Artifacts[1].ContainsUnsavedState);
        StringAssert.Contains(result.Artifacts[0].Path, "_UB__map.osu");
        StringAssert.Contains(result.Artifacts[1].Path, "_UB_2_map.osu");
        Assert.IsFalse(store.Files[result.Artifacts[0].Path]
            .Any(line => line == "Version:Unsaved"));
        Assert.IsTrue(store.Files[result.Artifacts[1].Path]
            .Any(line => line == "Version:Unsaved"));
    }

    [TestMethod]
    public async Task PeriodicBackupSkipsUnchangedSerializationAndTracksEachMapSeparately()
    {
        MemoryBackupStore store = CreateStore();
        BeatmapEditor2 editor = new(MapPath, store);
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.Disk,
            []);
        BeatmapBackupService service = CreateService(store, CreateSettings());

        BeatmapBackupArtifact? first =
            await service.CreatePeriodicIfChangedAsync(session);
        BeatmapBackupArtifact? unchanged =
            await service.CreatePeriodicIfChangedAsync(session);
        editor.Beatmap.Metadata["Version"] = new TValue("Changed");
        BeatmapBackupArtifact? changed =
            await service.CreatePeriodicIfChangedAsync(session);

        Assert.IsNotNull(first);
        Assert.IsNull(unchanged);
        Assert.IsNotNull(changed);
        StringAssert.Contains(first.Path, "_PB__map.osu");
        Assert.AreEqual(2, store.WriteOperations.Count);
    }

    [TestMethod]
    public async Task IncompatibleRestoreDoesNotCreateSafetyBackupOrOverwriteDestination()
    {
        MemoryBackupStore store = CreateStore();
        string incompatible = Path.Combine(BackupDirectory, "other.osu");
        store.AddFile(
            incompatible,
            ChangeDifficulty(store.Files[MapPath], "Other"),
            Now.AddMinutes(-1));
        List<string> original = store.Files[MapPath].ToList();
        BeatmapBackupService service = CreateService(store, CreateSettings());

        BeatmapBackupIncompatibleException exception =
            await Assert.ThrowsExceptionAsync<BeatmapBackupIncompatibleException>(
                () => service.RestoreAsync(incompatible, MapPath));

        StringAssert.Contains(exception.BackupFileName, "[Other]");
        CollectionAssert.AreEqual(original, store.Files[MapPath]);
        Assert.AreEqual(0, store.CopyOperations.Count);
    }

    [TestMethod]
    public async Task RestorePreservesDestinationBeforeOverwriteAndReloadsLast()
    {
        MemoryBackupStore store = CreateStore();
        string backup = Path.Combine(BackupDirectory, "chosen.osu");
        List<string> restoredLines = store.Files[MapPath].ToList();
        int previewIndex = restoredLines.FindIndex(
            line => line.StartsWith("PreviewTime:", StringComparison.Ordinal));
        restoredLines[previewIndex] = "PreviewTime:9876";
        store.AddFile(backup, restoredLines, Now.AddMinutes(-1));
        RecordingReloadService reload = new(store);
        ApplicationSettings settings = CreateSettings();
        settings.MaxBackupFiles = 1;
        BeatmapBackupService service = CreateService(
            store,
            settings,
            reload);

        BeatmapRestoreResult result = await service.RestoreAsync(
            backup,
            MapPath,
            reloadEditor: true);

        Assert.IsTrue(result.SafetyBackup.Path.Contains("_RU__map.osu"));
        Assert.IsTrue(store.Files.ContainsKey(backup));
        Assert.AreEqual(2, store.CopyOperations.Count);
        Assert.AreEqual(MapPath, store.CopyOperations[1].Destination);
        Assert.IsTrue(store.Files[MapPath].Contains("PreviewTime:9876"));
        Assert.AreEqual(1, reload.ReloadCount);
        Assert.IsTrue(reload.ReloadFollowedRestore);
    }

    [TestMethod]
    public async Task QuickUndoSelectsNewestBeforeCreatingItsRestoreSafetyCopy()
    {
        MemoryBackupStore store = CreateStore();
        string older = Path.Combine(BackupDirectory, "older.osu");
        string newer = Path.Combine(BackupDirectory, "newer.osu");
        List<string> olderLines = store.Files[MapPath].ToList();
        List<string> newerLines = store.Files[MapPath].ToList();
        int olderPreview = olderLines.FindIndex(
            line => line.StartsWith("PreviewTime:", StringComparison.Ordinal));
        int newerPreview = newerLines.FindIndex(
            line => line.StartsWith("PreviewTime:", StringComparison.Ordinal));
        olderLines[olderPreview] = "PreviewTime:1111";
        newerLines[newerPreview] = "PreviewTime:2222";
        store.AddFile(older, olderLines, Now.AddMinutes(-2));
        store.AddFile(newer, newerLines, Now.AddMinutes(-1));
        BeatmapBackupService service = CreateService(store, CreateSettings());

        BeatmapRestoreResult? result = await service.QuickUndoAsync(MapPath);

        Assert.IsNotNull(result);
        Assert.AreEqual(newer, result.BackupPath);
        Assert.IsTrue(store.Files[MapPath].Contains("PreviewTime:2222"));
        Assert.IsTrue(store.Files.ContainsKey(result.SafetyBackup.Path));
    }

    [TestMethod]
    public async Task RetentionNeverDeletesTheSafetyCopyRequiredForTheCurrentSave()
    {
        MemoryBackupStore store = CreateStore();
        ApplicationSettings settings = CreateSettings();
        settings.MaxBackupFiles = 0;
        BeatmapBackupService service = CreateService(store, settings);

        BeatmapBackupResult result = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);

        Assert.IsTrue(store.Files.ContainsKey(result.Artifacts.Single().Path));
    }

    [TestMethod]
    public async Task PreCancelledRequestDoesNotCreateOrDeleteFiles()
    {
        MemoryBackupStore store = CreateStore();
        BeatmapBackupService service = CreateService(store, CreateSettings());
        using CancellationTokenSource source = new();
        source.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => service.CreateAsync(
                [MapPath],
                BeatmapBackupReason.Automatic,
                force: true,
                source.Token));

        Assert.AreEqual(0, store.CopyOperations.Count);
        Assert.AreEqual(0, store.DeletedPaths.Count);
    }

    private static BeatmapBackupService CreateService(
        MemoryBackupStore store,
        ApplicationSettings settings,
        RecordingReloadService? reload = null)
    {
        return new BeatmapBackupService(
            store,
            store,
            reload ?? new RecordingReloadService(store),
            settings,
            new FixedTimeProvider(Now));
    }

    private static ApplicationSettings CreateSettings()
    {
        return new ApplicationSettings
        {
            BackupsPath = BackupDirectory,
            MakeBackups = true,
            MakePeriodicBackups = true,
            MaxBackupFiles = 1000
        };
    }

    private static MemoryBackupStore CreateStore()
    {
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        MemoryBackupStore store = new();
        store.Directories.Add(BackupDirectory);
        store.AddFile(MapPath, File.ReadAllLines(fixture), Now.AddHours(-1));
        return store;
    }

    private static List<string> ChangeDifficulty(
        IEnumerable<string> lines,
        string difficulty)
    {
        List<string> changed = lines.ToList();
        int index = changed.FindIndex(
            line => line.StartsWith("Version:", StringComparison.Ordinal));
        changed[index] = $"Version:{difficulty}";
        return changed;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone(
                "Test",
                _now.Offset,
                "Test",
                "Test");
    }

    private sealed class RecordingReloadService : IEditorReloadService
    {
        private readonly MemoryBackupStore _store;

        public RecordingReloadService(MemoryBackupStore store)
        {
            _store = store;
        }

        public int ReloadCount { get; private set; }

        public bool ReloadFollowedRestore { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReloadCount++;
            ReloadFollowedRestore = _store.CopyOperations.Count >= 2;
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryBackupStore : IBeatmapBackupStore, ITextFileStore
    {
        private int _creationSequence;

        public Dictionary<string, List<string>> Files { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, DateTimeOffset> CreationTimes { get; } =
            new(StringComparer.Ordinal);

        public HashSet<string> Directories { get; } =
            new(StringComparer.Ordinal);

        public List<(string Source, string Destination)> CopyOperations { get; } = [];

        public List<string> WriteOperations { get; } = [];

        public List<string> DeletedPaths { get; } = [];

        public void AddFile(
            string path,
            IEnumerable<string> lines,
            DateTimeOffset createdAt)
        {
            Files[path] = lines.ToList();
            CreationTimes[path] = createdAt;
        }

        public bool FileExists(string path) => Files.ContainsKey(path);

        public bool DirectoryExists(string path) => Directories.Contains(path);

        public string GetFileName(string path) => Path.GetFileName(path);

        public string Combine(string directory, string fileName) =>
            Path.Combine(directory, fileName);

        public Task CopyAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CopyOperations.Add((sourcePath, destinationPath));
            Files[destinationPath] = Files[sourcePath].ToList();
            CreationTimes[destinationPath] =
                Now.AddMinutes(++_creationSequence);
            return Task.CompletedTask;
        }

        public Task WriteLinesAsync(
            string destinationPath,
            IReadOnlyList<string> lines,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteOperations.Add(destinationPath);
            Files[destinationPath] = lines.ToList();
            CreationTimes[destinationPath] =
                Now.AddMinutes(++_creationSequence);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StoredBeatmapBackup>> ListAsync(
            string directory,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string prefix = directory + Path.DirectorySeparatorChar;
            IReadOnlyList<StoredBeatmapBackup> result = Files.Keys
                .Where(path => path.StartsWith(prefix, StringComparison.Ordinal))
                .OrderByDescending(path => CreationTimes[path])
                .ThenByDescending(path => path, StringComparer.Ordinal)
                .Select(path => new StoredBeatmapBackup(path, CreationTimes[path]))
                .ToArray();
            return Task.FromResult(result);
        }

        public Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeletedPaths.Add(path);
            Files.Remove(path);
            CreationTimes.Remove(path);
            return Task.CompletedTask;
        }

        public IReadOnlyList<string> ReadAllLines(string path) =>
            Files[path].ToList();

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            Files[path] = lines.ToList();
        }

        public void Delete(string path)
        {
            Files.Remove(path);
            CreationTimes.Remove(path);
        }

        public string GetParentFolder(string path) =>
            Path.GetDirectoryName(path)!;

        public string CombinePath(string parent, string child) =>
            Path.Combine(parent, child);
    }
}
