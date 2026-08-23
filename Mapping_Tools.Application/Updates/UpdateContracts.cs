using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Updates;

/// <summary>
///     Describes the release metadata returned by the update source.
/// </summary>
/// <param name="CurrentVersion">The version of the running application.</param>
/// <param name="LatestVersion">The newest version offered by the selected update channel.</param>
/// <param name="ReleaseTitle">The GitHub release title, when the release payload contains one.</param>
/// <param name="ReleaseBody">The GitHub release description, when the release payload contains one.</param>
/// <param name="AssetName">The architecture-specific package asset selected for this process.</param>
public sealed record UpdatePackageInfo(
    Version CurrentVersion,
    Version? LatestVersion,
    string? ReleaseTitle,
    string? ReleaseBody,
    string AssetName)
{
    /// <summary>
    ///     Gets whether the source returned a package newer than the running version.
    /// </summary>
    public bool CanUpdate => LatestVersion is not null && LatestVersion > CurrentVersion;
}

/// <summary>
///     Identifies the user-visible result of an update check.
/// </summary>
public enum UpdateAvailability
{
    /// <summary>No newer package was found.</summary>
    None,

    /// <summary>A newer package was suppressed by the persisted skip setting.</summary>
    Skipped,

    /// <summary>A newer package is ready to be offered to the user.</summary>
    Available,
}

/// <summary>
///     Contains the update check outcome shown by the updater UI.
/// </summary>
/// <param name="Availability">Whether a package is available, skipped, or absent.</param>
/// <param name="CurrentVersion">The running application version.</param>
/// <param name="LatestVersion">The offered version, or <see langword="null" /> when no package was found.</param>
/// <param name="ReleaseTitle">The release title returned by GitHub.</param>
/// <param name="ReleaseBody">The release description returned by GitHub.</param>
/// <param name="AssetName">The architecture-specific package asset selected for this process.</param>
public sealed record UpdateCheckResult(
    UpdateAvailability Availability,
    Version CurrentVersion,
    Version? LatestVersion,
    string? ReleaseTitle,
    string? ReleaseBody,
    string AssetName)
{
    /// <summary>Gets whether the caller should display the update decision UI.</summary>
    public bool CanUpdate => Availability == UpdateAvailability.Available;
}

/// <summary>
///     Reports a normalized package-preparation progress value.
/// </summary>
/// <param name="Progress">A value in the inclusive range zero through one.</param>
public sealed class UpdateProgressChangedEventArgs : EventArgs
{
    /// <summary>Creates a progress notification.</summary>
    /// <param name="progress">A value in the inclusive range zero through one.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite or is outside zero through one.</exception>
    public UpdateProgressChangedEventArgs(double progress)
    {
        if (!double.IsFinite(progress) || progress is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(progress));

        Progress = progress;
    }

    /// <summary>Gets the normalized package-preparation progress.</summary>
    public double Progress { get; }
}

/// <summary>
///     Provides the network, archive, staging, and process boundary used by the
///     application updater use case.
/// </summary>
public interface IUpdateGateway : IDisposable
{
    /// <summary>
    ///     Checks the configured release channel and selects the process-architecture package.
    /// </summary>
    /// <param name="cancellationToken">Cancels network and release-metadata work.</param>
    /// <returns>The running version, newest available version, release metadata, and asset name.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    Task<UpdatePackageInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads, validates, extracts, and stages one previously discovered package.
    /// </summary>
    /// <param name="version">The version returned by the last successful check.</param>
    /// <param name="progress">Receives normalized package-preparation progress.</param>
    /// <param name="cancellationToken">Cancels download, extraction, or staging before launch.</param>
    /// <exception cref="InvalidOperationException">The package cannot be prepared in the current updater state.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    Task PrepareUpdateAsync(
        Version version,
        IProgress<double> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Launches the external updater that applies the staged package after this process exits.
    /// </summary>
    /// <param name="version">The version prepared by <see cref="PrepareUpdateAsync" />.</param>
    /// <param name="restartAfterUpdate">Restarts the application with its original arguments after replacement.</param>
    /// <exception cref="InvalidOperationException">The package was not prepared or launch was requested twice.</exception>
    /// <exception cref="PlatformNotSupportedException">The selected deployment platform has no supported updater process.</exception>
    void LaunchUpdater(Version version, bool restartAfterUpdate);
}

/// <summary>
///     Coordinates update-channel policy, persisted skip-version behavior, and
///     the staged-package lifecycle without referencing a UI or platform API.
/// </summary>
public interface IUpdateService : IDisposable
{
    /// <summary>Gets the most recent check result, or <see langword="null" /> before a check.</summary>
    UpdateCheckResult? LastCheck { get; }

    /// <summary>Gets the current preparation task when a download is active or has completed.</summary>
    Task? ActiveDownloadTask { get; }

