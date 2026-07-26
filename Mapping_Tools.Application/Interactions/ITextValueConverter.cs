namespace Mapping_Tools.Application.Interactions;

/// <summary>
/// Converts between editable text and a typed form value.
/// </summary>
/// <typeparam name="T">The value type represented by the text.</typeparam>
public interface ITextValueConverter<T>
{
    /// <summary>
    /// Formats an existing value for editing without losing required precision.
    /// </summary>
    /// <param name="value">The value to place in a form field.</param>
    /// <returns>The editable text representation.</returns>
    string Format(T value);

    /// <summary>
    /// Attempts to parse text and supplies a correction message on failure.
    /// </summary>
    /// <param name="text">The current field text, which may be <see langword="null"/>.</param>
    /// <param name="value">The parsed value when conversion succeeds.</param>
    /// <param name="errorMessage">A user-facing format error when conversion fails.</param>
    /// <returns><see langword="true"/> only when <paramref name="value"/> is usable.</returns>
    bool TryConvert(string? text, out T value, out string? errorMessage);
}