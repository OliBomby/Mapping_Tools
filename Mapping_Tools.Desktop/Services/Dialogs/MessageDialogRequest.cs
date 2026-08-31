namespace Mapping_Tools.Desktop.Services.Dialogs;

/// <summary>
///     Defines a modal message whose actions return values chosen by the caller.
/// </summary>
/// <typeparam name="TResult">The result type used by the calling workflow.</typeparam>
public sealed class MessageDialogRequest<TResult>
{
    /// <summary>
    ///     Creates a typed message request and verifies its keyboard actions are unambiguous.
    /// </summary>
    /// <param name="title">The owner-window title-bar text.</param>
    /// <param name="message">The primary, wrapping message.</param>
    /// <param name="choices">One or more typed actions in display order.</param>
    /// <param name="dismissResult">The result returned when native window chrome closes the dialog.</param>
    /// <param name="details">Optional secondary diagnostic or explanatory text.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown for empty text, no choices, empty labels, or anything other than one default
    ///     and one cancel action.
    /// </exception>
    public MessageDialogRequest(
        string title,
        string message,
        IReadOnlyList<DialogChoice<TResult>> choices,
        TResult dismissResult,
        string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Count == 0) throw new ArgumentException("A dialog requires at least one choice.", nameof(choices));

        if (choices.Any(choice => string.IsNullOrWhiteSpace(choice.Label))) throw new ArgumentException("Dialog choice labels cannot be empty.", nameof(choices));

        if (choices.Count(choice => choice.IsDefault) != 1) throw new ArgumentException("A dialog requires exactly one default choice.", nameof(choices));

        if (choices.Count(choice => choice.IsCancel) != 1) throw new ArgumentException("A dialog requires exactly one cancel choice.", nameof(choices));

        Title = title;
        Message = message;
        Choices = choices.ToArray();
        DismissResult = dismissResult;
        Details = string.IsNullOrWhiteSpace(details) ? null : details;
    }

    /// <summary>
    ///     Gets the dialog title displayed by the operating-system window chrome.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     Gets the primary message, which may contain long wrapping content.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets an immutable snapshot of the actions in display order.
    /// </summary>
    public IReadOnlyList<DialogChoice<TResult>> Choices { get; }

    /// <summary>
    ///     Gets the typed fallback returned when the title-bar close action dismisses the window.
    /// </summary>
    public TResult DismissResult { get; }

    /// <summary>
    ///     Gets optional secondary text for diagnostics or nested-error context.
    /// </summary>
    public string? Details { get; }
}
