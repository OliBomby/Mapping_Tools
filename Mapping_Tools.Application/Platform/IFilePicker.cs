namespace Mapping_Tools.Application.Platform;

/// <summary>
/// Presents native file and folder pickers while returning only local filesystem paths.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Gets whether the current platform can present an open-file picker.
    /// </summary>
    bool CanOpenFiles { get; }

    /// <summary>
    /// Gets whether the current platform can present a save-file picker.
    /// </summary>
    bool CanSaveFiles { get; }

    /// <summary>
    /// Gets whether the current platform can present a folder picker.
    /// </summary>
    bool CanPickFolders { get; }

    /// <summary>
    /// Lets the user choose existing local files.
    /// </summary>
    /// <param name="request">Dialog title, initial location, multiplicity, and filters.</param>
    /// <param name="cancellationToken">Cancels result processing around the native dialog.</param>
    /// <returns>Selected local paths, or an empty list when the user cancels.</returns>
    Task<IReadOnlyList<string>> PickOpenFilesAsync(
        OpenFilePickerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lets the user choose a destination file.
    /// </summary>
    /// <param name="request">Dialog title, initial location, proposed name, and filters.</param>
    /// <param name="cancellationToken">Cancels result processing around the native dialog.</param>
    /// <returns>The selected local path, or <see langword="null"/> when the user cancels.</returns>
    Task<string?> PickSaveFileAsync(
        SaveFilePickerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lets the user choose existing local directories.
    /// </summary>
    /// <param name="request">Dialog title, initial location, and multiplicity.</param>
    /// <param name="cancellationToken">Cancels result processing around the native dialog.</param>
    /// <returns>Selected local directory paths, or an empty list when the user cancels.</returns>
    Task<IReadOnlyList<string>> PickFoldersAsync(
        OpenFolderPickerRequest request,
        CancellationToken cancellationToken = default);
}
