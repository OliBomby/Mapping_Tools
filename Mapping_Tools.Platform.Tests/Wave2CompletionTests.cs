using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.QuickRun;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class Wave2CompletionTests
{
    private const string MapPath =
        @"C:\osu!\Songs\123 Artist - Title\map.osu";

    [TestMethod]
    public async Task HeadlessQuickRunSelectsLoadsEditsBacksUpSavesAndReports()
    {
        ApplicationSettings settings = new()
        {
            SmartQuickRunEnabled = false,
            UseEditorReader = false,
            AutoReload = false
        };
        MemoryTextFileStore store = CreateStore();
        RecordingBackupService backups = new(store);
        NullLiveReader liveReader = new();
        BeatmapEditingGateway gateway = new(
            store,
            backups,
            liveReader,
            new NullReloadService(),
            settings);
        BeatmapWorkspace workspace = new(
            settings,
            new UnusedFilePicker(),
            new ExistingMapFileSystem(),
            new FixedCurrentBeatmapLocator(),
            TimeProvider.System);
        CurrentBeatmapSelectionResult selection =
            await workspace.SelectCurrentBeatmapAsync();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        ToolExecutionService execution = new(
            notifications,
            new NullReloadService(),
            settings,
            TimeProvider.System);
        ToolExecutionResult<string>? toolResult = null;
        QuickRunCommandRegistry registry = new();
        registry.Register(
            new QuickRunCommand(
                "wave2-acceptance",
                "Wave 2 acceptance",
                QuickRunTargets.Always,
                async cancellationToken =>
                {
                    ToolExecutionRequest<string> request = new(
                        "wave2-acceptance",
                        "Wave 2 acceptance",
                        async context =>
                        {
                            string path = workspace.SelectedPaths.Single();
                            BeatmapEditingSession session = await gateway
                                .OpenBeatmapAsync(
                                    path,
                                    LiveBeatmapPreference.DiskOnly,
                                    context.CancellationToken);
                            session.Editor.Beatmap.Metadata["Version"] =
                                new TValue("Wave 2 validated");
                            await gateway.SaveAsync(
                                session.Editor,
                                cancellationToken: context.CancellationToken);
                            return new ToolExecutionOutput<string>(
                                path,
                                "Wave 2 headless workflow completed.");
                        });
                    toolResult = await execution.ExecuteAsync(
                        request,
                        cancellationToken: cancellationToken);
                }));
        registry.SelectCurrent("wave2-acceptance");
        QuickRunService quickRun = new(
            registry,
            liveReader,
            settings,
            notifications);

        QuickRunResult quickRunResult = await quickRun.RunAsync();

        Assert.AreEqual(CurrentBeatmapSelectionStatus.Selected, selection.Status);
        CollectionAssert.AreEqual(
            new[] { MapPath },
            workspace.SelectedPaths.ToArray());
        Assert.AreEqual(QuickRunStatus.Executed, quickRunResult.Status);
        Assert.IsNotNull(toolResult);
        Assert.AreEqual(ToolExecutionStatus.Succeeded, toolResult.Status);
        Assert.AreEqual(MapPath, toolResult.Value);
        Assert.AreEqual(1, backups.CreateCount);
        Assert.IsTrue(backups.BackupPrecededWrite);
        Assert.IsTrue(store.Files[MapPath].Contains("Version:Wave 2 validated"));
        Assert.AreEqual(1, published.Count);
        Assert.AreEqual(UserNotificationSeverity.Success, published[0].Severity);
    }

    private static MemoryTextFileStore CreateStore()
    {
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        return new MemoryTextFileStore(MapPath, File.ReadAllLines(fixture));
    }

    private sealed class MemoryTextFileStore : ITextFileStore
    {
        public MemoryTextFileStore(string path, IEnumerable<string> lines)
        {
            Files[path] = lines.ToList();
        }

        public Dictionary<string, List<string>> Files { get; } =
            new(StringComparer.Ordinal);

        public int WriteCount { get; private set; }

        public IReadOnlyList<string> ReadAllLines(string path) =>
            Files[path].ToList();

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            WriteCount++;
            Files[path] = lines.ToList();
        }

        public void Delete(string path) => Files.Remove(path);

        public string GetParentFolder(string path) =>
            Path.GetDirectoryName(path)!;

        public string CombinePath(string parent, string child) =>
            Path.Combine(parent, child);
    }

    private sealed class RecordingBackupService : IBeatmapBackupService
    {
        private readonly MemoryTextFileStore _store;

        public RecordingBackupService(MemoryTextFileStore store)
        {
            _store = store;
        }

        public int CreateCount { get; private set; }

        public bool BackupPrecededWrite { get; private set; }

        public Task<BeatmapBackupResult> CreateAsync(
            IEnumerable<string> sourcePaths,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string sourcePath = sourcePaths.Single();
            CreateCount++;
            BackupPrecededWrite = _store.WriteCount == 0;
            BeatmapBackupArtifact artifact = new(
                "backup.osu",
                sourcePath,
                reason,
                false,
                DateTimeOffset.UnixEpoch);
            return Task.FromResult(new BeatmapBackupResult([artifact], false));
        }

        public Task<BeatmapBackupResult> CreateAsync(
            BeatmapEditingSession session,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
            BeatmapEditingSession session,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BeatmapRestoreResult> RestoreAsync(
            string backupPath,
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BeatmapRestoreResult?> QuickUndoAsync(
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NullLiveReader : ILiveBeatmapReader
    {
        public Task<LiveBeatmapSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<LiveBeatmapSnapshot?>(null);
        }
    }

    private sealed class NullReloadService : IEditorReloadService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<string?>(MapPath);
        }
    }

    private sealed class ExistingMapFileSystem : IBeatmapFileSystem
    {
        public bool FileExists(string path) =>
            string.Equals(path, MapPath, StringComparison.Ordinal);

        public string? GetParentDirectory(string filePath) =>
            Path.GetDirectoryName(filePath);
    }

    private sealed class UnusedFilePicker : IFilePicker
    {
        public bool CanOpenFiles => false;

        public bool CanSaveFiles => false;

        public bool CanPickFolders => false;

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string?> PickSaveFileAsync(
            SaveFilePickerRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> PickFoldersAsync(
            OpenFolderPickerRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
