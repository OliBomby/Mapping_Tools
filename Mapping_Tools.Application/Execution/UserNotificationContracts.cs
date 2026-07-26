namespace Mapping_Tools.Application.Execution;

/// <summary>
/// Classifies user-facing messages without prescribing a snackbar, dialog,
/// status bar, or other frontend presentation.
/// </summary>
public enum UserNotificationSeverity
{
    /// <summary>
    /// Communicates neutral state that does not require corrective action.
    /// </summary>
    Information,

    /// <summary>
    /// Confirms that the requested operation completed as intended.
    /// </summary>
    Success,

    /// <summary>
    /// Reports a recoverable condition that deserves attention but did not crash the operation.
    /// </summary>
    Warning,

    /// <summary>
    /// Reports that an operation failed and may include its diagnostic exception.
    /// </summary>
    Error
}

/// <summary>
/// Carries presentation-neutral text and diagnostics from an application use
/// case to whichever notification surface the active frontend provides.
/// </summary>
public sealed record UserNotification
{
    /// <summary>
    /// Creates a message whose title remains concise while the body can explain
    /// the completed action, recovery, or failure.
    /// </summary>
    /// <param name="severity">The visual and accessibility importance assigned by the use case.</param>
    /// <param name="title">A short operation-specific heading.</param>
    /// <param name="message">The user-facing outcome or corrective guidance.</param>
    /// <param name="exception">An optional diagnostic retained for logging or a details view.</param>
    public UserNotification(
        UserNotificationSeverity severity,
        string title,
        string message,
        Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Severity = severity;
        Title = title;
        Message = message;
        Exception = exception;
    }

    /// <summary>
    /// Determines whether the frontend presents neutral, success, warning, or failure styling.
    /// </summary>
    public UserNotificationSeverity Severity { get; }

    /// <summary>
    /// Supplies the compact heading used to identify the originating operation.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Supplies the complete user-facing outcome or recovery guidance.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Retains the underlying failure for logs or expandable details without exposing it in normal prose.
    /// </summary>
    public Exception? Exception { get; }
}

/// <summary>
/// Delivers application outcomes to frontends without referencing a window,
/// dispatcher, snackbar library, or message-box API.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Fires synchronously on the publishing thread after a notification enters
    /// the process-lifetime stream; UI subscribers must marshal to their dispatcher.
    /// </summary>
    event EventHandler<UserNotificationPublishedEventArgs>? Published;

    /// <summary>
    /// Publishes one immutable message in call order.
    /// </summary>
    /// <param name="notification">The typed message to make available to frontend subscribers.</param>
    /// <param name="cancellationToken">Cancels before publication; delivery already in progress is not recalled.</param>
    /// <returns>A task that completes after all synchronous subscribers return.</returns>
    /// <exception cref="OperationCanceledException">Cancellation was requested before publication.</exception>
    Task PublishAsync(
        UserNotification notification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Wraps a published message in conventional event data while preserving the
/// immutable notification instance shared by every subscriber.
/// </summary>
public sealed class UserNotificationPublishedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event data for one message entering the notification stream.
    /// </summary>
    /// <param name="notification">The exact message supplied by the application use case.</param>
    public UserNotificationPublishedEventArgs(UserNotification notification)
    {
        Notification = notification
            ?? throw new ArgumentNullException(nameof(notification));
    }

    /// <summary>
    /// Exposes the same immutable message to every registered frontend subscriber.
    /// </summary>
    public UserNotification Notification { get; }
}

/// <summary>
/// Maintains a frontend-neutral in-process notification stream with no hidden
/// thread switch, queue timeout, or presentation side effect.
/// </summary>
public sealed class UserNotificationService : IUserNotificationService
{
    /// <inheritdoc/>
    public event EventHandler<UserNotificationPublishedEventArgs>? Published;

    /// <inheritdoc/>
    public Task PublishAsync(
        UserNotification notification,
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
