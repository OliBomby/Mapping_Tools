using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tests.TestDoubles;
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
        RecordingLiveBeatmapReader liveReader = new((LiveBeatmapSnapshot?)null);
        BeatmapEditingGateway gateway = new(
            store,
            backups,
            liveReader,
            new RecordingEditorReloadService(),
            settings);
        BeatmapWorkspace workspace = new(
            settings,
            new RecordingFilePicker
            {
                CanOpenFiles = false,
                CanSaveFiles = false,
                CanPickFolders = false,
            },
            new RecordingBeatmapFileSystem
            {
                FileExistsResolver = path => string.Equals(path, map_path, StringComparison.Ordinal),
                ParentDirectoryResolver = Path.GetDirectoryName,
            },
            new RecordingCurrentBeatmapLocator(map_path),
            TimeProvider.System);
        var selection =
            await workspace.SelectCurrentBeatmapAsync();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        ToolExecutionService execution = new(
            notifications,
            new RecordingEditorReloadService(),
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

    private static RecordingTextFileStore CreateStore()
    {
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        return new RecordingTextFileStore(map_path, File.ReadAllLines(fixture));
    }

    private sealed class RecordingBackupService : IBeatmapBackupService
    {
        private readonly RecordingTextFileStore store;

        public RecordingBackupService(RecordingTextFileStore store)
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

}
