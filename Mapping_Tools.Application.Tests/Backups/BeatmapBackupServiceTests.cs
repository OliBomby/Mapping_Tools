using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Core.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Backups;

[TestClass]
public sealed class BeatmapBackupServiceTests
{
    private const string backup_directory = @"C:\Backups";
    private const string map_path = @"C:\osu!\Songs\set\map.osu";

    private static readonly DateTimeOffset now =
        new(2026, 7, 25, 14, 5, 6, TimeSpan.FromHours(2));

    [TestMethod]
    public async Task CreateAsync_UserBackup_UsesLegacyNameAndPrunesOldestUnprotectedFile()
    {
        // Arrange
        var store = CreateStore();
        store.AddFile(
            Path.Combine(backup_directory, "old-1.osu"),
            ["old"],
            DateTimeOffset.UnixEpoch);
        store.AddFile(
            Path.Combine(backup_directory, "old-2.osu"),
            ["old"],
            DateTimeOffset.UnixEpoch.AddMinutes(1));
        var settings = CreateSettings();
        settings.MaxBackupFiles = 2;
        var service = CreateService(store, settings);

        var result = await service.CreateAsync(
            [map_path],
            BeatmapBackupReason.User,
            true);

        // Act
        string expected = Path.Combine(
            backup_directory,
            "2026-07-25 14-05-06_UB__map.osu");
        // Assert
        result.Artifacts.Single().Path.Should().Be(expected);
        store.Files.ContainsKey(expected).Should().BeTrue();
        store.Files.ContainsKey(
            Path.Combine(backup_directory, "old-1.osu")).Should().BeFalse();
        store.Files.ContainsKey(
            Path.Combine(backup_directory, "old-2.osu")).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_AutomaticBackup_RespectsPreferenceUnlessDestructive()
    {
        // Arrange
        var store = CreateStore();
        var settings = CreateSettings();
        settings.MakeBackups = false;
        var service = CreateService(store, settings);

        // Act
        var skipped = await service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic);
        var forced = await service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic,
            true);

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
        var store = CreateStore();
        var service = CreateService(store, CreateSettings());

