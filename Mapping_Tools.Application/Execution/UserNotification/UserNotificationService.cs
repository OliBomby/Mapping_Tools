using Mapping_Tools.Application.Execution.UserNotification.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Application.Execution.UserNotification;

/// <summary>
///     Maintains a frontend-neutral in-process notification stream with no hidden
///     thread switch, queue timeout, or presentation side effect.
/// </summary>
public sealed class UserNotificationService : IUserNotificationService
{
    private readonly ILogger<UserNotificationService> logger;

    /// <summary>Creates the notification stream with optional diagnostic logging.</summary>
    /// <param name="logger">Records notifications presented to the user.</param>
    public UserNotificationService(ILogger<UserNotificationService>? logger = null)
    {
        this.logger = logger ?? NullLogger<UserNotificationService>.Instance;
    }

    /// <inheritdoc />
    public event EventHandler<UserNotificationPublishedEventArgs>? Published;

    /// <inheritdoc />
    public Task PublishAsync(
        Models.UserNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        if (notification.Exception is null)
            logger.LogInformation("Notification {Severity}: {Title}: {Message}", notification.Severity, notification.Title, notification.Message);
        else
            logger.LogError(notification.Exception, "Notification {Severity}: {Title}: {Message}", notification.Severity, notification.Title, notification.Message);
        Published?.Invoke(
            this,
            new UserNotificationPublishedEventArgs(notification));
        return Task.CompletedTask;
    }
}
