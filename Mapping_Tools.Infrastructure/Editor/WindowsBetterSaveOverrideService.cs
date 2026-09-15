using System.Security.Cryptography;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Observes focused osu! saves on Windows and replaces them with the shared BetterSave workflow.
/// </summary>
public sealed class WindowsBetterSaveOverrideService : IBetterSaveOverrideService, IDisposable
{
    private readonly IBetterSaveService betterSave;
    private readonly object configurationGate = new();
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly Func<bool> isOsuForeground;
    private readonly Func<bool> isWindows;
    private readonly IUserNotificationService notifications;
    private readonly SemaphoreSlim saveGate = new(1, 1);

    private readonly FileSystemWatcher watcher = new()
    {
        Filter = "*.osu",
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
    };

    private bool disposed;
    private string? lastBetterSaveHash;
    private string? lastBetterSavePath;
    private CancellationTokenSource? observation;

    /// <summary>
    ///     Creates a disabled watcher over current-map lookup and the shared BetterSave command.
    /// </summary>
    /// <param name="currentBeatmapLocator">Identifies whether a changed file is current in osu!.</param>
    /// <param name="betterSave">Performs live-state loading, backup, and persistence.</param>
    /// <param name="notifications">Reports watcher configuration and callback failures.</param>
    public WindowsBetterSaveOverrideService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBetterSaveService betterSave,
        IUserNotificationService notifications)
        : this(currentBeatmapLocator, betterSave, notifications, OperatingSystem.IsWindows, IsOsuForegroundWindow)
    {
    }

    internal WindowsBetterSaveOverrideService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBetterSaveService betterSave,
        IUserNotificationService notifications,
        Func<bool> isWindows,
        Func<bool> isOsuForeground)
    {
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        this.isOsuForeground = isOsuForeground ?? throw new ArgumentNullException(nameof(isOsuForeground));
        watcher.Changed += OnBeatmapChanged;
        watcher.Created += OnBeatmapChanged;
        watcher.Renamed += OnBeatmapChanged;
    }

    /// <inheritdoc />
    public void Configure(string songsPath, bool enabled)
    {
        lock (configurationGate)
        {
            ThrowIfDisposed();
            watcher.EnableRaisingEvents = false;
            observation?.Cancel();
            observation?.Dispose();
            observation = null;
            lastBetterSaveHash = null;
            lastBetterSavePath = null;
            if (!enabled) return;

            if (!isWindows())
            {
                _ = PublishFailureAsync(new PlatformNotSupportedException(
                    "Automatic BetterSave override is currently supported only on Windows."));
                return;
            }

            if (string.IsNullOrWhiteSpace(songsPath) || !Directory.Exists(songsPath))
            {
                _ = PublishFailureAsync(new DirectoryNotFoundException(
                    "The configured Songs folder is unavailable, so automatic BetterSave override was not enabled."));
                return;
            }

            watcher.Path = Path.GetFullPath(songsPath);
            observation = new CancellationTokenSource();
            watcher.EnableRaisingEvents = true;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (configurationGate)
        {
            if (disposed) return;

            watcher.EnableRaisingEvents = false;
            observation?.Cancel();
        }
    }

    /// <summary>Stops and disposes the filesystem watcher.</summary>
    public void Dispose()
    {
        lock (configurationGate)
        {
            if (disposed) return;

            disposed = true;
            watcher.EnableRaisingEvents = false;
            observation?.Cancel();
            observation?.Dispose();
            watcher.Changed -= OnBeatmapChanged;
            watcher.Created -= OnBeatmapChanged;
            watcher.Renamed -= OnBeatmapChanged;
            watcher.Dispose();
        }
    }

    private async void OnBeatmapChanged(object sender, FileSystemEventArgs eventArgs)
    {
        CancellationToken cancellationToken;
        lock (configurationGate)
        {
            if (disposed || observation is null || observation.IsCancellationRequested) return;

            cancellationToken = observation.Token;
        }

        bool lockTaken = false;
        try
        {
            if (!isOsuForeground()) return;

            // Saves can replace a temporary file, or report several changes before
            // the writer closes. Let the burst settle and retain events arriving
            // during an earlier save instead of dropping them.
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            await saveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            string? currentPath = await currentBeatmapLocator
                .FindCurrentBeatmapAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(currentPath, eventArgs.FullPath, StringComparison.OrdinalIgnoreCase) || !isOsuForeground())
                return;

            string currentHash = await GetCompletedSaveHashAsync(eventArgs.FullPath, cancellationToken).ConfigureAwait(false);
            if (string.Equals(lastBetterSavePath, eventArgs.FullPath, StringComparison.OrdinalIgnoreCase)
                && currentHash == lastBetterSaveHash) return;

            cancellationToken.ThrowIfCancellationRequested();
            var result = await betterSave.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (result.Status == BetterSaveStatus.Saved)
            {
                string savedHash = await GetCompletedSaveHashAsync(eventArgs.FullPath, cancellationToken).ConfigureAwait(false);
                lock (configurationGate)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    lastBetterSavePath = eventArgs.FullPath;
                    lastBetterSaveHash = savedHash;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (disposed)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(exception).ConfigureAwait(false);
        }
        finally
        {
            if (lockTaken) saveGate.Release();
        }
    }

    private static bool IsOsuForegroundWindow()
    {
        using var process = OsuProcessDiscovery.FindStableProcess();
        return process is not null && process.MainWindowHandle != nint.Zero && WindowsNativeMethods.GetForegroundWindow() == process.MainWindowHandle;
    }

    private static async Task<string> GetCompletedSaveHashAsync(string path, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? hash = await TryGetHashAsync(path, cancellationToken).ConfigureAwait(false);
            if (hash is not null) return hash;

            await Task.Delay(20, cancellationToken).ConfigureAwait(false);
        }

        throw new IOException("The osu! save did not finish in time for BetterSave to replace it.");
    }

    private static async Task<string?> TryGetHashAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexString(hash);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private Task PublishFailureAsync(Exception exception)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            "BetterSave override",
            exception.Message,
            exception));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
