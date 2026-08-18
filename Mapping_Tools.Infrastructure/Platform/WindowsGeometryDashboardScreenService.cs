using System.Runtime.InteropServices;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
/// Enumerates the Windows virtual desktop and translates monitor rectangles
/// and effective DPI into neutral Geometry Dashboard records.
/// </summary>
public sealed class WindowsGeometryDashboardScreenService : IGeometryDashboardScreenService
{
    private readonly Func<bool> _isWindows;

    /// <summary>Creates the adapter using the current platform guard.</summary>
    public WindowsGeometryDashboardScreenService()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardScreenService(Func<bool> isWindows)
    {
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc/>
    public bool IsSupported => _isWindows();

    /// <inheritdoc/>
    public IReadOnlyList<GeometryDashboardScreen> GetScreens()
    {
        if (!_isWindows())
        {
            return [];
        }

        List<GeometryDashboardScreen> screens = [];
        bool enumerated = WindowsNativeMethods.EnumDisplayMonitors(
            0,
            0,
            (nint monitor, nint deviceContext, ref WindowsNativeMethods.RECT bounds, nint data) =>
            {
                if (TryReadScreen(monitor, out GeometryDashboardScreen? screen))
                {
                    screens.Add(screen!);
                }

                return true;
            },
            0);
        return enumerated ? screens : [];
    }

    /// <inheritdoc/>
    public GeometryDashboardScreen? GetPrimaryScreen()
    {
        return GetScreens().FirstOrDefault(screen => screen.IsPrimary);
    }

    /// <inheritdoc/>
    public GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window)
    {
        if (!_isWindows() || window.IsEmpty)
        {
            return null;
        }

        nint monitor = WindowsNativeMethods.MonitorFromWindow(
            new nint(window.Value),
            WindowsNativeMethods.MonitorDefaultToNearest);
        return monitor == 0 || !TryReadScreen(monitor, out GeometryDashboardScreen? screen)
            ? null
            : screen;
    }

    private static bool TryReadScreen(
        nint monitor,
        out GeometryDashboardScreen? screen)
    {
        screen = null;
        WindowsNativeMethods.MONITORINFO info = new()
        {
            Size = Marshal.SizeOf<WindowsNativeMethods.MONITORINFO>()
        };
        if (!WindowsNativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        Vector2 dpiScale = Vector2.One;
        bool dpiAvailable = false;
        try
        {
            int result = WindowsNativeMethods.GetDpiForMonitor(
                monitor,
                WindowsNativeMethods.DpiTypeEffective,
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
            (info.Flags & WindowsNativeMethods.MonitorInfoPrimary) != 0,
            dpiScale,
            dpiAvailable);
        return true;
    }

    private static Box2 ToBox(WindowsNativeMethods.RECT rectangle) =>
        new(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
}
