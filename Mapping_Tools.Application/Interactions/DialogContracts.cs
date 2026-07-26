namespace Mapping_Tools.Application.Interactions;

/// <summary>
/// Describes one strongly typed action in a message or confirmation dialog.
/// </summary>
/// <typeparam name="TResult">The result returned when the action is chosen.</typeparam>
/// <param name="Label">The concise text shown on the action button.</param>
/// <param name="Result">The value returned to the caller.</param>
/// <param name="IsDefault">Whether Enter activates this action.</param>
/// <param name="IsCancel">Whether Escape activates this action.</param>
public sealed record DialogChoice<TResult>(
    string Label,
    TResult Result,
    bool IsDefault = false,
    bool IsCancel = false);

/// <summary>
/// Defines a modal message whose actions return values chosen by the caller.
/// </summary>
/// <typeparam name="TResult">The result type used by the calling workflow.</typeparam>
public sealed class MessageDialogRequest<TResult>
{
    /// <summary>
    /// Creates a typed message request and verifies its keyboard actions are unambiguous.
    /// </summary>
    /// <param name="title">The owner-window title-bar text.</param>
    /// <param name="message">The primary, wrapping message.</param>
    /// <param name="choices">One or more typed actions in display order.</param>
    /// <param name="dismissResult">The result returned when native window chrome closes the dialog.</param>
    /// <param name="details">Optional secondary diagnostic or explanatory text.</param>
    /// <exception cref="ArgumentException">Thrown for empty text, no choices, empty labels, or anything other than one default and one cancel action.</exception>
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
        if (choices.Count == 0)
        {
            throw new ArgumentException("A dialog requires at least one choice.", nameof(choices));
        }

        if (choices.Any(choice => string.IsNullOrWhiteSpace(choice.Label)))
        {
            throw new ArgumentException("Dialog choice labels cannot be empty.", nameof(choices));
        }

        if (choices.Count(choice => choice.IsDefault) != 1)
        {
            throw new ArgumentException("A dialog requires exactly one default choice.", nameof(choices));
        }

        if (choices.Count(choice => choice.IsCancel) != 1)
        {
            throw new ArgumentException("A dialog requires exactly one cancel choice.", nameof(choices));
        }

        Title = title;
        Message = message;
        Choices = choices.ToArray();
        DismissResult = dismissResult;
        Details = string.IsNullOrWhiteSpace(details) ? null : details;
    }

    /// <summary>
    /// Gets the dialog title displayed by the operating-system window chrome.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the primary message, which may contain long wrapping content.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets an immutable snapshot of the actions in display order.
    /// </summary>
    public IReadOnlyList<DialogChoice<TResult>> Choices { get; }

    /// <summary>
    /// Gets the typed fallback returned when the title-bar close action dismisses the window.
    /// </summary>
    public TResult DismissResult { get; }

    /// <summary>
    /// Gets optional secondary text for diagnostics or nested-error context.
    /// </summary>
    public string? Details { get; }
}

/// <summary>
/// Defines a modal field that parses and validates a typed value before acceptance.
/// </summary>
/// <typeparam name="TValue">The parsed value returned to the caller.</typeparam>
public sealed class ValueDialogRequest<TValue>
{
    /// <summary>
    /// Creates a typed field request using UI-independent conversion and validation contracts.
    /// </summary>
    /// <param name="title">The owner-window title-bar text.</param>
    /// <param name="prompt">The label or instruction placed above the field.</param>
    /// <param name="initialValue">The value formatted into the field when it opens.</param>
    /// <param name="converter">The bidirectional text representation.</param>
    /// <param name="validators">Rules evaluated in order after successful parsing.</param>
    /// <param name="acceptLabel">The default-action button text.</param>
    /// <param name="cancelLabel">The Escape-action button text.</param>
    public ValueDialogRequest(
        string title,
        string prompt,
        TValue initialValue,
        ITextValueConverter<TValue> converter,
        IReadOnlyList<IValueValidator<TValue>>? validators = null,
        string acceptLabel = "OK",
        string cancelLabel = "Cancel")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentException.ThrowIfNullOrWhiteSpace(acceptLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(cancelLabel);

        Title = title;
        Prompt = prompt;
        InitialValue = initialValue;
        Converter = converter;
        Validators = validators?.ToArray() ?? [];
        AcceptLabel = acceptLabel;
        CancelLabel = cancelLabel;
    }

