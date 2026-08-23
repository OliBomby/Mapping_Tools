using System.Text;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Tracks Windows top-level windows, activation, bounds, and effective DPI
///     for Geometry Dashboard target selection.
/// </summary>
public sealed class WindowsGeometryDashboardWindowService : IGeometryDashboardWindowService
{
    private readonly Func<bool> isWindows;

    /// <summary>Creates the adapter using the current platform guard.</summary>
    public WindowsGeometryDashboardWindowService()
        : this(OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardWindowService(Func<bool> isWindows)
    {
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public bool IsSupported => isWindows();

    /// <inheritdoc />
    public GeometryDashboardWindow? GetWindow(PlatformWindowId window)
    {
        if (!isWindows() || window.IsEmpty) return null;

        nint nativeWindow = new(window.Value);
        if (!WindowsNativeMethods.IsWindow(nativeWindow)
            || !WindowsNativeMethods.GetWindowRect(
                nativeWindow,
                out var rectangle))
            return null;

        if (WindowsNativeMethods.GetWindowThreadProcessId(
                nativeWindow,
                out uint processId)
            == 0)
            return null;

        return new GeometryDashboardWindow(
            window,
            processId,
            ReadTitle(nativeWindow),
            new Box2(
                rectangle.Left,
                rectangle.Top,
                rectangle.Right,
                rectangle.Bottom),
            WindowsNativeMethods.IsWindowVisible(nativeWindow),
            WindowsNativeMethods.GetForegroundWindow() == nativeWindow,
            ReadDpi(nativeWindow, out bool dpiAvailable),
            dpiAvailable);
    }

    /// <inheritdoc />
    public GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);
        var window = GetWindow(process.MainWindow);
        return window is null || window.ProcessId != process.ProcessId
            ? null
            : window;
    }

    /// <inheritdoc />
    public IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows()
    {
        if (!isWindows()) return [];

        List<GeometryDashboardWindow> windows = [];
        WindowsNativeMethods.EnumWindows(
            (window, _) =>
            {
                var snapshot = GetWindow(
                    new PlatformWindowId(window.ToInt64()));
                if (snapshot is not null) windows.Add(snapshot);

                return true;
            },
            0);
        return windows;
    }

    private static string ReadTitle(nint window)
    {
        int length = WindowsNativeMethods.GetWindowTextLength(window);
        StringBuilder title = new(Math.Max(length + 1, 1));
        WindowsNativeMethods.GetWindowText(window, title, title.Capacity);
        return title.ToString();
    }

    private static Vector2 ReadDpi(nint window, out bool available)
    {
        available = false;
        try
        {
            uint dpi = WindowsNativeMethods.GetDpiForWindow(window);
            if (dpi > 0)
            {
                available = true;
                return new Vector2(dpi / 96d, dpi / 96d);
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }

        return Vector2.One;
    }
}
