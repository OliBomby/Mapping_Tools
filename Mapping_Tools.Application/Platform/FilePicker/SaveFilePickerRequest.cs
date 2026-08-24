namespace Mapping_Tools.Application.Platform.FilePicker;

/// <summary>
///     Configures a native save-file picker without exposing frontend-specific types.
/// </summary>
public sealed record SaveFilePickerRequest
{
    /// <summary>
    ///     Gets the optional dialog title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    ///     Gets the optional local directory the picker should initially display.
    /// </summary>
    public string? SuggestedStartLocation { get; init; }

    /// <summary>
    ///     Gets the filename initially proposed to the user.
    /// </summary>
    public string? SuggestedFileName { get; init; }

    /// <summary>
    ///     Gets the extension appended when the user omits one.
    /// </summary>
    public string? DefaultExtension { get; init; }

    /// <summary>
    ///     Gets whether the platform should ask before replacing an existing file.
    /// </summary>
    public bool ShowOverwritePrompt { get; init; } = true;

    /// <summary>
    ///     Gets the selectable file-type filters.
    /// </summary>
    public IReadOnlyList<FilePickerFilter> Filters { get; init; } = [];
}

