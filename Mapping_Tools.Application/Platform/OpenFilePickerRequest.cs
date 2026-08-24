namespace Mapping_Tools.Application.Platform;

/// <summary>
///     Configures a native open-file picker without exposing frontend-specific types.
/// </summary>
public sealed record OpenFilePickerRequest
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
    ///     Gets whether the user may select more than one file.
    /// </summary>
    public bool AllowMultiple { get; init; }

    /// <summary>
    ///     Gets the selectable file-type filters.
    /// </summary>
    public IReadOnlyList<FilePickerFilter> Filters { get; init; } = [];
}

