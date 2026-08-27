using System.Runtime.InteropServices;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Platform;

/// <summary>
///     Enumerates the Windows virtual desktop and translates monitor rectangles
///     and effective DPI into neutral Geometry Dashboard records.
/// </summary>
public sealed class WindowsGeometryDashboardScreenService : IGeometryDashboardScreenService
{
    private readonly Func<bool> isWindows;

    /// <summary>Creates the adapter using the current platform guard.</summary>
    public WindowsGeometryDashboardScreenService()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardScreenService(Func<bool> isWindows)
    {
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public bool IsSupported => isWindows();

    /// <inheritdoc />
    public IReadOnlyList<GeometryDashboardScreen> GetScreens()
    {
        if (!isWindows()) return [];

        List<GeometryDashboardScreen> screens = [];
        bool enumerated = WindowsNativeMethods.EnumDisplayMonitors(
            0,
            0,
            (monitor, deviceContext, ref bounds, data) =>
            {
                if (TryReadScreen(monitor, out var screen)) screens.Add(screen!);

                return true;
            },
            0);
        return enumerated ? screens : [];
    }

    /// <inheritdoc />
    public GeometryDashboardScreen? GetPrimaryScreen()
    {
        return GetScreens().FirstOrDefault(screen => screen.IsPrimary);
    }

    /// <inheritdoc />
    public GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window)
    {
        if (!isWindows() || window.IsEmpty) return null;

        nint monitor = WindowsNativeMethods.MonitorFromWindow(
            new nint(window.Value),
            WindowsNativeMethods.MONITOR_DEFAULT_TO_NEAREST);
        return monitor == 0 || !TryReadScreen(monitor, out var screen)
            ? null
            : screen;
    }

    private static bool TryReadScreen(
        nint monitor,
        out GeometryDashboardScreen? screen)
    {
        screen = null;
        WindowsNativeMethods.Monitorinfo info = new()
        {
            Size = Marshal.SizeOf<WindowsNativeMethods.Monitorinfo>(),
        };
        if (!WindowsNativeMethods.GetMonitorInfo(monitor, ref info)) return false;

        var dpiScale = Vector2.One;
        bool dpiAvailable = false;
        try
        {
            int result = WindowsNativeMethods.GetDpiForMonitor(
                monitor,
                WindowsNativeMethods.DPI_TYPE_EFFECTIVE,
                out uint dpiX,
                out uint dpiY);
            if (result == 0 && dpiX > 0 && dpiY > 0)
            {
                dpiScale = new Vector2(dpiX / 96d, dpiY / 96d);
                dpiAvailable = true;
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }

        screen = new GeometryDashboardScreen(
            monitor.ToInt64(),
            ToBox(info.Monitor),
            ToBox(info.Work),
            (info.Flags & WindowsNativeMethods.MONITOR_INFO_PRIMARY) != 0,
            dpiScale,
            dpiAvailable);
        return true;
    }

    private static Box2 ToBox(WindowsNativeMethods.Rect rectangle)
    {
        return new Box2(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
    }
}
