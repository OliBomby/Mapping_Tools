using Mapping_Tools.Application.Execution.UserNotification.Models;

namespace Mapping_Tools.Application.Execution.UserNotification;

/// <summary>
///     Maintains a frontend-neutral in-process notification stream with no hidden
///     thread switch, queue timeout, or presentation side effect.
/// </summary>
public sealed class UserNotificationService : IUserNotificationService
{
    /// <inheritdoc />
    public event EventHandler<UserNotificationPublishedEventArgs>? Published;

    /// <inheritdoc />
    public Task PublishAsync(
        Models.UserNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        Published?.Invoke(
            this,
            new UserNotificationPublishedEventArgs(notification));
        return Task.CompletedTask;
    }
}
