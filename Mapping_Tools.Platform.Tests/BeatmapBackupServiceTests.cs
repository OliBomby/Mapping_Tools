using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.SafetyCopies;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.Classes.BeatmapHelper;
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
    public async Task CreateAsync_UserBackup_UsesLegacyNameAndPrunesOldestUnprotectedFile()
    {
        // Arrange
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

        // Act
        string expected = Path.Combine(
            BackupDirectory,
            "2026-07-25 14-05-06_UB__map.osu");
        // Assert
        result.Artifacts.Single().Path.Should().Be(expected);
        store.Files.ContainsKey(expected).Should().BeTrue();
        store.Files.ContainsKey(
            Path.Combine(BackupDirectory, "old-1.osu")).Should().BeFalse();
        store.Files.ContainsKey(
            Path.Combine(BackupDirectory, "old-2.osu")).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_AutomaticBackup_RespectsPreferenceUnlessDestructive()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        ApplicationSettings settings = CreateSettings();
        settings.MakeBackups = false;
        BeatmapBackupService service = CreateService(store, settings);

        // Act
        BeatmapBackupResult skipped = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic);
        BeatmapBackupResult forced = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);

        // Assert
        skipped.SkippedByPreference.Should().BeTrue();
        skipped.Artifacts.Count.Should().Be(0);
        forced.SkippedByPreference.Should().BeFalse();
        forced.Artifacts.Count.Should().Be(1);
    }

    [TestMethod]
    public async Task CreateAsync_SameSecondBackups_DoesNotOverwriteEarlierCopy()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        BeatmapBackupService service = CreateService(store, CreateSettings());

        // Act
        BeatmapBackupResult first = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);
        BeatmapBackupResult second = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);

        // Assert
        second.Artifacts.Single().Path.Should().NotBe(first.Artifacts.Single().Path);
        second.Artifacts.Single().Path.Should().Contain("_C2_map.osu");
        store.Files.ContainsKey(first.Artifacts.Single().Path).Should().BeTrue();
        store.Files.ContainsKey(second.Artifacts.Single().Path).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_WithMissingDirectory_ThrowsBeforeCopy()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        store.Directories.Clear();
        BeatmapBackupService service = CreateService(store, CreateSettings());

        // Act
        Func<Task> act1 = () => service.CreateAsync(
                [MapPath],
                BeatmapBackupReason.Automatic,
                force: true);

        // Assert
        await act1.Should().ThrowAsync<DirectoryNotFoundException>();

        store.CopyOperations.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task CreateAsync_WithLiveSession_CreatesDiskAndUnsavedSnapshots()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        BeatmapEditor2 editor = new(MapPath, store);
        editor.Beatmap.Metadata["Version"] = new TValue("Unsaved");
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.LiveEditor,
            []);
        editor.Beatmap.Metadata["Version"] = new TValue("AfterToolRun");
        BeatmapBackupService service = CreateService(store, CreateSettings());

        // Act
        BeatmapBackupResult result = await service.CreateAsync(
            session,
            BeatmapBackupReason.User,
            force: true);

        // Assert
        result.Artifacts.Count.Should().Be(2);
        result.Artifacts[0].ContainsUnsavedState.Should().BeFalse();
        result.Artifacts[1].ContainsUnsavedState.Should().BeTrue();
        result.Artifacts[0].Path.Should().Contain("_UB__map.osu");
        result.Artifacts[1].Path.Should().Contain("_UB_2_map.osu");
        store.Files[result.Artifacts[0].Path]
            .Any(line => line == "Version:Unsaved").Should().BeFalse();
        store.Files[result.Artifacts[1].Path]
            .Any(line => line == "Version:Unsaved").Should().BeTrue();
        store.Files[result.Artifacts[1].Path]
            .Any(line => line == "Version:AfterToolRun").Should().BeFalse();
    }

    [TestMethod]
    public async Task CreateAsync_WithLiveSessionMatchingDisk_CreatesOnlyDirectCopy()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        BeatmapEditor2 editor = new(MapPath, store);
        store.Files[MapPath] = editor.Beatmap.GetLines();
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.LiveEditor,
            []);
        BeatmapBackupService service = CreateService(store, CreateSettings());

        // Act
        BeatmapBackupResult result = await service.CreateAsync(
            session,
            BeatmapBackupReason.Automatic,
            force: true);

        // Assert
        result.Artifacts.Should().ContainSingle();
        result.Artifacts[0].ContainsUnsavedState.Should().BeFalse();
        store.WriteOperations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CreatePeriodicIfChangedAsync_WithMultipleMaps_SkipsUnchangedAndTracksSeparately()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        BeatmapEditor2 editor = new(MapPath, store);
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.Disk,
            []);
        BeatmapBackupService service = CreateService(store, CreateSettings());

        // Act
        BeatmapBackupArtifact? first =
            await service.CreatePeriodicIfChangedAsync(session);
        BeatmapBackupArtifact? unchanged =
            await service.CreatePeriodicIfChangedAsync(session);
        editor.Beatmap.Metadata["Version"] = new TValue("Changed");
        BeatmapBackupArtifact? changed =
            await service.CreatePeriodicIfChangedAsync(session);

        // Assert
        first.Should().NotBeNull();
        unchanged.Should().BeNull();
        changed.Should().NotBeNull();
        first.Path.Should().Contain("_PB__map.osu");
        store.WriteOperations.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task RestoreAsync_WithIncompatibleBackup_DoesNotBackupOrOverwrite()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        string incompatible = Path.Combine(BackupDirectory, "other.osu");
        store.AddFile(
            incompatible,
            ChangeDifficulty(store.Files[MapPath], "Other"),
            Now.AddMinutes(-1));
        List<string> original = store.Files[MapPath].ToList();
        BeatmapBackupService service = CreateService(store, CreateSettings());

        // Act
        Func<Task> act2 = () => service.RestoreAsync(incompatible, MapPath);

        // Assert
        BeatmapBackupIncompatibleException exception = (await act2.Should().ThrowAsync<BeatmapBackupIncompatibleException>()).Which;

        exception.BackupFileName.Should().Contain("[Other]");
        store.Files[MapPath].Should().Equal(original);
        store.CopyOperations.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task RestoreAsync_WithCompatibleBackup_PreservesDestinationAndReloadsLast()
    {
        // Arrange
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

        // Act
        BeatmapRestoreResult result = await service.RestoreAsync(
            backup,
            MapPath,
            reloadEditor: true);

        // Assert
        result.SafetyBackup.Path.Contains("_RU__map.osu").Should().BeTrue();
        store.Files.ContainsKey(backup).Should().BeTrue();
        store.CopyOperations.Count.Should().Be(2);
        store.CopyOperations[1].Destination.Should().Be(MapPath);
        store.Files[MapPath].Contains("PreviewTime:9876").Should().BeTrue();
        reload.ReloadCount.Should().Be(1);
        reload.ReloadFollowedRestore.Should().BeTrue();
    }

    [TestMethod]
    public async Task QuickUndoAsync_WithMultipleBackups_SelectsNewestBeforeSafetyCopy()
    {
        // Arrange
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

        // Act
        BeatmapRestoreResult? result = await service.QuickUndoAsync(MapPath);

        // Assert
        result.Should().NotBeNull();
        result.BackupPath.Should().Be(newer);
        store.Files[MapPath].Contains("PreviewTime:2222").Should().BeTrue();
        store.Files.ContainsKey(result.SafetyBackup.Path).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_WhenRetentionRuns_PreservesCurrentSafetyCopy()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        ApplicationSettings settings = CreateSettings();
        settings.MaxBackupFiles = 0;
        BeatmapBackupService service = CreateService(store, settings);

        // Act
        BeatmapBackupResult result = await service.CreateAsync(
            [MapPath],
            BeatmapBackupReason.Automatic,
            force: true);

        // Assert
        store.Files.ContainsKey(result.Artifacts.Single().Path).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_WithPreCancelledToken_DoesNotCreateOrDeleteFiles()
    {
        // Arrange
        MemoryBackupStore store = CreateStore();
        BeatmapBackupService service = CreateService(store, CreateSettings());
        using CancellationTokenSource source = new();
        source.Cancel();

        // Act
        Func<Task> act3 = () => service.CreateAsync(
                [MapPath],
                BeatmapBackupReason.Automatic,
                force: true,
                source.Token);

        // Assert
        await act3.Should().ThrowAsync<OperationCanceledException>();

        store.CopyOperations.Count.Should().Be(0);
        store.DeletedPaths.Count.Should().Be(0);
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
