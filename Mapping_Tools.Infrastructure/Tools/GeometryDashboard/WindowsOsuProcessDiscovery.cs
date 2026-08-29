using System.ComponentModel;
using System.Diagnostics;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Discovers the first osu! stable process whose executable and product name
///     match the legacy adapter's exact checks.
/// </summary>
public sealed class WindowsOsuProcessDiscovery : IGeometryDashboardProcessDiscovery
{
    private readonly Func<bool> isWindows;

    /// <summary>Creates a process discovery adapter using the current platform guard.</summary>
    public WindowsOsuProcessDiscovery()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsOsuProcessDiscovery(Func<bool> isWindows)
    {
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public bool IsSupported => isWindows();

    /// <inheritdoc />
    public Task<GeometryDashboardProcess?> FindAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!isWindows()) return Task.FromResult<GeometryDashboardProcess?>(null);

        using var process = OsuProcessDiscovery.FindStableProcess();
        if (process is null) return Task.FromResult<GeometryDashboardProcess?>(null);

        try
        {
            return Task.FromResult<GeometryDashboardProcess?>(new GeometryDashboardProcess(
                process.Id,
                new PlatformWindowId(process.MainWindowHandle.ToInt64()),
                process.MainWindowTitle));
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<GeometryDashboardProcess?>(null);
        }
        catch (Win32Exception)
        {
            return Task.FromResult<GeometryDashboardProcess?>(null);
        }
    }
}

internal static class OsuProcessDiscovery
{
    internal static Process? FindStableProcess()
    {
        return FindStableProcess(null);
    }

    internal static Process? FindStableProcess(long? expectedProcessId)
    {
        if (!OperatingSystem.IsWindows()) return null;

        if (expectedProcessId is <= 0) return null;

        foreach (var process in Process.GetProcessesByName("osu!"))
        {
            bool matches = false;
            try
            {
                if (expectedProcessId is null || process.Id == expectedProcessId.Value)
                {
                    var mainModule = process.MainModule;
                    matches = mainModule is not null
                              && string.Equals(
                                  mainModule.ModuleName,
                                  "osu!.exe",
                                  StringComparison.Ordinal)
                              && string.Equals(
                                  mainModule.FileVersionInfo.ProductName,
                                  "osu!",
                                  StringComparison.Ordinal);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }

            if (matches) return process;

            process.Dispose();
        }

        return null;
    }
}
