namespace Mapping_Tools.Application.Updates.Contracts;

/// <summary>
///     Resolves update package versions and downloads a selected package.
/// </summary>
public interface IPackageResolver
{
    /// <summary>
    ///     Gets the package versions available from the update source.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The available package versions.</returns>
    Task<IReadOnlyList<Version>> GetPackageVersionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads the package for a version to a destination file.
    /// </summary>
    /// <param name="version">The package version to download.</param>
    /// <param name="destinationPath">The file path to write.</param>
    /// <param name="progress">Receives download progress between zero and one.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    /// <returns>A task that completes when the package is downloaded.</returns>
    Task DownloadPackageAsync(
        Version version,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
