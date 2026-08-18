using System.Runtime.InteropServices;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
/// Creates native target-bound overlay hosts for Geometry Dashboard.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayHostFactory : IGeometryDashboardOverlayHostFactory
{
    private readonly IGeometryDashboardWindowService _windows;
    private readonly Func<bool> _isWindows;

    /// <summary>Creates a factory using the native window service and current platform guard.</summary>
    public WindowsGeometryDashboardOverlayHostFactory(
        IGeometryDashboardWindowService windows)
        : this(windows, OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardOverlayHostFactory(
        IGeometryDashboardWindowService windows,
        Func<bool> isWindows)
    {
        _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc/>
    public IGeometryDashboardOverlayHost Create() =>
        new WindowsGeometryDashboardOverlayHost(_windows, _isWindows);
}

/// <summary>
/// Owns a click-through, non-activating native popup window that follows the
/// active osu! window and retains the legacy overlay coordinate conversion.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayHost : IGeometryDashboardOverlayHost
{
    private const uint ExtendedStyleToolWindow = 0x00000080;
    private const uint ExtendedStyleTransparent = 0x00000020;
    private const uint ExtendedStyleNoActivate = 0x08000000;
    private const uint WindowStylePopup = 0x80000000;
    private const uint GreenYellow = 0x002FFFAD;
    private const int BorderThickness = 3;
    private static readonly object ClassGate = new();
    private static readonly Dictionary<nint, bool> BorderStates = [];
    private static readonly string ClassName =
        "MappingTools.GeometryDashboardOverlayWindow";
    private static WindowsNativeMethods.WindowProcedure? _windowProcedure;
    private static ushort _classAtom;

    private readonly IGeometryDashboardWindowService _windows;
    private readonly Func<bool> _isWindows;
    private bool _enabled;
    private bool _disposed;
    private bool _borderEnabled;
    private nint _window;

    /// <summary>
    /// Creates a native overlay host using the supplied window tracker.
    /// </summary>
    /// <param name="windows">Tracks target activation without exposing native handles to Application.</param>
    public WindowsGeometryDashboardOverlayHost(
        IGeometryDashboardWindowService windows)
        : this(windows, OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardOverlayHost(
        IGeometryDashboardWindowService windows,
        Func<bool> isWindows)
    {
        _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc/>
    public bool IsSupported => _isWindows() && _windows.IsSupported;

    /// <inheritdoc/>
    public bool IsVisible { get; private set; }

    /// <inheritdoc/>
    public PlatformWindowId? TargetWindow { get; private set; }

    /// <inheritdoc/>
    public void Initialize(PlatformWindowId targetWindow)
    {
        ThrowIfDisposed();
        if (!IsSupported)
        {
            return;
        }

        if (targetWindow.IsEmpty)
        {
            throw new ArgumentException(
                "A non-empty target window is required.",
                nameof(targetWindow));
        }

        TargetWindow = null;
        DestroyNativeWindow();
        EnsureWindowClass();
        _window = WindowsNativeMethods.CreateWindowEx(
            ExtendedStyleToolWindow | ExtendedStyleTransparent | ExtendedStyleNoActivate,
            ClassName,
            string.Empty,
            WindowStylePopup,
            0,
            0,
            1,
            1,
            0,
            0,
            WindowsNativeMethods.GetModuleHandle(null),
            0);
        if (_window == 0)
        {
            throw new InvalidOperationException(
                $"Windows could not create the Geometry Dashboard overlay " +
                $"(Win32 error {Marshal.GetLastWin32Error()}).");
        }

        lock (ClassGate)
        {
            BorderStates[_window] = _borderEnabled;
        }

        TargetWindow = targetWindow;
        IsVisible = false;
    }

    /// <inheritdoc/>
    public void Enable()
    {
        ThrowIfDisposed();
        _enabled = true;
    }

    /// <inheritdoc/>
    public void Disable()
    {
        ThrowIfDisposed();
        _enabled = false;
        HideNativeWindow();
    }

    /// <inheritdoc/>
    public void Update(
        Box2 physicalBounds,
        Vector2 dpiMultiplier,
        bool dpiSourceAvailable)
    {
        ThrowIfDisposed();
        if (!IsSupported || !_enabled || _window == 0 || TargetWindow is null)
        {
            return;
        }

        GeometryDashboardWindow? target = _windows.GetWindow(TargetWindow.Value);
        if (target is null || !target.IsActivated)
        {
            HideNativeWindow();
            return;
        }

        if (!TryConvertBounds(
                physicalBounds,
                dpiMultiplier,
                dpiSourceAvailable,
                out NativeBounds nativeBounds))
        {
            HideNativeWindow();
            return;
        }

        WindowsNativeMethods.ShowWindow(
            _window,
            WindowsNativeMethods.ShowNoActivate);
        if (!WindowsNativeMethods.SetWindowPos(
            _window,
            WindowsNativeMethods.TopMostWindow,
            nativeBounds.Left,
            nativeBounds.Top,
            nativeBounds.Width,
            nativeBounds.Height,
            WindowsNativeMethods.SetWindowPosNoActivate |
                WindowsNativeMethods.SetWindowPosNoSendChanging))
        {
            HideNativeWindow();
            return;
        }

        IsVisible = true;
    }

    /// <inheritdoc/>
    public void SetBorder(bool enabled)
    {
        if (_disposed)
        {
            return;
        }

        _borderEnabled = enabled;
        if (_window == 0)
        {
            return;
        }

        lock (ClassGate)
        {
            BorderStates[_window] = enabled;
        }

        Invalidate();
    }

    /// <inheritdoc/>
    public void Invalidate()
    {
        ThrowIfDisposed();
        if (_window != 0)
        {
            WindowsNativeMethods.InvalidateRect(_window, 0, false);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _enabled = false;
        try
        {
            DestroyNativeWindow();
        }
        finally
        {
            _disposed = true;
            IsVisible = false;
            TargetWindow = null;
            GC.SuppressFinalize(this);
        }
    }

    ~WindowsGeometryDashboardOverlayHost()
    {
        try
        {
            Dispose();
        }
        catch
        {
            // Finalization must not surface native cleanup failures.
        }
    }

    private static Vector2 ToDpi(
        Vector2 coordinate,
        Vector2 dpiMultiplier,
        bool dpiSourceAvailable) => !dpiSourceAvailable
        ? coordinate
        : new Vector2(
            coordinate.X / dpiMultiplier.X,
            coordinate.Y / dpiMultiplier.Y) + new Vector2(0.1, 0.1);

    internal static bool TryConvertBounds(
        Box2 physicalBounds,
        Vector2 dpiMultiplier,
        bool dpiSourceAvailable,
        out NativeBounds bounds)
    {
        bounds = default;
        if (!AreFinite(physicalBounds))
        {
            return false;
        }

        if (dpiSourceAvailable &&
            (!double.IsFinite(dpiMultiplier.X) ||
             !double.IsFinite(dpiMultiplier.Y) ||
             dpiMultiplier.X <= 0 ||
             dpiMultiplier.Y <= 0))
        {
            return false;
        }

        Vector2 topLeft = ToDpi(
            new Vector2(physicalBounds.Left, physicalBounds.Top),
            dpiMultiplier,
            dpiSourceAvailable);
        Vector2 bottomRight = ToDpi(
            new Vector2(physicalBounds.Right, physicalBounds.Bottom),
            dpiMultiplier,
            dpiSourceAvailable);
        if (!TryRoundToInt(topLeft.X, out int left) ||
            !TryRoundToInt(topLeft.Y, out int top) ||
            !TryRoundToInt(Math.Abs(bottomRight.X - topLeft.X), out int width) ||
            !TryRoundToInt(Math.Abs(bottomRight.Y - topLeft.Y), out int height))
        {
            return false;
        }

        bounds = new NativeBounds(left, top, width, height);
        return true;
    }

    private static bool AreFinite(Box2 bounds) =>
        double.IsFinite(bounds.Left) &&
        double.IsFinite(bounds.Top) &&
        double.IsFinite(bounds.Right) &&
        double.IsFinite(bounds.Bottom);

    private static bool TryRoundToInt(double value, out int result)
    {
        result = 0;
        if (!double.IsFinite(value))
        {
            return false;
        }

        double rounded = Math.Round(value);
        if (rounded < int.MinValue || rounded > int.MaxValue)
        {
            return false;
        }

        result = Convert.ToInt32(rounded);
        return true;
    }

    internal readonly record struct NativeBounds(int Left, int Top, int Width, int Height);

    private static void EnsureWindowClass()
    {
        lock (ClassGate)
        {
            if (_classAtom != 0)
            {
                return;
            }

            _windowProcedure = WindowProcedure;
            WindowsNativeMethods.WNDCLASS windowClass = new()
            {
                WindowProcedure = _windowProcedure,
                Instance = WindowsNativeMethods.GetModuleHandle(null),
                ClassName = ClassName
            };
            _classAtom = WindowsNativeMethods.RegisterClass(ref windowClass);
            if (_classAtom == 0 && Marshal.GetLastWin32Error() != 1410)
            {
                throw new InvalidOperationException(
                    $"Windows could not register the Geometry Dashboard overlay " +
                    $"class (Win32 error {Marshal.GetLastWin32Error()}).");
            }
        }
    }

    private static nint WindowProcedure(
        nint window,
        uint message,
        nint wParam,
        nint lParam)
    {
        if (message == WindowsNativeMethods.WindowMessageNcHitTest)
        {
            return WindowsNativeMethods.HitTestTransparent;
        }

        if (message == WindowsNativeMethods.WindowMessageEraseBackground)
        {
            return 1;
        }

        if (message == WindowsNativeMethods.WindowMessagePaint)
        {
            WindowsNativeMethods.PAINTSTRUCT paint;
            nint deviceContext = WindowsNativeMethods.BeginPaint(window, out paint);
            bool drawBorder;
            lock (ClassGate)
            {
                drawBorder = BorderStates.TryGetValue(window, out bool enabled) && enabled;
            }

            if (drawBorder &&
                WindowsNativeMethods.GetClientRect(
                    window,
                    out WindowsNativeMethods.RECT rectangle))
            {
                nint brush = WindowsNativeMethods.CreateSolidBrush(GreenYellow);
                if (brush != 0)
                {
                    for (int index = 0; index < BorderThickness; index++)
                    {
                        WindowsNativeMethods.FrameRect(
                            deviceContext,
                            ref rectangle,
                            brush);
                        rectangle.Left++;
                        rectangle.Top++;
                        rectangle.Right--;
                        rectangle.Bottom--;
                    }

                    WindowsNativeMethods.DeleteObject(brush);
                }
            }

            WindowsNativeMethods.EndPaint(window, ref paint);
            return 0;
        }

        if (message == WindowsNativeMethods.WindowMessageNcDestroy)
        {
            lock (ClassGate)
            {
                BorderStates.Remove(window);
            }
        }

        return WindowsNativeMethods.DefWindowProc(window, message, wParam, lParam);
    }

    private void HideNativeWindow()
    {
        if (_window == 0)
        {
            IsVisible = false;
            return;
        }

        WindowsNativeMethods.ShowWindow(_window, WindowsNativeMethods.ShowHide);
        IsVisible = false;
    }

    private void DestroyNativeWindow()
    {
        if (_window == 0)
        {
            IsVisible = false;
            return;
        }

        HideNativeWindow();
        WindowsNativeMethods.DestroyWindow(_window);
        lock (ClassGate)
        {
            BorderStates.Remove(_window);
        }

        _window = 0;
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
