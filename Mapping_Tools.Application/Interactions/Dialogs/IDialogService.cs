namespace Mapping_Tools.Application.Interactions.Dialogs;

/// <summary>
///     Presents owner-modal interactions without exposing a particular desktop framework.
/// </summary>
public interface IDialogService
{
    /// <summary>
    ///     Shows a typed message and completes after the owner-modal window closes.
    /// </summary>
    /// <typeparam name="TResult">The calling workflow's result type.</typeparam>
    /// <param name="request">Content, actions, keyboard defaults, and dismiss fallback.</param>
    /// <param name="cancellationToken">Closes the dialog and cancels the returned task when requested.</param>
    /// <returns>The result associated with the chosen action or native dismissal.</returns>
    Task<TResult> ShowMessageAsync<TResult>(
        MessageDialogRequest<TResult> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Shows a typed, validated field and completes after submission or cancellation.
    /// </summary>
    /// <typeparam name="TValue">The parsed field value type.</typeparam>
    /// <param name="request">Prompt, initial value, conversion, validation, and button labels.</param>
    /// <param name="cancellationToken">Closes the dialog and cancels the returned task when requested.</param>
    /// <returns>An accepted value or an explicit user-cancellation result.</returns>
    Task<ValueDialogResult<TValue>> ShowValueAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken = default);
}
