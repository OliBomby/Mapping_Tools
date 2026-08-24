using System.Security.Cryptography;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Platform.GeometryDashboard;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Observes focused osu! saves on Windows and replaces them with the shared BetterSave workflow.
/// </summary>
public sealed class WindowsBetterSaveOverrideService : IBetterSaveOverrideService, IDisposable
{
    private readonly IBetterSaveService betterSave;
    private readonly object configurationGate = new();
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly IUserNotificationService notifications;
    private readonly SemaphoreSlim saveGate = new(1, 1);

    private readonly FileSystemWatcher watcher = new()
    {
        Filter = "*.osu",
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
    };

    private bool disposed;
    private string? lastBetterSaveHash;

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
    {
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        watcher.Changed += OnBeatmapChanged;
    }

    /// <inheritdoc />
    public void Configure(string songsPath, bool enabled)
    {
        lock (configurationGate)
        {
            ThrowIfDisposed();
            watcher.EnableRaisingEvents = false;
            lastBetterSaveHash = null;
            if (!enabled) return;

            if (!OperatingSystem.IsWindows())
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
            watcher.EnableRaisingEvents = true;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (configurationGate)
        {
            if (!disposed) watcher.EnableRaisingEvents = false;
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
            watcher.Changed -= OnBeatmapChanged;
            watcher.Dispose();
        }
    }

    private async void OnBeatmapChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (!await saveGate.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            string? currentPath = await currentBeatmapLocator
                .FindCurrentBeatmapAsync()
                .ConfigureAwait(false);
            if (!string.Equals(currentPath, eventArgs.FullPath, StringComparison.OrdinalIgnoreCase) || !IsOsuForegroundWindow())
                return;

            string? currentHash = await TryGetHashAsync(eventArgs.FullPath).ConfigureAwait(false);
            if (currentHash is not null && currentHash == lastBetterSaveHash) return;

            var result = await betterSave.ExecuteAsync().ConfigureAwait(false);
            if (result.Status == BetterSaveStatus.Saved) lastBetterSaveHash = await TryGetHashAsync(eventArgs.FullPath).ConfigureAwait(false);
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
            saveGate.Release();
        }
    }

    private static bool IsOsuForegroundWindow()
    {
        using var process = OsuProcessDiscovery.FindStableProcess();
        return process is not null && process.MainWindowHandle != nint.Zero && WindowsNativeMethods.GetForegroundWindow() == process.MainWindowHandle;
    }

    private static async Task<string?> TryGetHashAsync(string path)
    {
        try
        {
            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            byte[] hash = await SHA256.HashDataAsync(stream).ConfigureAwait(false);
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