    /// <summary>Fires whenever package preparation reports a new progress value.</summary>
    event EventHandler<UpdateProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    ///     Checks the release channel and applies the persisted skipped-version policy.
    /// </summary>
    /// <param name="allowSkippedVersion">Suppresses versions at or below the persisted skip version when true.</param>
    /// <param name="cancellationToken">Cancels the check and metadata request.</param>
    /// <returns>The typed check result used by the updater UI.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    Task<UpdateCheckResult> CheckForUpdatesAsync(
        bool allowSkippedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Starts preparing the version from the last successful available check.
    ///     Reuses an already-running preparation task so Wait and shutdown share one download.
    /// </summary>
    /// <param name="cancellationToken">Cancels the package preparation.</param>
    /// <returns>The shared preparation task.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled while preparing the package.</exception>
    Task PrepareUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores the currently offered version as the user's skipped version.</summary>
    void SkipCurrentVersion();

    /// <summary>
    ///     Starts the staged external updater and clears the wait-after-close state.
    /// </summary>
    /// <param name="restartAfterUpdate">Restarts the application after replacement when true.</param>
    void StartUpdateProcess(bool restartAfterUpdate);

    /// <summary>
    ///     Discards the current check and preparation state without changing persisted settings.
    /// </summary>
    void AbandonUpdate();
}

/// <summary>
///     Implements update policy above a platform-specific package gateway.
/// </summary>
public sealed class UpdateService : IUpdateService, IAsyncDisposable
{
    private readonly SemaphoreSlim checkGate = new(1, 1);
    private readonly CancellationTokenSource disposeCancellation = new();
    private readonly IUpdateGateway gateway;
    private readonly ApplicationSettings settings;
    private readonly object stateLock = new();
    private Task? activeDownloadTask;
    private bool checkInProgress;
    private Task? disposeTask;
    private bool disposed;
    private CancellationTokenSource? downloadCancellation;
    private UpdateCheckResult? lastCheck;
    private long operationId;
    private bool prepared;

    /// <summary>Creates the update use case.</summary>
    /// <param name="gateway">The network, archive, staging, and process adapter.</param>
    /// <param name="settings">The shared settings document containing the skipped version.</param>
    public UpdateService(IUpdateGateway gateway, ApplicationSettings settings)
    {
        this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    ///     Waits for an in-flight package preparation and then releases the update
    ///     gateway, coordination semaphore, and cancellation sources.
    /// </summary>
    /// <returns>A task that completes after all updater resources are released.</returns>
    public ValueTask DisposeAsync()
    {
        lock (stateLock)
        {
            if (disposeTask is not null) return new ValueTask(disposeTask);

            disposed = true;
            operationId++;
            downloadCancellation?.Cancel();
            downloadCancellation = null;
            lastCheck = null;
            prepared = false;
            disposeCancellation.Cancel();
            disposeTask = DisposeCoreAsync(activeDownloadTask);
            return new ValueTask(disposeTask);
        }
    }

    /// <inheritdoc />
    public event EventHandler<UpdateProgressChangedEventArgs>? ProgressChanged;

    /// <inheritdoc />
    public UpdateCheckResult? LastCheck
    {
        get
        {
            lock (stateLock)
            {
                return lastCheck;
            }
        }
    }

    /// <inheritdoc />
    public Task? ActiveDownloadTask
    {
        get
        {
            lock (stateLock)
            {
                return activeDownloadTask;
            }
        }
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        bool allowSkippedVersion,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await checkGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var checkCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                disposeCancellation.Token);
        try
        {
            ThrowIfDisposed();

            Task? activeDownload;
            lock (stateLock)
            {
                checkInProgress = true;
                operationId++;
                downloadCancellation?.Cancel();
                activeDownload = activeDownloadTask;
                lastCheck = null;
                prepared = false;
            }

            if (activeDownload is not null)
                try
                {
                    await activeDownload
                        .WaitAsync(checkCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!checkCancellation.IsCancellationRequested)
                {
                    // A canceled preparation must be observed before the next
                    // check can reuse the update gateway.
                }
                catch (Exception) when (!checkCancellation.IsCancellationRequested)
                {
                    // A failed preparation is already reported by its caller;
                    // the next explicit check is allowed to recover from it.
                }

            checkCancellation.Token.ThrowIfCancellationRequested();
            var package = await gateway
                .CheckForUpdatesAsync(checkCancellation.Token)
                .ConfigureAwait(false);

            var availability = package.CanUpdate
                ? IsSkipped(package.LatestVersion, allowSkippedVersion)
                    ? UpdateAvailability.Skipped
                    : UpdateAvailability.Available
                : UpdateAvailability.None;

            UpdateCheckResult result = new(
                availability,
                package.CurrentVersion,
                package.LatestVersion,
                package.ReleaseTitle,
                package.ReleaseBody,
                package.AssetName);
            lock (stateLock)
            {
                lastCheck = result;
                prepared = false;
                activeDownloadTask = null;
            }

            return result;
        }
        finally
        {
            lock (stateLock)
            {
                checkInProgress = false;
            }

            checkGate.Release();
            checkCancellation.Dispose();
        }
    }

    /// <inheritdoc />
    public Task PrepareUpdateAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        UpdateCheckResult check;
        CancellationTokenSource? downloadCancellation = null;
        TaskCompletionSource? completion = null;
        long operationId = 0;
        lock (stateLock)
        {
            ThrowIfDisposed();
            if (checkInProgress)
                throw new InvalidOperationException(
                    "Do not prepare an update while checking for updates!");

            check = lastCheck
                    ?? throw new InvalidOperationException("Do not call this method before fetching updates!");
            if (!check.CanUpdate || check.LatestVersion is null) throw new InvalidOperationException("Do not call this method if there are no updates!");

            if (prepared) return Task.CompletedTask;

            if (activeDownloadTask is { IsCompleted: false }) return activeDownloadTask;

            downloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                disposeCancellation.Token);
            completion = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            this.downloadCancellation = downloadCancellation;
            activeDownloadTask = completion.Task;
            operationId = ++this.operationId;
        }

