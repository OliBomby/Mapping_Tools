using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class QuickUndoCommandServiceTests
{
    [TestMethod]
    public async Task MissingCurrentMapStopsBeforeBackupLookup()
    {
        RecordingBackupService backups = new();
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator(null),
            backups,
            notifications);

        QuickUndoCommandResult result = await service.ExecuteAsync();

        Assert.AreEqual(
            QuickUndoCommandStatus.NoCurrentBeatmap,
            result.Status);
        Assert.AreEqual(0, backups.QuickUndoCount);
        Assert.AreEqual(UserNotificationSeverity.Warning, notifications[0].Severity);
    }

    [TestMethod]
    public async Task MissingBackupReturnsTypedWarningAndForwardsReloadPreference()
    {
        RecordingBackupService backups = new();
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator("map.osu"),
            backups,
            notifications,
            autoReload: true);

        QuickUndoCommandResult result = await service.ExecuteAsync();

        Assert.AreEqual(QuickUndoCommandStatus.NoBackup, result.Status);
        Assert.AreEqual(1, backups.QuickUndoCount);
        Assert.IsTrue(backups.ReloadEditor);
        Assert.AreEqual(UserNotificationSeverity.Warning, notifications[0].Severity);
    }

    [TestMethod]
    public async Task SuccessfulRestoreReturnsMetadataAndPublishesSuccess()
    {
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

        QuickUndoCommandResult result = await service.ExecuteAsync();

        Assert.AreEqual(QuickUndoCommandStatus.Restored, result.Status);
        Assert.AreSame(restore, result.Restore);
        Assert.AreEqual(UserNotificationSeverity.Success, notifications[0].Severity);
    }

    [TestMethod]
    public async Task RestoreFailureIsCapturedAndPublished()
    {
        IOException failure = new("Backup is locked.");
        RecordingBackupService backups = new() { Failure = failure };
        List<UserNotification> notifications = [];
        QuickUndoCommandService service = CreateService(
            new FixedLocator("map.osu"),
            backups,
            notifications);

        QuickUndoCommandResult result = await service.ExecuteAsync();

        Assert.AreEqual(QuickUndoCommandStatus.Failed, result.Status);
        Assert.AreSame(failure, result.Exception);
        Assert.AreEqual(UserNotificationSeverity.Error, notifications[0].Severity);
        Assert.AreSame(failure, notifications[0].Exception);
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
