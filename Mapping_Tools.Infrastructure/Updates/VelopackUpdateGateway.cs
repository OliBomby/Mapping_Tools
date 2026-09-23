using System.Reflection;
using System.Runtime.InteropServices;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;
using Velopack;
using Velopack.Sources;

namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Adapts Velopack's cross-platform feed, download, and restart lifecycle to
///     the application update boundary.
/// </summary>
public sealed class VelopackUpdateGateway : IUpdateGateway
{
    private const string github_repository_url = "https://github.com/OliBomby/Mapping_Tools";

    private const string release_metadata_url =
        "https://api.github.com/repos/OliBomby/Mapping_Tools/releases/latest";

    private const string release_history_url =
        "https://api.github.com/repos/OliBomby/Mapping_Tools/releases";

    private const int release_page_size = 100;

    private readonly string channel;
    private readonly HttpClient httpClient;
    private readonly UpdateManager updateManager;
    private bool disposed;
    private UpdateInfo? pendingUpdate;

    /// <summary>
    ///     Creates a GitHub-backed updater, or a local-feed updater when a file
    ///     from a Velopack feed directory is supplied.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for release-notes requests.</param>
    /// <param name="localUpdatePackagePath">
    ///     An optional path to any file in a local Velopack feed directory,
    ///     normally a <c>*.nupkg</c> package or <c>releases.*.json</c> file.
    /// </param>
    public VelopackUpdateGateway(
        HttpClient httpClient,
        string? localUpdatePackagePath = null)
        : this(
            CreateUpdateSource(localUpdatePackagePath),
            GetUpdateChannel(),
            httpClient)
    {
    }

    /// <summary>
    ///     Creates an updater for a supplied Velopack source.
    /// </summary>
    /// <param name="updateSource">The Velopack feed source.</param>
    /// <param name="channel">The platform and architecture-specific feed channel.</param>
    /// <param name="httpClient">The HTTP client used for release-notes requests.</param>
    public VelopackUpdateGateway(
        IUpdateSource updateSource,
        string channel,
        HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(updateSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(httpClient);

        this.channel = channel;
        this.httpClient = httpClient;
        if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
            this.httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mapping Tools");

        updateManager = new UpdateManager(
            updateSource,
            new UpdateOptions
            {
                ExplicitChannel = channel,
            });
    }

    /// <inheritdoc />
    public async Task<UpdatePackageInfo> CheckForUpdatesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Version currentVersion = GetCurrentVersion();
        if (!updateManager.IsInstalled)
            return new UpdatePackageInfo(
                currentVersion,
                null,
                null,
                null,
                channel);

        UpdateInfo? update = await updateManager
            .CheckForUpdatesAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        pendingUpdate = update;
        if (update is null)
            return new UpdatePackageInfo(
                currentVersion,
                null,
                null,
                null,
                channel);

        VelopackAsset release = update.TargetFullRelease;
        UpdateReleaseNotes? notes = await ReadUpdateReleaseNotesAsync(
            release,
            cancellationToken).ConfigureAwait(false);
        return new UpdatePackageInfo(
            currentVersion,
            release.Version.Version,
            notes?.Title,
            notes?.Body,
            release.FileName);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UpdateReleaseNotes>> GetReleaseNotesAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return ReadReleaseNotesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PrepareUpdateAsync(
        Version version,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(progress);
        ThrowIfDisposed();

        UpdateInfo update = pendingUpdate
                            ?? throw new InvalidOperationException(
                                "Do not prepare an update before checking for updates.");
        if (update.TargetFullRelease.Version.Version != version)
            throw new InvalidOperationException(
                "The requested update is different from the last checked update.");
        if (!updateManager.IsInstalled)
            throw new InvalidOperationException(
                "Updates can only be prepared by a Velopack installation.");

        await updateManager.DownloadUpdatesAsync(
                update,
                value => progress.Report(value / 100d),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void LaunchUpdater(Version version, bool restartAfterUpdate)
    {
        ArgumentNullException.ThrowIfNull(version);
        ThrowIfDisposed();

        UpdateInfo update = pendingUpdate
                            ?? throw new InvalidOperationException(
                                "Do not launch the updater before checking for updates.");
        if (update.TargetFullRelease.Version.Version != version)
            throw new InvalidOperationException(
                "The requested update is different from the last checked update.");

        updateManager.WaitExitThenApplyUpdates(
            update.TargetFullRelease,
            restart: restartAfterUpdate,
            restartArgs: restartAfterUpdate
                ? Environment.GetCommandLineArgs().Skip(1).ToArray()
                : null);
        pendingUpdate = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        pendingUpdate = null;
    }

    private static IUpdateSource CreateUpdateSource(string? localUpdatePackagePath)
    {
        if (localUpdatePackagePath is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localUpdatePackagePath);
            string? directoryPath = Path.GetDirectoryName(localUpdatePackagePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException(
                    "The local update package path must have a parent directory.",
                    nameof(localUpdatePackagePath));

            return new SimpleFileSource(new DirectoryInfo(directoryPath));
        }

        return new GithubSource(
            github_repository_url,
            accessToken: null,
            prerelease: false);
    }

    private static string GetUpdateChannel()
    {
        string platform = OperatingSystem.IsWindows()
            ? "win"
            : OperatingSystem.IsMacOS()
                ? "osx"
                : OperatingSystem.IsLinux()
                    ? "linux"
                    : throw new PlatformNotSupportedException(
                        "Velopack updates are not supported on this operating system.");

        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 when platform == "win" => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException(
                "Velopack updates are not supported for this process architecture."),
        };

        if (platform == "win" && architecture == "arm64")
            throw new PlatformNotSupportedException(
                "Velopack updates are not published for Windows ARM64.");

        if (platform != "win" && architecture == "x86")
            throw new PlatformNotSupportedException(
                "Velopack updates are not published for 32-bit Unix processes.");

        return $"{platform}-{architecture}";
    }

    private Version GetCurrentVersion()
    {
        return updateManager.CurrentVersion?.Version
               ?? (Assembly.GetEntryAssembly() ?? typeof(VelopackUpdateGateway).Assembly)
               .GetName()
               .Version
               ?? new Version(0, 0, 0, 0);
    }

    private async Task<UpdateReleaseNotes?> ReadUpdateReleaseNotesAsync(
        VelopackAsset release,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(release.NotesMarkdown))
            return new UpdateReleaseNotes(
                release.Version.ToString(),
                release.NotesMarkdown);

        return await ReadLatestReleaseNotesAsync(cancellationToken).ConfigureAwait(false);
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
                .GetAsync(
                    $"{release_history_url}?per_page={release_page_size}&page={page}",
                    cancellationToken)
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
}
