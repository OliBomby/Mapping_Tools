using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Coordinates current-map lookup, live-state loading, mandatory backup, and user notification.
/// </summary>
public sealed class BetterSaveService : IBetterSaveService
{
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IUserNotificationService notifications;

    /// <summary>
    ///     Creates BetterSave over the shared current-map, editing, and notification boundaries.
    /// </summary>
    /// <param name="currentBeatmapLocator">Finds the beatmap currently open in osu!.</param>
    /// <param name="editingGateway">Requires live state and enforces backup-before-save.</param>
    /// <param name="notifications">Reports completion and captured failures.</param>
    public BetterSaveService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBeatmapEditingGateway editingGateway,
        IUserNotificationService notifications)
    {
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
        this.notifications = notifications
                             ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <inheritdoc />
    public async Task<BetterSaveResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        string? path = null;
        try
        {
            path = await currentBeatmapLocator
                .FindCurrentBeatmapAsync(cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(path))
            {
                await PublishAsync(
                    UserNotificationSeverity.Warning,
                    "BetterSave",
                    "BetterSave could not determine the beatmap open in osu!.");
                return new BetterSaveResult(BetterSaveStatus.NoCurrentBeatmap);
            }

            var session = await editingGateway
                .OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.RequireLive,
                    cancellationToken)
                .ConfigureAwait(false);
            await editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await PublishAsync(
                UserNotificationSeverity.Success,
                "BetterSave",
                "The current beatmap was saved successfully.");
            return new BetterSaveResult(BetterSaveStatus.Saved, path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await PublishAsync(
                UserNotificationSeverity.Error,
                "BetterSave",
                exception.Message,
                exception);
            return new BetterSaveResult(BetterSaveStatus.Failed, path, exception);
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
            await notifications.PublishAsync(new UserNotification(
                severity,
                title,
                message,
                exception));
        }
        catch
        {
            // Presentation failures cannot change the save outcome.
        }
    }
}
