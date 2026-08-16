using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Backups;

[TestClass]
public sealed class QuickUndoCommandServiceTests
{
    [TestMethod]
    public async Task ExecuteAsync_WithoutCurrentMap_StopsBeforeBackupLookup()
    {
        // Arrange
        RecordingBackupService backups = new();
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator(null),
            backups,
            notifications);

        // Act
        QuickUndoCommandResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(QuickUndoCommandStatus.NoCurrentBeatmap);
        backups.QuickUndoCount.Should().Be(0);
        notifications[0].Severity.Should().Be(UserNotificationSeverity.Warning);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithoutBackup_ReturnsWarningAndForwardsReload()
    {
        // Arrange
        RecordingBackupService backups = new();
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator("map.osu"),
            backups,
            notifications,
            autoReload: true);

        // Act
        QuickUndoCommandResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(QuickUndoCommandStatus.NoBackup);
        backups.QuickUndoCount.Should().Be(1);
        backups.ReloadEditor.Should().BeTrue();
        notifications[0].Severity.Should().Be(UserNotificationSeverity.Warning);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithSuccessfulRestore_ReturnsMetadataAndPublishesSuccess()
    {
        // Arrange
        BeatmapBackupArtifact safety = new(
            "safety.osu",
            "map.osu",
            BeatmapBackupReason.RestoreSafety,
            false,
            DateTimeOffset.UnixEpoch);
        BeatmapRestoreResult restore = new(
            "newest.osu",
            "map.osu",
            safety);
        RecordingBackupService backups = new() { Restore = restore };
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator("map.osu"),
            backups,
            notifications);

        // Act
        QuickUndoCommandResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(QuickUndoCommandStatus.Restored);
        result.Restore.Should().BeSameAs(restore);
        notifications[0].Severity.Should().Be(UserNotificationSeverity.Success);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenRestoreFails_CapturesAndPublishesFailure()
    {
        // Arrange
        IOException failure = new("Backup is locked.");
        RecordingBackupService backups = new() { Failure = failure };
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator("map.osu"),
            backups,
            notifications);

        // Act
        QuickUndoCommandResult result = await service.ExecuteAsync();

        // Assert
        result.Status.Should().Be(QuickUndoCommandStatus.Failed);
        result.Exception.Should().BeSameAs(failure);
        notifications[0].Severity.Should().Be(UserNotificationSeverity.Error);
        notifications[0].Exception.Should().BeSameAs(failure);
    }

    private static QuickUndoCommandService CreateService(
        ICurrentBeatmapLocator locator,
        RecordingBackupService backups,
        ICollection<UserNotification> published,
        bool autoReload = false)
    {
        UserNotificationService notifications = new();
        notifications.Published += (_, args) => published.Add(args.Notification);
        return new QuickUndoCommandService(
            locator,
            backups,
            new ApplicationSettings { AutoReload = autoReload },
            notifications);
    }

    private sealed class FixedLocator : ICurrentBeatmapLocator
    {
        private readonly string? _path;

        public FixedLocator(string? path)
        {
            _path = path;
        }

        public Task<string?> FindCurrentBeatmapAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_path);
        }
    }

    private sealed class RecordingBackupService : IBeatmapBackupService
    {
        public int QuickUndoCount { get; private set; }

        public bool ReloadEditor { get; private set; }

        public BeatmapRestoreResult? Restore { get; init; }

        public Exception? Failure { get; init; }

        public Task<BeatmapRestoreResult?> QuickUndoAsync(
            string destinationPath,
            bool allowDifferentFilename = false,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QuickUndoCount++;
            ReloadEditor = reloadEditor;
            return Failure is null
                ? Task.FromResult(Restore)
                : Task.FromException<BeatmapRestoreResult?>(Failure);
        }

        public Task<BeatmapBackupResult> CreateAsync(
            IEnumerable<string> sourcePaths,
            BeatmapBackupReason reason,
            bool force = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
    }
}
