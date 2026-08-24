namespace Mapping_Tools.Application.Execution.UserNotification.Models;

/// <summary>
///     Wraps a published message in conventional event data while preserving the
///     immutable notification instance shared by every subscriber.
/// </summary>
public sealed class UserNotificationPublishedEventArgs : EventArgs
{
    /// <summary>
    ///     Creates event data for one message entering the notification stream.
    /// </summary>
    /// <param name="notification">The exact message supplied by the application use case.</param>
    public UserNotificationPublishedEventArgs(UserNotification notification)
    {
        Notification = notification
                       ?? throw new ArgumentNullException(nameof(notification));
    }

    /// <summary>
    ///     Exposes the same immutable message to every registered frontend subscriber.
    /// </summary>
    public UserNotification Notification { get; }
}

