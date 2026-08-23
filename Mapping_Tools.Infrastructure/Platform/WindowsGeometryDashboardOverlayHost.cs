using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Creates native target-bound overlay hosts for Geometry Dashboard.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayHostFactory : IGeometryDashboardOverlayHostFactory
{
    private readonly Func<bool> isWindows;
    private readonly IGeometryDashboardWindowService windows;

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
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public IGeometryDashboardOverlayHost Create()
    {
        return new WindowsGeometryDashboardOverlayHost(windows, isWindows);
    }
}

/// <summary>
///     Owns a click-through, non-activating native popup window that follows the
///     active osu! window and retains the legacy overlay coordinate conversion.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayHost : IGeometryDashboardOverlayHost
{
    private const uint extended_style_tool_window = 0x00000080;
    private const uint extended_style_transparent = 0x00000020;
    private const uint extended_style_no_activate = 0x08000000;
    private const uint window_style_popup = 0x80000000;
    private const uint green_yellow = 0x002FFFAD;
    private const int border_thickness = 3;
    private static readonly object classGate = new();
    private static readonly Dictionary<nint, bool> borderStates = [];
    private static readonly Dictionary<nint, OverlayPaintState> paintStates = [];

    private static readonly string className =
        "MappingTools.GeometryDashboardOverlayWindow";

    private static WindowsNativeMethods.WindowProcedure? windowProcedure;
    private static ushort classAtom;
    private readonly Func<bool> isWindows;

    private readonly IGeometryDashboardWindowService windows;
    private bool borderEnabled;
    private bool disposed;
    private bool enabled;
    private nint window;

    /// <summary>
    ///     Creates a native overlay host using the supplied window tracker.
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
        this.windows = windows ?? throw new ArgumentNullException(nameof(windows));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
    public bool IsSupported => isWindows() && windows.IsSupported;

    /// <inheritdoc />
    public bool IsVisible { get; private set; }

    /// <inheritdoc />
    public PlatformWindowId? TargetWindow { get; private set; }

    /// <inheritdoc />
    public void Initialize(PlatformWindowId targetWindow)
    {
        ThrowIfDisposed();
        if (!IsSupported) return;

        if (targetWindow.IsEmpty)
            throw new ArgumentException(
                "A non-empty target window is required.",
                nameof(targetWindow));

        TargetWindow = null;
        DestroyNativeWindow();
        EnsureWindowClass();
        window = WindowsNativeMethods.CreateWindowEx(
            extended_style_tool_window | extended_style_transparent | extended_style_no_activate,
            className,
            string.Empty,
            window_style_popup,
            0,
            0,
            1,
            1,
            0,
            0,
            WindowsNativeMethods.GetModuleHandle(null),
            0);
        if (window == 0)
            throw new InvalidOperationException(
                $"Windows could not create the Geometry Dashboard overlay " + $"(Win32 error {Marshal.GetLastWin32Error()}).");

        lock (classGate)
        {
            borderStates[window] = borderEnabled;
            paintStates[window] = new OverlayPaintState();
        }

        TargetWindow = targetWindow;
        IsVisible = false;
    }

    /// <inheritdoc />
    public void Enable()
    {
        ThrowIfDisposed();
        enabled = true;
    }

    /// <inheritdoc />
    public void Disable()
    {
        ThrowIfDisposed();
        enabled = false;
        HideNativeWindow();
    }

    /// <inheritdoc />
    public void Update(
        Box2 physicalBounds,
        Vector2 dpiMultiplier,
        bool dpiSourceAvailable)
    {
        ThrowIfDisposed();
        if (!IsSupported || !enabled || window == 0 || TargetWindow is null) return;

        var target = windows.GetWindow(TargetWindow.Value);
        if (target is null || !target.IsActivated)
        {
            HideNativeWindow();
            return;
        }

        if (!TryConvertBounds(
                physicalBounds,
                dpiMultiplier,
                dpiSourceAvailable,
                out var nativeBounds))
        {
            HideNativeWindow();
            return;
        }

        lock (classGate)
        {
            if (paintStates.TryGetValue(window, out var state))
            {
                state.PhysicalBounds = physicalBounds;
                state.DpiMultiplier = dpiMultiplier;
                state.DpiSourceAvailable = dpiSourceAvailable;
            }
        }

        WindowsNativeMethods.ShowWindow(
            window,
            WindowsNativeMethods.SHOW_NO_ACTIVATE);
        if (!WindowsNativeMethods.SetWindowPos(
                window,
                WindowsNativeMethods.TopMostWindow,
                nativeBounds.Left,
                nativeBounds.Top,
                nativeBounds.Width,
                nativeBounds.Height,
                WindowsNativeMethods.SET_WINDOW_POS_NO_ACTIVATE | WindowsNativeMethods.SET_WINDOW_POS_NO_SEND_CHANGING))
        {
            HideNativeWindow();
            return;
        }

        IsVisible = true;
    }

    /// <inheritdoc />
    public void SetBorder(bool enabled)
    {
        if (disposed) return;

        borderEnabled = enabled;
        if (window == 0) return;

        lock (classGate)
        {
            borderStates[window] = enabled;
        }

        Invalidate();
    }

    /// <inheritdoc />
    public void SetFrame(GeometryDashboardOverlayFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (disposed || window == 0) return;

        lock (classGate)
        {
            if (paintStates.TryGetValue(window, out var state)) state.Frame = frame;
        }

        Invalidate();
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        ThrowIfDisposed();
        if (window != 0) WindowsNativeMethods.InvalidateRect(window, 0, false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;

        enabled = false;
        try
        {
            DestroyNativeWindow();
        }
        finally
        {
            disposed = true;
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
        bool dpiSourceAvailable)
    {
        return !dpiSourceAvailable
            ? coordinate
            : new Vector2(
                  coordinate.X / dpiMultiplier.X,
                  coordinate.Y / dpiMultiplier.Y)
              + new Vector2(0.1, 0.1);
    }

    internal static bool TryConvertBounds(
        Box2 physicalBounds,
        Vector2 dpiMultiplier,
        bool dpiSourceAvailable,
        out NativeBounds bounds)
    {
        bounds = default;
        if (!AreFinite(physicalBounds)) return false;

        if (dpiSourceAvailable && (!double.IsFinite(dpiMultiplier.X) || !double.IsFinite(dpiMultiplier.Y) || dpiMultiplier.X <= 0 || dpiMultiplier.Y <= 0))
            return false;

        var topLeft = ToDpi(
            new Vector2(physicalBounds.Left, physicalBounds.Top),
            dpiMultiplier,
            dpiSourceAvailable);
        var bottomRight = ToDpi(
            new Vector2(physicalBounds.Right, physicalBounds.Bottom),
            dpiMultiplier,
            dpiSourceAvailable);
        if (!TryRoundToInt(topLeft.X, out int left)
            || !TryRoundToInt(topLeft.Y, out int top)
            || !TryRoundToInt(Math.Abs(bottomRight.X - topLeft.X), out int width)
            || !TryRoundToInt(Math.Abs(bottomRight.Y - topLeft.Y), out int height))
            return false;

        bounds = new NativeBounds(left, top, width, height);
        return true;
    }

    private static bool AreFinite(Box2 bounds)
    {
        return double.IsFinite(bounds.Left) && double.IsFinite(bounds.Top) && double.IsFinite(bounds.Right) && double.IsFinite(bounds.Bottom);
    }

    private static bool TryRoundToInt(double value, out int result)
    {
        result = 0;
        if (!double.IsFinite(value)) return false;

        double rounded = Math.Round(value);
        if (rounded < int.MinValue || rounded > int.MaxValue) return false;

        result = Convert.ToInt32(rounded);
        return true;
    }

    private static void EnsureWindowClass()
    {
        lock (classGate)
        {
            if (classAtom != 0) return;

            windowProcedure = WindowProcedure;
            WindowsNativeMethods.Wndclass windowClass = new()
            {
                WindowProcedure = windowProcedure,
                Instance = WindowsNativeMethods.GetModuleHandle(null),
                ClassName = className,
            };
            classAtom = WindowsNativeMethods.RegisterClass(ref windowClass);
            if (classAtom == 0 && Marshal.GetLastWin32Error() != 1410)
                throw new InvalidOperationException(
                    $"Windows could not register the Geometry Dashboard overlay " + $"class (Win32 error {Marshal.GetLastWin32Error()}).");
        }
    }

    private static nint WindowProcedure(
        nint window,
        uint message,
        nint wParam,
        nint lParam)
    {
        if (message == WindowsNativeMethods.WINDOW_MESSAGE_NC_HIT_TEST) return WindowsNativeMethods.HIT_TEST_TRANSPARENT;

        if (message == WindowsNativeMethods.WINDOW_MESSAGE_ERASE_BACKGROUND) return 1;

        if (message == WindowsNativeMethods.WINDOW_MESSAGE_PAINT)
        {
            WindowsNativeMethods.Paintstruct paint;
            nint deviceContext = WindowsNativeMethods.BeginPaint(window, out paint);
            lock (classGate)
            {
                if (paintStates.TryGetValue(window, out var paintState)) DrawFrame(deviceContext, paintState);

                if (borderStates.TryGetValue(window, out bool drawBorder)
                    && drawBorder
                    && WindowsNativeMethods.GetClientRect(
                        window,
                        out var rectangle))
                {
                    nint brush = WindowsNativeMethods.CreateSolidBrush(green_yellow);
                    if (brush != 0)
                    {
                        for (int index = 0; index < border_thickness; index++)
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
            }

            WindowsNativeMethods.EndPaint(window, ref paint);
            return 0;
        }

        if (message == WindowsNativeMethods.WINDOW_MESSAGE_NC_DESTROY)
            lock (classGate)
            {
                borderStates.Remove(window);
                paintStates.Remove(window);
            }

        return WindowsNativeMethods.DefWindowProc(window, message, wParam, lParam);
    }

    private void HideNativeWindow()
    {
        if (window == 0)
        {
            IsVisible = false;
            return;
        }

        WindowsNativeMethods.ShowWindow(window, WindowsNativeMethods.SHOW_HIDE);
        IsVisible = false;
    }

    private void DestroyNativeWindow()
    {
        if (window == 0)
        {
            IsVisible = false;
            return;
        }

        HideNativeWindow();
        WindowsNativeMethods.DestroyWindow(window);
        lock (classGate)
        {
            borderStates.Remove(window);
            paintStates.Remove(window);
        }

        window = 0;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static void DrawFrame(nint deviceContext, OverlayPaintState state)
    {
        using var graphics = Graphics.FromHdc(deviceContext);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (var shape in state.Frame.Shapes)
        {
            using Pen pen = new(
                ToDrawingColor(shape.Color, shape.Opacity),
                (float)Math.Max(0.1, shape.Thickness));
            pen.DashStyle = ToDashStyle(shape.DashStyle);
            var start = ToClientPoint(shape.Start, state);
            switch (shape.Kind)
            {
                case GeometryDashboardOverlayShapeKind.Point:
                case GeometryDashboardOverlayShapeKind.Circle:
                    graphics.DrawEllipse(
                        pen,
                        start.X - (float)shape.Radius,
                        start.Y - (float)shape.Radius,
                        (float)shape.Radius * 2,
                        (float)shape.Radius * 2);
                    break;
                case GeometryDashboardOverlayShapeKind.Line:
                    graphics.DrawLine(pen, start, ToClientPoint(shape.End, state));
                    break;
            }
        }
    }

    private static PointF ToClientPoint(Vector2 point, OverlayPaintState state)
    {
        Vector2 relative = new(
            point.X - state.PhysicalBounds.Left,
            point.Y - state.PhysicalBounds.Top);
        var logical = ToDpi(relative, state.DpiMultiplier, state.DpiSourceAvailable);
        return new PointF((float)logical.X, (float)logical.Y);
    }

    private static Color ToDrawingColor(RgbaColour colour, double opacity)
    {
        int alpha = Convert.ToInt32(Math.Clamp(colour.A * opacity, 0, 255));
        return Color.FromArgb(alpha, colour.R, colour.G, colour.B);
    }

    private static DashStyle ToDashStyle(DashStylesEnum dashStyle)
    {
        return dashStyle switch
        {
            DashStylesEnum.Dash => DashStyle.Dash,
            DashStylesEnum.Dot => DashStyle.Dot,
            DashStylesEnum.DashSingleDot => DashStyle.DashDot,
            DashStylesEnum.DashDoubleDot => DashStyle.DashDotDot,
            _ => DashStyle.Solid,
        };
    }

    internal readonly record struct NativeBounds(int Left, int Top, int Width, int Height);

    private sealed class OverlayPaintState
    {
        public GeometryDashboardOverlayFrame Frame { get; set; } = GeometryDashboardOverlayFrame.Empty;
        public Box2 PhysicalBounds { get; set; }
        public Vector2 DpiMultiplier { get; set; } = Vector2.One;
        public bool DpiSourceAvailable { get; set; }
    }
}