        // Act
        var first = await service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic,
            true);
        var second = await service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic,
            true);

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
        var store = CreateStore();
        store.Directories.Clear();
        var service = CreateService(store, CreateSettings());

        // Act
        Func<Task> act1 = () => service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic,
            true);

        // Assert
        await act1.Should().ThrowAsync<DirectoryNotFoundException>();

        store.CopyOperations.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task CreateAsync_WithLiveSession_CreatesDiskAndUnsavedSnapshots()
    {
        // Arrange
        var store = CreateStore();
        BeatmapEditor editor = new(map_path, store);
        editor.Beatmap.Metadata["Version"] = new StringValue("Unsaved");
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.LiveEditor,
            []);
        editor.Beatmap.Metadata["Version"] = new StringValue("AfterToolRun");
        var service = CreateService(store, CreateSettings());

        // Act
        var result = await service.CreateAsync(
            session,
            BeatmapBackupReason.User,
            true);

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
        var store = CreateStore();
        BeatmapEditor editor = new(map_path, store);
        store.Files[map_path] = editor.Beatmap.GetLines();
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.LiveEditor,
            []);
        var service = CreateService(store, CreateSettings());

        // Act
        var result = await service.CreateAsync(
            session,
            BeatmapBackupReason.Automatic,
            true);

        // Assert
        result.Artifacts.Should().ContainSingle();
        result.Artifacts[0].ContainsUnsavedState.Should().BeFalse();
        store.WriteOperations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CreatePeriodicIfChangedAsync_WithMultipleMaps_SkipsUnchangedAndTracksSeparately()
    {
        // Arrange
        var store = CreateStore();
        BeatmapEditor editor = new(map_path, store);
        BeatmapEditingSession session = new(
            editor,
            BeatmapEditingSource.Disk,
            []);
        var service = CreateService(store, CreateSettings());

        // Act
        var first =
            await service.CreatePeriodicIfChangedAsync(session);
        var unchanged =
            await service.CreatePeriodicIfChangedAsync(session);
        editor.Beatmap.Metadata["Version"] = new StringValue("Changed");
        var changed =
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
        var store = CreateStore();
        string incompatible = Path.Combine(backup_directory, "other.osu");
        store.AddFile(
            incompatible,
            ChangeDifficulty(store.Files[map_path], "Other"),
            now.AddMinutes(-1));
        var original = store.Files[map_path].ToList();
        var service = CreateService(store, CreateSettings());

        // Act
        Func<Task> act2 = () => service.RestoreAsync(incompatible, map_path);

        // Assert
        var exception = (await act2.Should().ThrowAsync<BeatmapBackupIncompatibleException>()).Which;

        exception.BackupFileName.Should().Contain("[Other]");
        store.Files[map_path].Should().Equal(original);
        store.CopyOperations.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task RestoreAsync_WithCompatibleBackup_PreservesDestinationAndReloadsLast()
    {
        // Arrange
        var store = CreateStore();
        string backup = Path.Combine(backup_directory, "chosen.osu");
        var restoredLines = store.Files[map_path].ToList();
        int previewIndex = restoredLines.FindIndex(line => line.StartsWith("PreviewTime:", StringComparison.Ordinal));
        restoredLines[previewIndex] = "PreviewTime:9876";
        store.AddFile(backup, restoredLines, now.AddMinutes(-1));
        RecordingEditorReloadService reload = new()
        {
            FileWrittenResolver = () => store.CopyOperations.Count >= 2,
        };
        var settings = CreateSettings();
        settings.MaxBackupFiles = 1;
        var service = CreateService(
            store,
            settings,
            reload);

        // Act
        var result = await service.RestoreAsync(
            backup,
            map_path,
            reloadEditor: true);

        // Assert
        result.SafetyBackup.Path.Contains("_RU__map.osu").Should().BeTrue();
        store.Files.ContainsKey(backup).Should().BeTrue();
        store.CopyOperations.Count.Should().Be(2);
        store.CopyOperations[1].Destination.Should().Be(map_path);
        store.Files[map_path].Contains("PreviewTime:9876").Should().BeTrue();
        reload.ReloadCount.Should().Be(1);
        reload.FileHadBeenWritten.Should().BeTrue();
    }

    [TestMethod]
    public async Task QuickUndoAsync_WithMultipleBackups_SelectsNewestBeforeSafetyCopy()
    {
        // Arrange
        var store = CreateStore();
        string older = Path.Combine(backup_directory, "older.osu");
        string newer = Path.Combine(backup_directory, "newer.osu");
        var olderLines = store.Files[map_path].ToList();
        var newerLines = store.Files[map_path].ToList();
        int olderPreview = olderLines.FindIndex(line => line.StartsWith("PreviewTime:", StringComparison.Ordinal));
        int newerPreview = newerLines.FindIndex(line => line.StartsWith("PreviewTime:", StringComparison.Ordinal));
        olderLines[olderPreview] = "PreviewTime:1111";
        newerLines[newerPreview] = "PreviewTime:2222";
        store.AddFile(older, olderLines, now.AddMinutes(-2));
        store.AddFile(newer, newerLines, now.AddMinutes(-1));
        var service = CreateService(store, CreateSettings());

        // Act
        var result = await service.QuickUndoAsync(map_path);

        // Assert
        result.Should().NotBeNull();
        result.BackupPath.Should().Be(newer);
        store.Files[map_path].Contains("PreviewTime:2222").Should().BeTrue();
        store.Files.ContainsKey(result.SafetyBackup.Path).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_WhenRetentionRuns_PreservesCurrentSafetyCopy()
    {
        // Arrange
        var store = CreateStore();
        var settings = CreateSettings();
        settings.MaxBackupFiles = 0;
        var service = CreateService(store, settings);

        // Act
        var result = await service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic,
            true);

        // Assert
        store.Files.ContainsKey(result.Artifacts.Single().Path).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateAsync_WithPreCancelledToken_DoesNotCreateOrDeleteFiles()
    {
        // Arrange
        var store = CreateStore();
        var service = CreateService(store, CreateSettings());
        using CancellationTokenSource source = new();
        source.Cancel();

        // Act
        Func<Task> act3 = () => service.CreateAsync(
            [map_path],
            BeatmapBackupReason.Automatic,
            true,
            source.Token);

        // Assert
        await act3.Should().ThrowAsync<OperationCanceledException>();

        store.CopyOperations.Count.Should().Be(0);
        store.DeletedPaths.Count.Should().Be(0);
    }

    private static BeatmapBackupService CreateService(
        MemoryBackupStore store,
        ApplicationSettings settings,
        RecordingEditorReloadService? reload = null)
    {
        return new BeatmapBackupService(
            store,
            store,
            reload ?? new RecordingEditorReloadService
            {
                FileWrittenResolver = () => store.CopyOperations.Count >= 2,
            },
            settings,
            new FixedTimeProvider(now));
    }

    private static ApplicationSettings CreateSettings()
    {
        return new ApplicationSettings
        {
            BackupsPath = backup_directory,
            MakeBackups = true,
            MakePeriodicBackups = true,
            MaxBackupFiles = 1000,
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
        store.Directories.Add(backup_directory);
        store.AddFile(map_path, File.ReadAllLines(fixture), now.AddHours(-1));
        return store;
    }

    private static List<string> ChangeDifficulty(
        IEnumerable<string> lines,
        string difficulty)
    {
        var changed = lines.ToList();
        int index = changed.FindIndex(line => line.StartsWith("Version:", StringComparison.Ordinal));
        changed[index] = $"Version:{difficulty}";
        return changed;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            this.now = now;
        }

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone(
                "Test",
                now.Offset,
                "Test",
                "Test");

        public override DateTimeOffset GetUtcNow()
        {
            return now.ToUniversalTime();
        }
    }

    private sealed class MemoryBackupStore : IBeatmapBackupStore, ITextFileStore
    {
        private int creationSequence;

        public Dictionary<string, List<string>> Files { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, DateTimeOffset> CreationTimes { get; } =
            new(StringComparer.Ordinal);

        public HashSet<string> Directories { get; } =
            new(StringComparer.Ordinal);

        public List<(string Source, string Destination)> CopyOperations { get; } = [];

        public List<string> WriteOperations { get; } = [];

        public List<string> DeletedPaths { get; } = [];

        public bool FileExists(string path)
        {
            return Files.ContainsKey(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directories.Contains(path);
        }

        public string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public string Combine(string directory, string fileName)
        {
            return Path.Combine(directory, fileName);
        }

        public Task CopyAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CopyOperations.Add((sourcePath, destinationPath));
            Files[destinationPath] = Files[sourcePath].ToList();
            CreationTimes[destinationPath] =
                now.AddMinutes(++creationSequence);
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
                now.AddMinutes(++creationSequence);
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

        public IReadOnlyList<string> ReadAllLines(string path)
        {
            return Files[path].ToList();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            Files[path] = lines.ToList();
        }

        public void Delete(string path)
        {
            Files.Remove(path);
            CreationTimes.Remove(path);
        }

        public string GetParentFolder(string path)
        {
            return Path.GetDirectoryName(path)!;
        }

        public string CombinePath(string parent, string child)
        {
            return Path.Combine(parent, child);
        }

        public void AddFile(
            string path,
            IEnumerable<string> lines,
            DateTimeOffset createdAt)
        {
            Files[path] = lines.ToList();
            CreationTimes[path] = createdAt;
        }
    }
}
