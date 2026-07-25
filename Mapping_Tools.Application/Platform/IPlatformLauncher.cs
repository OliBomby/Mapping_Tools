namespace Mapping_Tools.ApplicationServices.Platform;

/// <summary>
/// Opens URIs and local filesystem items with their operating-system handlers.
/// </summary>
public interface IPlatformLauncher
{
    /// <summary>
    /// Opens an absolute URI with its registered application.
    /// </summary>
    /// <param name="uri">The absolute URI to open.</param>
    /// <param name="cancellationToken">Cancels before the platform handoff.</param>
    /// <returns><see langword="true"/> when the platform accepted the URI.</returns>
    Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an existing file with its registered application.
    /// </summary>
    /// <param name="path">The local file to open.</param>
    /// <param name="cancellationToken">Cancels before the platform handoff.</param>
    /// <returns><see langword="true"/> when the platform accepted the file.</returns>
    Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an existing directory in the platform file manager.
    /// </summary>
    /// <param name="path">The local directory to open.</param>
    /// <param name="cancellationToken">Cancels before the platform handoff.</param>
    /// <returns><see langword="true"/> when the platform accepted the directory.</returns>
    Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default);
}
