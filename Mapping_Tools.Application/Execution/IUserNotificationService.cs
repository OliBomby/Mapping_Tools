namespace Mapping_Tools.Application.Execution;

/// <summary>
///     Delivers application outcomes to frontends without referencing a window,
///     dispatcher, snackbar library, or message-box API.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    ///     Fires synchronously on the publishing thread after a notification enters
    ///     the process-lifetime stream; UI subscribers must marshal to their dispatcher.
    /// </summary>
    event EventHandler<UserNotificationPublishedEventArgs>? Published;

    /// <summary>
    ///     Publishes one immutable message in call order.
    /// </summary>
    /// <param name="notification">The typed message to make available to frontend subscribers.</param>
    /// <param name="cancellationToken">Cancels before publication; delivery already in progress is not recalled.</param>
    /// <returns>A task that completes after all synchronous subscribers return.</returns>
    /// <exception cref="OperationCanceledException">Cancellation was requested before publication.</exception>
    Task PublishAsync(
        UserNotification notification,
        CancellationToken cancellationToken = default);
}