    /// <summary>
    /// Gets the dialog title displayed by the operating-system window chrome.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the field label or editing instruction.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// Gets the value formatted into the field before it receives focus.
    /// </summary>
    public TValue InitialValue { get; }

    /// <summary>
    /// Gets the converter responsible for culture and format compatibility.
    /// </summary>
    public ITextValueConverter<TValue> Converter { get; }

    /// <summary>
    /// Gets an immutable snapshot of rules evaluated in order after parsing.
    /// </summary>
    public IReadOnlyList<IValueValidator<TValue>> Validators { get; }

    /// <summary>
    /// Gets the label for the Enter/default action.
    /// </summary>
    public string AcceptLabel { get; }

    /// <summary>
    /// Gets the label for the Escape/cancel action.
    /// </summary>
    public string CancelLabel { get; }

    /// <summary>
    /// Parses current field text and returns the first conversion or validation problem.
    /// </summary>
    /// <param name="text">The editable text to evaluate.</param>
    /// <returns>A typed value only when parsing and every ordered validator succeed.</returns>
    public ValueEvaluation<TValue> Evaluate(string? text)
    {
        if (!Converter.TryConvert(
                text,
                out TValue? value,
                out string? conversionError))
        {
            return new ValueEvaluation<TValue>(
                false,
                default,
                conversionError ?? "The value could not be parsed.");
        }

        foreach (IValueValidator<TValue> validator in Validators)
        {
            ValidationOutcome outcome = validator.Validate(value);
            if (!outcome.IsValid)
            {
                return new ValueEvaluation<TValue>(
                    false,
                    default,
                    outcome.ErrorMessage);
            }
        }

        return new ValueEvaluation<TValue>(true, value, null);
    }
}

/// <summary>
/// Carries the typed result of parsing and validating current field text.
/// </summary>
/// <typeparam name="TValue">The form value type.</typeparam>
/// <param name="IsValid">Whether parsing and every ordered rule succeeded.</param>
/// <param name="Value">The parsed value when valid, or the type default after failure.</param>
/// <param name="ErrorMessage">The first format or validation correction.</param>
public readonly record struct ValueEvaluation<TValue>(
    bool IsValid,
    TValue? Value,
    string? ErrorMessage);

/// <summary>
/// Carries either an accepted typed field value or an explicit cancellation.
/// </summary>
/// <typeparam name="TValue">The form value type.</typeparam>
/// <param name="Accepted">Whether the user submitted a valid value.</param>
/// <param name="Value">The submitted value, or the type default after cancellation.</param>
public readonly record struct ValueDialogResult<TValue>(bool Accepted, TValue? Value);

/// <summary>
/// Presents owner-modal interactions without exposing a particular desktop framework.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a typed message and completes after the owner-modal window closes.
    /// </summary>
    /// <typeparam name="TResult">The calling workflow's result type.</typeparam>
    /// <param name="request">Content, actions, keyboard defaults, and dismiss fallback.</param>
    /// <param name="cancellationToken">Closes the dialog and cancels the returned task when requested.</param>
    /// <returns>The result associated with the chosen action or native dismissal.</returns>
    Task<TResult> ShowMessageAsync<TResult>(
        MessageDialogRequest<TResult> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a typed, validated field and completes after submission or cancellation.
    /// </summary>
    /// <typeparam name="TValue">The parsed field value type.</typeparam>
    /// <param name="request">Prompt, initial value, conversion, validation, and button labels.</param>
    /// <param name="cancellationToken">Closes the dialog and cancels the returned task when requested.</param>
    /// <returns>An accepted value or an explicit user-cancellation result.</returns>
    Task<ValueDialogResult<TValue>> ShowValueAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken = default);
}
