using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;

namespace Mapping_Tools.Application.Updates;

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
