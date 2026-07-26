using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.QuickRun;

/// <summary>
/// Reproduces legacy Smart QuickRun routing while returning explicit outcomes
/// instead of reaching through the WPF dispatcher and active view.
/// </summary>
public sealed class QuickRunService : IQuickRunService
{
    private const string CurrentToolSentinel = "<Current Tool>";
    private readonly QuickRunCommandRegistry _registry;
    private readonly ILiveBeatmapReader _liveReader;
    private readonly ApplicationSettings _settings;
    private readonly IUserNotificationService _notifications;

    /// <summary>
    /// Creates the resolver over the shared command catalog, live editor
    /// boundary, settings instance, and frontend-neutral notification stream.
    /// </summary>
    /// <param name="registry">Supplies current and named commands without view reflection.</param>
    /// <param name="liveReader">Reports selected hit objects when smart routing is enabled.</param>
    /// <param name="settings">Supplies live Smart QuickRun preferences.</param>
    /// <param name="notifications">Reports stale configuration and captured failures.</param>
    public QuickRunService(
        QuickRunCommandRegistry registry,
        ILiveBeatmapReader liveReader,
        ApplicationSettings settings,
        IUserNotificationService notifications)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _liveReader = liveReader ?? throw new ArgumentNullException(nameof(liveReader));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <inheritdoc/>
    public async Task<QuickRunResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        QuickRunCommand? command = null;
        try
        {
            if (!_settings.SmartQuickRunEnabled)
            {
                command = _registry.FindCurrent();
                if (command is null)
                {
                    return new QuickRunResult(QuickRunStatus.NoCurrentCommand);
                }
            }
            else
            {
                LiveBeatmapSnapshot? snapshot = await _liveReader
                    .ReadAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (snapshot is null)
                {
                    await PublishAsync(
                            UserNotificationSeverity.Warning,
                            "QuickRun",
                            "QuickRun could not determine the current osu! editor selection.")
                        .ConfigureAwait(false);
                    return new QuickRunResult(QuickRunStatus.EditorUnavailable);
                }

                string configuredName = GetConfiguredName(
                    snapshot.HitObjects.Count(hitObject => hitObject.IsSelected));
                command = string.Equals(
                    configuredName,
                    CurrentToolSentinel,
                    StringComparison.Ordinal)
                    ? _registry.FindCurrent()
                    : _registry.FindByDisplayName(configuredName);
                if (command is null)
                {
                    QuickRunStatus status = string.Equals(
                        configuredName,
                        CurrentToolSentinel,
                        StringComparison.Ordinal)
                        ? QuickRunStatus.NoCurrentCommand
                        : QuickRunStatus.CommandNotFound;
                    await PublishAsync(
                            UserNotificationSeverity.Warning,
                            "QuickRun",
                            status == QuickRunStatus.NoCurrentCommand
                                ? "The current screen does not provide a QuickRun command."
                                : $"The configured QuickRun tool '{configuredName}' is not available.")
                        .ConfigureAwait(false);
                    return new QuickRunResult(status);
                }
            }

            await command.Execute(cancellationToken).ConfigureAwait(false);
            return new QuickRunResult(QuickRunStatus.Executed, command.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await PublishAsync(
                    UserNotificationSeverity.Error,
                    "QuickRun",
                    exception.Message,
                    exception)
                .ConfigureAwait(false);
            return new QuickRunResult(
                QuickRunStatus.Failed,
                command?.Id,
                exception);
        }
    }

    private string GetConfiguredName(int selectedHitObjectCount)
    {
        if (selectedHitObjectCount <= 0)
        {
            return _settings.NoneQuickRunTool;
        }

        return selectedHitObjectCount == 1
            ? _settings.SingleQuickRunTool
            : _settings.MultipleQuickRunTool;
    }

    private async Task PublishAsync(
        UserNotificationSeverity severity,
        string title,
        string message,
        Exception? exception = null)
    {
        try
        {
            await _notifications.PublishAsync(
                    new UserNotification(severity, title, message, exception),
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // A presentation failure cannot change QuickRun command resolution.
        }
    }
}
