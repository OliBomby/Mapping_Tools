namespace Mapping_Tools.Application.Interactions.Dialogs;

/// <summary>
///     Carries either an accepted typed field value or an explicit cancellation.
/// </summary>
/// <typeparam name="TValue">The form value type.</typeparam>
/// <param name="Accepted">Whether the user submitted a valid value.</param>
/// <param name="Value">The submitted value, or the type default after cancellation.</param>
public readonly record struct ValueDialogResult<TValue>(bool Accepted, TValue? Value);

