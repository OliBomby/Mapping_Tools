using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Application.QuickRun;

/// <summary>
///     Reproduces legacy Smart QuickRun routing while returning explicit outcomes
///     instead of reaching through the WPF dispatcher and active view.
/// </summary>
public sealed class QuickRunService : IQuickRunService
{
    private const string current_tool_sentinel = "<Current Tool>";
    private readonly ILiveBeatmapReader liveReader;
    private readonly ILogger<QuickRunService> logger;
    private readonly IUserNotificationService notifications;
    private readonly QuickRunCommandRegistry registry;
    private readonly ApplicationSettings settings;

    /// <summary>
    ///     Creates the resolver over the shared command catalog, live editor
    ///     boundary, settings instance, and frontend-neutral notification stream.
    /// </summary>
    /// <param name="registry">Supplies current and named commands without view reflection.</param>
    /// <param name="liveReader">Reports selected hit objects when smart routing is enabled.</param>
    /// <param name="settings">Supplies live Smart QuickRun preferences.</param>
    /// <param name="notifications">Reports stale configuration and captured failures.</param>
    /// <param name="logger">Records command routing and outcomes.</param>
    public QuickRunService(
        QuickRunCommandRegistry registry,
        ILiveBeatmapReader liveReader,
        ApplicationSettings settings,
        IUserNotificationService notifications,
        ILogger<QuickRunService>? logger = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.liveReader = liveReader ?? throw new ArgumentNullException(nameof(liveReader));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.notifications = notifications
                             ?? throw new ArgumentNullException(nameof(notifications));
        this.logger = logger ?? NullLogger<QuickRunService>.Instance;
    }

    /// <inheritdoc />
    public async Task<QuickRunResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        QuickRunCommand? command = null;
        logger.LogInformation("QuickRun requested. Smart routing enabled: {SmartRouting}", settings.SmartQuickRunEnabled);
        try
        {
            if (!settings.SmartQuickRunEnabled)
            {
                command = registry.FindCurrent();
                if (command is null)
                {
                    logger.LogWarning("QuickRun has no current command");
                    return new QuickRunResult(QuickRunStatus.NoCurrentCommand);
                }
            }
            else
            {
                var snapshot = await liveReader
                    .ReadAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (snapshot is null)
                {
                    logger.LogWarning("QuickRun could not read the current editor selection");
                    await PublishAsync(
                            UserNotificationSeverity.Warning,
                            "QuickRun",
                            "QuickRun could not determine the current osu! editor selection.")
                        .ConfigureAwait(false);
                    return new QuickRunResult(QuickRunStatus.EditorUnavailable);
                }

                string configuredName = GetConfiguredName(
                    snapshot.SelectedHitObjects.Count);
                logger.LogInformation("QuickRun editor selection count {Count}; configured command {Command}",
                    snapshot.SelectedHitObjects.Count, configuredName);
                command = string.Equals(
                    configuredName,
                    current_tool_sentinel,
                    StringComparison.Ordinal)
                    ? registry.FindCurrent()
                    : registry.FindByDisplayName(configuredName);
                if (command is null)
                {
                    logger.LogWarning("QuickRun command unavailable: {Command}", configuredName);
                    var status = string.Equals(
                        configuredName,
                        current_tool_sentinel,
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

            logger.LogInformation("QuickRun executing {CommandId}", command.Id);
            await command.Execute(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("QuickRun completed {CommandId}", command.Id);
            return new QuickRunResult(QuickRunStatus.Executed, command.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("QuickRun cancelled for {CommandId}", command?.Id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "QuickRun failed for {CommandId}", command?.Id);
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
        if (selectedHitObjectCount <= 0) return settings.NoneQuickRunTool;

        return selectedHitObjectCount == 1
            ? settings.SingleQuickRunTool
            : settings.MultipleQuickRunTool;
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
            // A presentation failure cannot change QuickRun command resolution.
        }
    }
}
