using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
/// Observes focused osu! saves on Windows and replaces them with the shared BetterSave workflow.
/// </summary>
public sealed class WindowsBetterSaveOverrideService : IBetterSaveOverrideService, IDisposable
{
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IBetterSaveService _betterSave;
    private readonly IUserNotificationService _notifications;
    private readonly FileSystemWatcher _watcher = new()
    {
        Filter = "*.osu",
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
    };
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private readonly object _configurationGate = new();
    private string? _lastBetterSaveHash;
    private bool _disposed;

    /// <summary>
    /// Creates a disabled watcher over current-map lookup and the shared BetterSave command.
    /// </summary>
    /// <param name="currentBeatmapLocator">Identifies whether a changed file is current in osu!.</param>
    /// <param name="betterSave">Performs live-state loading, backup, and persistence.</param>
    /// <param name="notifications">Reports watcher configuration and callback failures.</param>
    public WindowsBetterSaveOverrideService(
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBetterSaveService betterSave,
        IUserNotificationService notifications)
    {
        _currentBeatmapLocator = currentBeatmapLocator
            ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _watcher.Changed += OnBeatmapChanged;
    }

    /// <inheritdoc/>
    public void Configure(string songsPath, bool enabled)
    {
        lock (_configurationGate)
        {
            ThrowIfDisposed();
            _watcher.EnableRaisingEvents = false;
            _lastBetterSaveHash = null;
            if (!enabled)
            {
                return;
            }

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

            _watcher.Path = Path.GetFullPath(songsPath);
            _watcher.EnableRaisingEvents = true;
        }
    }

    /// <inheritdoc/>
    public void Stop()
    {
        lock (_configurationGate)
        {
            if (!_disposed)
            {
                _watcher.EnableRaisingEvents = false;
            }
        }
    }

    /// <summary>Stops and disposes the filesystem watcher.</summary>
    public void Dispose()
    {
        lock (_configurationGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnBeatmapChanged;
            _watcher.Dispose();
        }
    }

    private async void OnBeatmapChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (!await _saveGate.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            string? currentPath = await _currentBeatmapLocator
                .FindCurrentBeatmapAsync()
                .ConfigureAwait(false);
            if (!string.Equals(currentPath, eventArgs.FullPath, StringComparison.OrdinalIgnoreCase) ||
                !IsOsuForegroundWindow())
            {
                return;
            }

            string? currentHash = await TryGetHashAsync(eventArgs.FullPath).ConfigureAwait(false);
            if (currentHash is not null && currentHash == _lastBetterSaveHash)
            {
                return;
            }

            BetterSaveResult result = await _betterSave.ExecuteAsync().ConfigureAwait(false);
            if (result.Status == BetterSaveStatus.Saved)
            {
                _lastBetterSaveHash = await TryGetHashAsync(eventArgs.FullPath).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException) when (_disposed)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(exception).ConfigureAwait(false);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private static bool IsOsuForegroundWindow()
    {
        nint foreground = GetForegroundWindow();
        Process[] processes = Process.GetProcessesByName("osu!");
        try
        {
            return processes.Any(process => process.MainWindowHandle == foreground);
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
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

    private Task PublishFailureAsync(Exception exception) =>
        _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            "BetterSave override",
            exception.Message,
            exception));

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();
}