        var downloadCancellationSource = downloadCancellation!;
        var completionSource = completion!;
        IProgress<double> progress = new InlineProgress(value =>
        {
            ProgressChanged?.Invoke(this, new UpdateProgressChangedEventArgs(value));
        });
        _ = PrepareCoreAsync(
            check,
            operationId,
            progress,
            downloadCancellationSource,
            completionSource);
        return completionSource.Task;
    }

    /// <inheritdoc />
    public void SkipCurrentVersion()
    {
        ThrowIfDisposed();
        UpdateCheckResult check;
        lock (stateLock)
        {
            check = lastCheck
                    ?? throw new InvalidOperationException("Do not call this method before fetching updates!");
        }

        if (!check.CanUpdate || check.LatestVersion is null) throw new InvalidOperationException("Do not skip a version when there are no updates!");

        settings.SkipVersion = check.LatestVersion.ToString();
    }

    /// <inheritdoc />
    public void StartUpdateProcess(bool restartAfterUpdate)
    {
        ThrowIfDisposed();

        UpdateCheckResult check;
        lock (stateLock)
        {
            check = lastCheck
                    ?? throw new InvalidOperationException("Do not call this method before fetching updates!");
            if (!check.CanUpdate || check.LatestVersion is null) throw new InvalidOperationException("Do not call this method if there are no updates!");

            if (!prepared) throw new InvalidOperationException("Do not call this method before download has finished!");
        }

        gateway.LaunchUpdater(check.LatestVersion, restartAfterUpdate);
        lock (stateLock)
        {
            operationId++;
            prepared = false;
            activeDownloadTask = null;
        }
    }

    /// <inheritdoc />
    public void AbandonUpdate()
    {
        ThrowIfDisposed();
        lock (stateLock)
        {
            operationId++;
            downloadCancellation?.Cancel();
            lastCheck = null;
            prepared = false;
            if (activeDownloadTask?.IsCompleted == true) activeDownloadTask = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task DisposeCoreAsync(Task? activeDownloadTask)
    {
        if (activeDownloadTask is not null)
            try
            {
                await activeDownloadTask.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Disposal must still release the gateway after a canceled or
                // failed package preparation.
            }

        await checkGate.WaitAsync().ConfigureAwait(false);
        checkGate.Release();
        gateway.Dispose();
        checkGate.Dispose();
        disposeCancellation.Dispose();
        ProgressChanged = null;
    }

    private async Task PrepareCoreAsync(
        UpdateCheckResult check,
        long operationId,
        IProgress<double> progress,
        CancellationTokenSource downloadCancellation,
        TaskCompletionSource completion)
    {
        try
        {
            await gateway
                .PrepareUpdateAsync(check.LatestVersion!, progress, downloadCancellation.Token)
                .ConfigureAwait(false);
            downloadCancellation.Token.ThrowIfCancellationRequested();
            bool isCurrent;
            lock (stateLock)
            {
                isCurrent = !disposed && this.operationId == operationId && ReferenceEquals(lastCheck, check);
                prepared = isCurrent;
            }

            if (isCurrent)
                completion.TrySetResult();
            else
                completion.TrySetCanceled();
        }
        catch (OperationCanceledException exception)
        {
            lock (stateLock)
            {
                if (this.operationId == operationId) prepared = false;
            }

            var token = exception.CancellationToken.IsCancellationRequested
                ? exception.CancellationToken
                : downloadCancellation.Token;
            completion.TrySetCanceled(token);
        }
        catch (Exception exception)
        {
            lock (stateLock)
            {
                if (this.operationId == operationId) prepared = false;
            }

            completion.TrySetException(exception);
        }
        finally
        {
            lock (stateLock)
            {
                if (ReferenceEquals(this.downloadCancellation, downloadCancellation)) this.downloadCancellation = null;
            }

            downloadCancellation.Dispose();
        }
    }

    private bool IsSkipped(Version? latestVersion, bool allowSkippedVersion)
    {
        if (!allowSkippedVersion || latestVersion is null || string.IsNullOrWhiteSpace(settings.SkipVersion))
            return false;

        return Version.TryParse(settings.SkipVersion, out var skippedVersion) && latestVersion <= skippedVersion;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class InlineProgress(Action<double> callback) : IProgress<double>
    {
        private readonly Action<double> callback = callback ?? throw new ArgumentNullException(nameof(callback));

        public void Report(double value)
        {
            callback(value);
        }
    }
}
