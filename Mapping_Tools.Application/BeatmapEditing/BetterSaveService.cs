using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Workspace.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Coordinates current-map lookup, live-state loading, configured backup, and user notification.
/// </summary>
public sealed class BetterSaveService : IBetterSaveService
{
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly ILogger<BetterSaveService> logger;
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IUserNotificationService notifications;

    /// <summary>
    ///     Creates BetterSave over the shared current-map, editing, and notification boundaries.
    /// </summary>
    /// <param name="currentBeatmapLocator">Finds the beatmap currently open in osu!.</param>
    /// <param name="editingGateway">Requires live state and applies the configured backup-before-save policy.</param>
    /// <param name="notifications">Reports completion and captured failures.</param>
    /// <param name="logger">Records save stages and failures.</param>
    public BetterSaveService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBeatmapEditingGateway editingGateway,
        IUserNotificationService notifications,
        ILogger<BetterSaveService>? logger = null)
    {
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
        this.notifications = notifications
                             ?? throw new ArgumentNullException(nameof(notifications));
        this.logger = logger ?? NullLogger<BetterSaveService>.Instance;
    }

    /// <inheritdoc />
    public async Task<BetterSaveResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        string? path = null;
        logger.LogInformation("BetterSave started");
        try
        {
            try
            {
                path = await currentBeatmapLocator
                    .FindCurrentBeatmapAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException exception)
            {
                logger.LogWarning(exception, "BetterSave could not resolve current beatmap");
                await PublishAsync(
                    UserNotificationSeverity.Warning,
                    "BetterSave",
                    ApplicationStrings.BetterSave_EditorUnavailable);
                return new BetterSaveResult(BetterSaveStatus.NoCurrentBeatmap);
            }

            logger.LogInformation("BetterSave opening live beatmap {Path}", path);
            var session = await editingGateway
                .OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.RequireLive,
                    cancellationToken)
                .ConfigureAwait(false);
            logger.LogInformation("BetterSave writing beatmap {Path}", path);
            await editingGateway
                .SaveAsync(session, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await PublishAsync(
                UserNotificationSeverity.Success,
                "BetterSave",
                ApplicationStrings.BetterSave_Saved);
            logger.LogInformation("BetterSave completed for {Path}", path);
            return new BetterSaveResult(BetterSaveStatus.Saved, path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("BetterSave cancelled for {Path}", path);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "BetterSave failed for {Path}", path);
            await PublishAsync(
                UserNotificationSeverity.Error,
                "BetterSave",
                ApplicationExceptionText.GetSummary(exception, ApplicationStrings.BetterSave_Failed),
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
