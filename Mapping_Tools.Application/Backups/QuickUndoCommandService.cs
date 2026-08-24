using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.Backups;

/// <summary>
///     Coordinates current-map discovery, newest-backup restore, optional editor
///     reload, and user notification independently of its invocation surface.
/// </summary>
public sealed class QuickUndoCommandService : IQuickUndoCommandService
{
    private readonly IBeatmapBackupService backupService;
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly IUserNotificationService notifications;
    private readonly ApplicationSettings settings;

    /// <summary>
    ///     Creates the command over the shared live-map, backup, settings, and
    ///     notification boundaries.
    /// </summary>
    /// <param name="currentBeatmapLocator">Finds the destination currently open in osu!.</param>
    /// <param name="backupService">Selects and safely applies the newest retained snapshot.</param>
    /// <param name="settings">Determines whether a successful restore reloads osu!.</param>
    /// <param name="notifications">Reports non-success and completion outcomes to the active frontend.</param>
    public QuickUndoCommandService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBeatmapBackupService backupService,
        ApplicationSettings settings,
        IUserNotificationService notifications)
    {
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.backupService = backupService
                             ?? throw new ArgumentNullException(nameof(backupService));
        this.settings = settings
                        ?? throw new ArgumentNullException(nameof(settings));
        this.notifications = notifications
                             ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <inheritdoc />
    public async Task<QuickUndoCommandResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? path = await currentBeatmapLocator
                .FindCurrentBeatmapAsync(cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(path))
            {
                await PublishAsync(
                        UserNotificationSeverity.Warning,
                        "QuickUndo",
                        "QuickUndo could not determine the beatmap open in osu!.")
                    .ConfigureAwait(false);
                return new QuickUndoCommandResult(
                    QuickUndoCommandStatus.NoCurrentBeatmap);
            }

            var restore = await backupService
                .QuickUndoAsync(
                    path,
                    reloadEditor: settings.AutoReload,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (restore is null)
            {
                await PublishAsync(
                        UserNotificationSeverity.Warning,
                        "QuickUndo",
                        "No retained backup is available to restore.")
                    .ConfigureAwait(false);
                return new QuickUndoCommandResult(
                    QuickUndoCommandStatus.NoBackup);
            }

            await PublishAsync(
                    UserNotificationSeverity.Success,
                    "QuickUndo",
                    "The newest backup was restored successfully.")
                .ConfigureAwait(false);
            return new QuickUndoCommandResult(
                QuickUndoCommandStatus.Restored,
                restore);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await PublishAsync(
                    UserNotificationSeverity.Error,
                    "QuickUndo",
                    exception.Message,
                    exception)
                .ConfigureAwait(false);
            return new QuickUndoCommandResult(
                QuickUndoCommandStatus.Failed,
                Exception: exception);
        }
    }

    private async Task PublishAsync(
        UserNotificationSeverity severity,
        string title,
        string message,
        Exception? exception = null)
    {
        try
        {
            await notifications.PublishAsync(
                    new UserNotification(severity, title, message, exception),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Presentation failures cannot change the restore outcome.
        }
    }
}
