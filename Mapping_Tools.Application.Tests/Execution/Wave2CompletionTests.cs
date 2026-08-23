using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Execution;

[TestClass]
public sealed class Wave2CompletionTests
{
    private const string map_path =
        @"C:\osu!\Songs\123 Artist - Title\map.osu";

    [TestMethod]
    public async Task RunAsync_HeadlessQuickRun_CompletesFullWorkflow()
    {
        // Arrange
        ApplicationSettings settings = new()
        {
            SmartQuickRunEnabled = false,
            UseEditorReader = false,
            AutoReload = false,
        };
        var store = CreateStore();
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
        var selection =
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
                            var session = await gateway
                                .OpenBeatmapAsync(
                                    path,
                                    LiveBeatmapPreference.DiskOnly,
                                    context.CancellationToken);
                            session.Editor.Beatmap.Metadata["Version"] =
                                new StringValue("Wave 2 validated");
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

        // Act
        var quickRunResult = await quickRun.RunAsync();

        // Assert
        selection.Status.Should().Be(CurrentBeatmapSelectionStatus.Selected);
        workspace.SelectedPaths.ToArray().Should().Equal(map_path);
        quickRunResult.Status.Should().Be(QuickRunStatus.Executed);
        toolResult.Should().NotBeNull();
        toolResult.Status.Should().Be(ToolExecutionStatus.Succeeded);
        toolResult.Value.Should().Be(map_path);
        backups.CreateCount.Should().Be(1);
        backups.BackupPrecededWrite.Should().BeTrue();
        store.Files[map_path].Contains("Version:Wave 2 validated").Should().BeTrue();
        published.Count.Should().Be(1);
        published[0].Severity.Should().Be(UserNotificationSeverity.Success);
    }

    private static MemoryTextFileStore CreateStore()
    {
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        return new MemoryTextFileStore(map_path, File.ReadAllLines(fixture));
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

        public IReadOnlyList<string> ReadAllLines(string path)
        {
            return Files[path].ToList();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            WriteCount++;
            Files[path] = lines.ToList();
        }

        public void Delete(string path)
        {
            Files.Remove(path);
        }

        public string GetParentFolder(string path)
        {
            return Path.GetDirectoryName(path)!;
        }

        public string CombinePath(string parent, string child)
        {
            return Path.Combine(parent, child);
        }
    }

    private sealed class RecordingBackupService : IBeatmapBackupService
    {
        private readonly MemoryTextFileStore store;

        public RecordingBackupService(MemoryTextFileStore store)
        {
            this.store = store;
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
            BackupPrecededWrite = store.WriteCount == 0;
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
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BeatmapBackupArtifact?> CreatePeriodicIfChangedAsync(
            BeatmapEditingSession session,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BeatmapRestoreResult> RestoreAsync(
            string backupPath,
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<BeatmapRestoreResult?> QuickUndoAsync(
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
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
            return Task.FromResult<string?>(map_path);
        }
    }

    private sealed class ExistingMapFileSystem : IBeatmapFileSystem
    {
        public bool FileExists(string path)
        {
            return string.Equals(path, map_path, StringComparison.Ordinal);
        }

        public string? GetParentDirectory(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }
    }

    private sealed class UnusedFilePicker : IFilePicker
    {
        public bool CanOpenFiles => false;

        public bool CanSaveFiles => false;

        public bool CanPickFolders => false;

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
