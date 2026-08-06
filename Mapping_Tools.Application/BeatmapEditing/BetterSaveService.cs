using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
/// Distinguishes a completed BetterSave from missing editor state and a captured failure.
/// </summary>
public enum BetterSaveStatus
{
    /// <summary>The current live editor document was backed up and saved.</summary>
    Saved,

    /// <summary>osu! did not expose a current beatmap path.</summary>
    NoCurrentBeatmap,

    /// <summary>Opening live state, creating the backup, or saving failed.</summary>
    Failed
}

/// <summary>
/// Reports a BetterSave attempt without requiring hotkey or watcher callbacks to present UI.
/// </summary>
/// <param name="Status">Whether the document was saved or why it was not.</param>
/// <param name="Path">The current beatmap path when lookup succeeded.</param>
/// <param name="Exception">The captured failure retained for diagnostics.</param>
public sealed record BetterSaveResult(
    BetterSaveStatus Status,
    string? Path = null,
    Exception? Exception = null);

/// <summary>
/// Saves the exact live osu! editor state through the mandatory backup gateway.
/// </summary>
public interface IBetterSaveService
{
    /// <summary>
    /// Locates the current beatmap, requires matching live editor state, and saves it safely.
    /// </summary>
    /// <param name="cancellationToken">Cancels lookup, live reading, backup, or persistence.</param>
    /// <returns>A typed outcome; ordinary integration and persistence failures are captured.</returns>
    Task<BetterSaveResult> ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Controls the platform watcher that replaces focused osu! saves with BetterSave output.
/// </summary>
public interface IBetterSaveOverrideService
{
    /// <summary>
    /// Reconfigures recursive beatmap observation after a path or enabled preference changes.
    /// </summary>
    /// <param name="songsPath">The osu! beatmap-library root to observe.</param>
    /// <param name="enabled">Whether matching saves should invoke BetterSave.</param>
    void Configure(string songsPath, bool enabled);

    /// <summary>Stops observation and releases platform watcher resources.</summary>
    void Stop();
}

/// <summary>
/// Coordinates current-map lookup, live-state loading, mandatory backup, and user notification.
/// </summary>
public sealed class BetterSaveService : IBetterSaveService
{
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IUserNotificationService _notifications;

    /// <summary>
    /// Creates BetterSave over the shared current-map, editing, and notification boundaries.
    /// </summary>
    /// <param name="currentBeatmapLocator">Finds the beatmap currently open in osu!.</param>
    /// <param name="editingGateway">Requires live state and enforces backup-before-save.</param>
    /// <param name="notifications">Reports completion and captured failures.</param>
    public BetterSaveService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBeatmapEditingGateway editingGateway,
        IUserNotificationService notifications)
    {
        _currentBeatmapLocator = currentBeatmapLocator
            ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _editingGateway = editingGateway
            ?? throw new ArgumentNullException(nameof(editingGateway));
        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <inheritdoc/>
    public async Task<BetterSaveResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        string? path = null;
        try
        {
            path = await _currentBeatmapLocator
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

            BeatmapEditingSession session = await _editingGateway
                .OpenBeatmapAsync(
                    path,
                    LiveBeatmapPreference.RequireLive,
                    cancellationToken)
                .ConfigureAwait(false);
            await _editingGateway
                .SaveAsync(session.Editor, cancellationToken: cancellationToken)
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
            await _notifications.PublishAsync(new UserNotification(
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
