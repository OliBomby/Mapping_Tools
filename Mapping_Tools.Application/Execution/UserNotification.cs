namespace Mapping_Tools.Application.Execution;

/// <summary>
///     Carries presentation-neutral text and diagnostics from an application use
///     case to whichever notification surface the active frontend provides.
/// </summary>
public sealed record UserNotification
{
    /// <summary>
    ///     Creates a message whose title remains concise while the body can explain
    ///     the completed action, recovery, or failure.
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
    ///     Determines whether the frontend presents neutral, success, warning, or failure styling.
    /// </summary>
    public UserNotificationSeverity Severity { get; }

    /// <summary>
    ///     Supplies the compact heading used to identify the originating operation.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     Supplies the complete user-facing outcome or recovery guidance.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Retains the underlying failure for logs or expandable details without exposing it in normal prose.
    /// </summary>
    public Exception? Exception { get; }
}

