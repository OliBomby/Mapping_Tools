using Mapping_Tools.Application.Updates.Contracts;

namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Resolves update packages from GitHub through Onova.
/// </summary>
public sealed class GithubUpdatePackageResolver : IPackageResolver
{
    private readonly Onova.Services.GithubPackageResolver resolver;

    /// <summary>
    ///     Creates a GitHub-backed package resolver.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to query GitHub.</param>
    /// <param name="repositoryOwner">The GitHub repository owner.</param>
    /// <param name="repositoryName">The GitHub repository name.</param>
    /// <param name="packageFilePattern">The release asset name to resolve.</param>
    public GithubUpdatePackageResolver(
        HttpClient httpClient,
        string repositoryOwner,
        string repositoryName,
        string packageFilePattern)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFilePattern);

        resolver = new Onova.Services.GithubPackageResolver(
            httpClient,
            repositoryOwner,
            repositoryName,
            packageFilePattern);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Version>> GetPackageVersionsAsync(
        CancellationToken cancellationToken = default)
    {
        return resolver.GetPackageVersionsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DownloadPackageAsync(
        Version version,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return resolver.DownloadPackageAsync(
            version,
            destinationPath,
            progress,
            cancellationToken);
    }
}
