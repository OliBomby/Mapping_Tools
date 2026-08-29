using System.Runtime.InteropServices;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;
using SkiaSharp;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Owns a click-through, non-activating native popup window that follows the
///     active osu! window and retains the legacy overlay coordinate conversion.
/// </summary>
internal sealed class WindowsGeometryDashboardOverlayHost
{
    private const uint extended_style_tool_window = 0x00000080;
    private const uint extended_style_transparent = 0x00000020;
    private const uint extended_style_no_activate = 0x08000000;
    private const uint window_style_popup = 0x80000000;
    private static readonly SKColor green_yellow = new(173, 255, 47);
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
    internal WindowsGeometryDashboardOverlayHost(
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

    internal bool IsVisible { get; private set; }

    internal PlatformWindowId? TargetWindow { get; private set; }

    internal void Initialize(PlatformWindowId targetWindow)
    {
        ThrowIfDisposed();
        if (!isWindows()) return;

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

    internal void Enable()
    {
        ThrowIfDisposed();
        enabled = true;
    }

    internal void Disable()
    {
        ThrowIfDisposed();
        enabled = false;
        HideNativeWindow();
    }

    internal void Update(
        Box2 physicalBounds,
        Vector2 dpiMultiplier,
        bool dpiSourceAvailable)
    {
        ThrowIfDisposed();
        if (!isWindows() || !enabled || window == 0 || TargetWindow is null) return;

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
                state.PhysicalBounds = physicalBounds;
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

    internal void SetBorder(bool enabled)
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

    internal void SetScene(
        GeometryDashboardOverlayScene scene,
        WindowsGeometryDashboardCoordinateTransform transform)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(transform);
        if (disposed || window == 0) return;

        lock (classGate)
        {
            if (paintStates.TryGetValue(window, out var state))
            {
                state.Scene = scene;
                state.Transform = transform;
            }
        }

        Invalidate();
    }

    internal void Invalidate()
    {
        ThrowIfDisposed();
        if (window != 0) WindowsNativeMethods.InvalidateRect(window, 0, false);
    }

    internal void Dispose()
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
                paintStates.TryGetValue(window, out var paintState);
                bool drawBorder = borderStates.TryGetValue(window, out bool border) && border;
                if (paintState is not null || drawBorder)
                {
                    if (WindowsNativeMethods.GetClientRect(window, out var rectangle))
                    {
                        DrawOverlay(
                            deviceContext,
                            rectangle.Right - rectangle.Left,
                            rectangle.Bottom - rectangle.Top,
                            paintState,
                            drawBorder);
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

    private static void DrawOverlay(
        nint deviceContext,
        int width,
        int height,
        OverlayPaintState? state,
        bool drawBorder)
    {
        if (width <= 0 || height <= 0) return;

        nint memoryDeviceContext = WindowsNativeMethods.CreateCompatibleDC(deviceContext);
        if (memoryDeviceContext == 0)
            throw new InvalidOperationException("Windows could not create the Geometry Dashboard overlay buffer.");

        nint bitmap = 0;
        nint previousBitmap = 0;
        try
        {
            WindowsNativeMethods.BitmapInfoHeader bitmapInfo = new()
            {
                Size = (uint)Marshal.SizeOf<WindowsNativeMethods.BitmapInfoHeader>(),
                Width = width,
                Height = -height,
                Planes = 1,
                BitCount = 32,
                Compression = WindowsNativeMethods.BI_RGB,
            };
            bitmap = WindowsNativeMethods.CreateDibSection(
                deviceContext,
                ref bitmapInfo,
                WindowsNativeMethods.DIB_RGB_COLORS,
                out nint pixels,
                0,
                0);
            if (bitmap == 0 || pixels == 0)
                throw new InvalidOperationException("Windows could not allocate the Geometry Dashboard overlay buffer.");

            previousBitmap = WindowsNativeMethods.SelectObject(memoryDeviceContext, bitmap);
            if (previousBitmap == 0)
                throw new InvalidOperationException("Windows could not select the Geometry Dashboard overlay buffer.");

            SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKSurface surface = SKSurface.Create(imageInfo, pixels, checked(width * 4))
                                       ?? throw new InvalidOperationException("SkiaSharp could not create the overlay surface."))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                if (state is not null) DrawFrame(canvas, state);
                if (drawBorder) DrawBorder(canvas, width, height);
                surface.Flush();
            }

            WindowsNativeMethods.BlendFunction blend = new()
            {
                BlendOp = WindowsNativeMethods.AC_SRC_OVER,
                SourceConstantAlpha = byte.MaxValue,
                AlphaFormat = WindowsNativeMethods.AC_SRC_ALPHA,
            };
            if (!WindowsNativeMethods.AlphaBlend(
                    deviceContext,
                    0,
                    0,
                    width,
                    height,
                    memoryDeviceContext,
                    0,
                    0,
                    width,
                    height,
                    blend))
            {
                throw new InvalidOperationException("Windows could not copy the Geometry Dashboard overlay buffer.");
            }
        }
        finally
        {
            if (previousBitmap != 0) WindowsNativeMethods.SelectObject(memoryDeviceContext, previousBitmap);
            if (bitmap != 0) WindowsNativeMethods.DeleteObject(bitmap);
            WindowsNativeMethods.DeleteDC(memoryDeviceContext);
        }
    }

    private static void DrawFrame(SKCanvas canvas, OverlayPaintState state)
    {
        foreach (var shape in state.Scene.Shapes)
        {
            using SKPathEffect? pathEffect = ToPathEffect(shape.DashStyle);
            using SKPaint paint = new()
            {
                Color = ToSkiaColor(shape.Color, shape.Opacity),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = (float)Math.Max(0.1, shape.Thickness),
                PathEffect = pathEffect,
            };

            SKPoint start = ToClientPoint(shape.Start, state);
            switch (shape.Kind)
            {
                case GeometryDashboardOverlayShapeKind.Point:
                    canvas.DrawOval(
                        new SKRect(
                            start.X - (float)shape.Radius,
                            start.Y - (float)shape.Radius,
                            start.X + (float)shape.Radius,
                            start.Y + (float)shape.Radius),
                        paint);
                    break;
                case GeometryDashboardOverlayShapeKind.Circle:
                    double radius = state.Transform is null
                        ? 0
                        : state.Transform.ToDpi(state.Transform.ScaleByRatio(new Vector2(shape.Radius, 0))).X;
                    canvas.DrawOval(
                        new SKRect(
                            start.X - (float)radius,
                            start.Y - (float)radius,
                            start.X + (float)radius,
                            start.Y + (float)radius),
                        paint);
                    break;
                case GeometryDashboardOverlayShapeKind.Line:
                    canvas.DrawLine(start, ToClientPoint(shape.End, state), paint);
                    break;
                case GeometryDashboardOverlayShapeKind.Box:
                    canvas.DrawRect(
                        new SKRect(
                            start.X,
                            start.Y,
                            ToClientPoint(shape.End, state).X,
                            ToClientPoint(shape.End, state).Y),
                        paint);
                    break;
            }
        }
    }

    private static void DrawBorder(SKCanvas canvas, int width, int height)
    {
        using SKPaint paint = new()
        {
            Color = green_yellow,
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
        };

        for (int index = 0; index < border_thickness; index++)
        {
            float inset = index + 0.5f;
            canvas.DrawRect(new SKRect(inset, inset, width - inset, height - inset), paint);
        }
    }

    private static SKPoint ToClientPoint(Vector2 point, OverlayPaintState state)
    {
        if (state.Transform is null) return default;

        Vector2 overlayPoint = state.Transform.EditorToOverlayCoordinate(point);
        Vector2 relative = new(
            overlayPoint.X - state.PhysicalBounds.Left,
            overlayPoint.Y - state.PhysicalBounds.Top);
        var logical = state.Transform.ToDpi(relative);
        return new SKPoint((float)logical.X, (float)logical.Y);
    }

    private static SKColor ToSkiaColor(RgbaColour colour, double opacity)
    {
        int alpha = Convert.ToInt32(Math.Clamp(colour.A * opacity, 0, 255));
        return new SKColor(colour.R, colour.G, colour.B, (byte)alpha);
    }

    private static SKPathEffect? ToPathEffect(DashStylesEnum dashStyle)
    {
        return dashStyle switch
        {
            DashStylesEnum.Dash => SKPathEffect.CreateDash([3, 1], 0),
            DashStylesEnum.Dot => SKPathEffect.CreateDash([1, 1], 0),
            DashStylesEnum.DashSingleDot => SKPathEffect.CreateDash([3, 1, 1, 1], 0),
            DashStylesEnum.DashDoubleDot => SKPathEffect.CreateDash([3, 1, 1, 1, 1, 1], 0),
            _ => null,
        };
    }

    internal readonly record struct NativeBounds(int Left, int Top, int Width, int Height);

    private sealed class OverlayPaintState
    {
        public GeometryDashboardOverlayScene Scene { get; set; } = GeometryDashboardOverlayScene.Empty;
        public WindowsGeometryDashboardCoordinateTransform? Transform { get; set; }
        public Box2 PhysicalBounds { get; set; }
    }
}
