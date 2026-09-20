using System.Reflection;
using System.Runtime.InteropServices;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;
using Onova;
using Onova.Models;
using Onova.Services;
using IPackageResolver = Mapping_Tools.Application.Updates.Contracts.IPackageResolver;

namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Adapts an Onova package resolver, ZIP extractor, staging directory, lock file,
///     and external updater process to the Application update contract.
/// </summary>
public sealed class OnovaUpdateGateway : IUpdateGateway
{
    private const string published_executable_name = "Mapping Tools.exe";

    private const string release_metadata_url =
        "https://api.github.com/repos/OliBomby/Mapping_Tools/releases/latest";

    private const string release_history_url =
        "https://api.github.com/repos/OliBomby/Mapping_Tools/releases";

    private const int release_page_size = 100;

    private readonly string assetName;
    private readonly bool disposeHttpClient;
    private readonly HttpClient httpClient;
    private readonly IUpdateManager updateManager;
    private bool disposed;

    /// <summary>
    ///     Creates an updater using the supplied package resolver.
    /// </summary>
    /// <param name="packageResolver">Resolves the packages offered by the update channel.</param>
    public OnovaUpdateGateway(IPackageResolver packageResolver)
        : this(packageResolver, new HttpClient(), true)
    {
    }

    /// <summary>
    ///     Creates an updater using caller-owned resolver and HTTP resources.
    /// </summary>
    /// <param name="packageResolver">Resolves the packages offered by the update channel.</param>
    /// <param name="httpClient">The HTTP client used for release metadata requests.</param>
    public OnovaUpdateGateway(IPackageResolver packageResolver, HttpClient httpClient)
        : this(packageResolver, httpClient, false)
    {
    }

    private OnovaUpdateGateway(
        IPackageResolver packageResolver,
        HttpClient httpClient,
        bool disposeHttpClient)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.disposeHttpClient = disposeHttpClient;

        if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mapping Tools");

        assetName = GetAssetName();

        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? typeof(OnovaUpdateGateway).Assembly;
        string publishedExecutablePath = ResolveExecutablePath(entryAssembly);
        var assemblyMetadata = File.Exists(publishedExecutablePath)
            ? AssemblyMetadata.FromAssembly(entryAssembly, publishedExecutablePath)
            : AssemblyMetadata.FromAssembly(entryAssembly);

        updateManager = new UpdateManager(
            assemblyMetadata,
            new OnovaPackageResolverAdapter(packageResolver),
            new ZipPackageExtractor());
    }

    /// <inheritdoc />
    public async Task<UpdatePackageInfo> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var result = await updateManager
            .CheckForUpdatesAsync(cancellationToken)
            .ConfigureAwait(false);

        UpdateReleaseNotes? notes = null;
        if (result is { CanUpdate: true, LastVersion: not null }) notes = await ReadLatestReleaseNotesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdatePackageInfo(
            updateManager.Updatee.Version,
            result.LastVersion,
            notes?.Title,
            notes?.Body,
            assetName);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UpdateReleaseNotes>> GetReleaseNotesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return ReadReleaseNotesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task PrepareUpdateAsync(
        Version version,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(progress);
        ThrowIfDisposed();
        return updateManager.PrepareUpdateAsync(version, progress, cancellationToken);
    }

    /// <inheritdoc />
    public void LaunchUpdater(Version version, bool restartAfterUpdate)
    {
        ArgumentNullException.ThrowIfNull(version);
        ThrowIfDisposed();
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "The current release package uses the Windows Onova updater.");

        updateManager.LaunchUpdater(version, restartAfterUpdate);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        updateManager.Dispose();
        if (disposeHttpClient) httpClient.Dispose();
    }

    private async Task<UpdateReleaseNotes> ReadLatestReleaseNotesAsync(
        CancellationToken cancellationToken)
    {
        using var response = await httpClient
            .GetAsync(release_metadata_url, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string json = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        return GithubReleaseMetadataParser.Parse(json);
    }

    private async Task<IReadOnlyList<UpdateReleaseNotes>> ReadReleaseNotesAsync(
        CancellationToken cancellationToken)
    {
        List<UpdateReleaseNotes> releaseNotes = [];

        for (int page = 1;; page++)
        {
            using var response = await httpClient
                .GetAsync($"{release_history_url}?per_page={release_page_size}&page={page}", cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string json = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            var pageNotes = GithubReleaseMetadataParser.ParseMany(json);
            releaseNotes.AddRange(pageNotes);

            if (pageNotes.Count < release_page_size) return releaseNotes;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static string ResolveExecutablePath(Assembly entryAssembly)
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath)
            && string.Equals(
                Path.GetFileName(processPath),
                published_executable_name,
                StringComparison.OrdinalIgnoreCase))
            return processPath;

        string publishedExecutablePath = Path.Combine(
            AppContext.BaseDirectory,
            published_executable_name);
        return File.Exists(publishedExecutablePath)
            ? publishedExecutablePath
            : entryAssembly.Location;
    }

    private static string GetAssetName()
    {
        if (OperatingSystem.IsWindows())
            return RuntimeInformation.ProcessArchitecture == Architecture.X86
                ? "release.zip"
                : "release_x64.zip";

        string platform = OperatingSystem.IsMacOS() ? "osx" : "linux";
        string architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "arm64"
            : "x64";
        return $"mapping-tools-{platform}-{architecture}.zip";
    }

    private sealed class OnovaPackageResolverAdapter : Onova.Services.IPackageResolver
    {
        private readonly IPackageResolver resolver;

        public OnovaPackageResolverAdapter(IPackageResolver resolver)
        {
            this.resolver = resolver;
        }

        public Task<IReadOnlyList<Version>> GetPackageVersionsAsync(
            CancellationToken cancellationToken)
        {
            return resolver.GetPackageVersionsAsync(cancellationToken);
        }

        public Task DownloadPackageAsync(
            Version version,
            string destFilePath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return resolver.DownloadPackageAsync(
                version,
                destFilePath,
                progress,
                cancellationToken);
        }
    }
}
