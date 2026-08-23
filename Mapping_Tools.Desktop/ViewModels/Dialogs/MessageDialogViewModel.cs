using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>
///     Supplies immutable, wrapping content and typed action adapters to a message dialog.
/// </summary>
public sealed class MessageDialogViewModel : ObservableObject
{
    /// <summary>
    ///     Creates presentation state for a message or confirmation dialog.
    /// </summary>
    /// <param name="title">The native window title.</param>
    /// <param name="message">The primary message.</param>
    /// <param name="details">Optional secondary or diagnostic text.</param>
    /// <param name="choices">The actions shown in display order.</param>
    public MessageDialogViewModel(
        string title,
        string message,
        string? details,
        IReadOnlyList<DialogChoiceViewModel> choices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(choices);
        Title = title;
        Message = message;
        Details = details;
        HasDetails = !string.IsNullOrWhiteSpace(details);
        Choices = choices;
    }

    /// <summary>
    ///     Gets the native window title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    ///     Gets the primary message, including long content that must wrap.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets optional secondary text for explanatory or nested-error context.
    /// </summary>
    public string? Details { get; }

    /// <summary>
    ///     Gets whether secondary text should occupy layout space.
    /// </summary>
    public bool HasDetails { get; }

    /// <summary>
    ///     Gets the immutable action list in display order.
    /// </summary>
    public IReadOnlyList<DialogChoiceViewModel> Choices { get; }
}
