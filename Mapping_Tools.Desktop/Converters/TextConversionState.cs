namespace Mapping_Tools.Desktop.Converters;

/// <summary>
/// Tracks the conversion status of one editable field whose submit command
/// must remain disabled while its text cannot be converted.
/// </summary>
public sealed class TextConversionState
{
    /// <summary>
    /// Raised when the field enters or leaves a conversion-error state.
    /// </summary>
    public event EventHandler? ErrorChanged;

    /// <summary>
    /// Gets the current format correction, or <see langword="null"/> after a successful conversion.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets whether the current field text failed conversion.
    /// </summary>
    public bool HasError => ErrorMessage is not null;

    internal void SetError(string? errorMessage)
    {
        if (ErrorMessage == errorMessage)
        {
            return;
        }

        ErrorMessage = errorMessage;
        ErrorChanged?.Invoke(this, EventArgs.Empty);
    }
}
