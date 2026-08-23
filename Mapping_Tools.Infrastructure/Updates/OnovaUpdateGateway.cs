using System.Reflection;
using System.Text.Json;
using Mapping_Tools.Application.Updates;
using Onova;
using Onova.Models;
using Onova.Services;

namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Parses the small release-metadata payload returned by GitHub's latest-release endpoint.
/// </summary>
public static class GithubReleaseMetadataParser
{
    /// <summary>
    ///     Reads the optional release name and body without accepting arbitrary JSON as metadata.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response body.</param>
    /// <returns>The title and body, or empty metadata for a JSON null response.</returns>
    /// <exception cref="JsonException">The payload is malformed or is not a JSON object.</exception>
    public static UpdateReleaseNotes Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Null) return new UpdateReleaseNotes(null, null);

        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("The GitHub release response is not an object.");

        return new UpdateReleaseNotes(
            ReadString(document.RootElement, "name"),
            ReadString(document.RootElement, "body"));
    }

    private static string? ReadString(JsonElement objectElement, string propertyName)
    {
        if (!objectElement.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : throw new JsonException($"GitHub release property '{propertyName}' is not a string.");
    }
}

/// <summary>
///     Holds the release title and long description shown before installation.
/// </summary>
/// <param name="Title">The release name, when supplied.</param>
/// <param name="Body">The release description, when supplied.</param>
public sealed record UpdateReleaseNotes(string? Title, string? Body);

/// <summary>
///     Adapts Onova's GitHub resolver, ZIP extractor, staging directory, lock file,
///     and external updater process to the Application update contract.
/// </summary>
public sealed class OnovaUpdateGateway : IUpdateGateway
{
    private const string PublishedExecutableName = "Mapping Tools.exe";
    private const string RepositoryOwner = "OliBomby";
    private const string RepositoryName = "Mapping_Tools";

    private const string ReleaseMetadataUrl =
        "https://api.github.com/repos/OliBomby/Mapping_Tools/releases/latest";

    private readonly string _assetName;
    private readonly bool _disposeHttpClient;
    private readonly HttpClient _httpClient;
    private readonly GithubPackageResolver _packageResolver;
    private readonly IUpdateManager _updateManager;
    private bool _disposed;

    /// <summary>
    ///     Creates the production GitHub updater with the Mapping Tools user-agent
    ///     and architecture-specific Avalonia release assets.
    /// </summary>
    public OnovaUpdateGateway()
        : this(new HttpClient(), true)
    {
    }

    /// <summary>
    ///     Creates a gateway using a caller-owned HTTP client, which is useful for
    ///     deterministic hosts and tests.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for GitHub API and asset requests.</param>
    public OnovaUpdateGateway(HttpClient httpClient)
        : this(httpClient, false)
    {
    }

    private OnovaUpdateGateway(HttpClient httpClient, bool disposeHttpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mapping Tools");

        _assetName = Environment.Is64BitProcess ? "release_x64.zip" : "release.zip";
        _packageResolver = new GithubPackageResolver(
            _httpClient,
            RepositoryOwner,
            RepositoryName,
            _assetName);

        var entryAssembly = Assembly.GetEntryAssembly()
                            ?? typeof(OnovaUpdateGateway).Assembly;
        string publishedExecutablePath = ResolveExecutablePath(entryAssembly);
        var assemblyMetadata = File.Exists(publishedExecutablePath)
            ? AssemblyMetadata.FromAssembly(entryAssembly, publishedExecutablePath)
            : AssemblyMetadata.FromAssembly(entryAssembly);
        _updateManager = new UpdateManager(
            assemblyMetadata,
            _packageResolver,
            new ZipPackageExtractor());
    }

    /// <inheritdoc />
    public async Task<UpdatePackageInfo> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var result = await _updateManager
            .CheckForUpdatesAsync(cancellationToken)
            .ConfigureAwait(false);
        UpdateReleaseNotes? notes = null;
        if (result.CanUpdate && result.LastVersion is not null) notes = await ReadReleaseNotesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdatePackageInfo(
            _updateManager.Updatee.Version,
            result.LastVersion,
            notes?.Title,
            notes?.Body,
            _assetName);
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
        return _updateManager.PrepareUpdateAsync(version, progress, cancellationToken);
    }

    /// <inheritdoc />
    public void LaunchUpdater(Version version, bool restartAfterUpdate)
    {
        ArgumentNullException.ThrowIfNull(version);
        ThrowIfDisposed();
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "The current release package uses the Windows Onova updater.");

        _updateManager.LaunchUpdater(version, restartAfterUpdate);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _updateManager.Dispose();
        if (_disposeHttpClient) _httpClient.Dispose();
    }

    private async Task<UpdateReleaseNotes> ReadReleaseNotesAsync(
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient
            .GetAsync(ReleaseMetadataUrl, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        string json = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        return GithubReleaseMetadataParser.Parse(json);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static string ResolveExecutablePath(Assembly entryAssembly)
    {
        string? processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath)
            && string.Equals(
                Path.GetFileName(processPath),
                PublishedExecutableName,
                StringComparison.OrdinalIgnoreCase))
            return processPath;

        string publishedExecutablePath = Path.Combine(
            AppContext.BaseDirectory,
            PublishedExecutableName);
        return File.Exists(publishedExecutablePath)
            ? publishedExecutablePath
            : entryAssembly.Location;
    }
}
