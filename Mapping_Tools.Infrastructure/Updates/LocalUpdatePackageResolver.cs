using Onova.Services;
using IPackageResolver = Mapping_Tools.Application.Updates.Contracts.IPackageResolver;

namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Resolves a local update package through Onova.
/// </summary>
public sealed class LocalUpdatePackageResolver : IPackageResolver
{
    private readonly LocalPackageResolver resolver;

    /// <summary>
    ///     Creates a resolver for a package in a local directory.
    /// </summary>
    /// <param name="directoryPath">The directory containing the package.</param>
    /// <param name="packageFilePattern">The package file name or pattern.</param>
    public LocalUpdatePackageResolver(string directoryPath, string packageFilePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageFilePattern);

        resolver = new LocalPackageResolver(
            directoryPath,
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
