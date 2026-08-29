using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Immutable conversion snapshot for one osu! window, monitor, DPI state,
///     configuration state, and overlay offset.
/// </summary>
internal sealed class WindowsGeometryDashboardCoordinateTransform
{
    private readonly bool dpiSourceAvailable;
    private readonly bool fullscreen;
    private readonly Vector2 dpiMultiplier;
    private readonly Box2 editorBoxOffset;
    private readonly bool letterboxing;
    private readonly Vector2 letterboxingPosition;
    private readonly Vector2 osuResolution;
    private readonly Vector2 osuWindowPosition;
    private readonly Box2 screenBox;

    internal WindowsGeometryDashboardCoordinateTransform(
        GeometryDashboardWindow window,
        GeometryDashboardScreen? primaryScreen,
        WindowsGeometryDashboardOsuDisplaySettings display,
        Box2 editorBoxOffset)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(display);

        this.editorBoxOffset = editorBoxOffset;
        dpiMultiplier = window.DpiScale;
        dpiSourceAvailable = window.DpiSourceAvailable;
        fullscreen = display.Fullscreen;
        letterboxing = display.Letterboxing;
        letterboxingPosition = display.LetterboxingPosition;
        osuResolution = display.Resolution;
        osuWindowPosition = new Vector2(window.Bounds.Left, window.Bounds.Top);
        screenBox = primaryScreen?.Bounds ?? window.Bounds;
    }

    internal Vector2 EditorResolution => osuResolution - new Vector2(0, FilebarHeight);

    internal Vector2 EditorGridResolution => new(512, 384);

    internal Box2 EditorBox => GetEditorBox();

    internal Vector2 ScreenToEditorCoordinate(Vector2 coordinate)
    {
        var editorGridBox = GetEditorGridBox();
        double ratioX = editorGridBox.Width / EditorGridResolution.X;
        double ratioY = editorGridBox.Height / EditorGridResolution.Y;
        return new Vector2(
            (coordinate.X - 0.5 - editorGridBox.Left) / ratioX,
            (coordinate.Y - 0.5 - editorGridBox.Top) / ratioY);
    }

    internal Vector2 EditorToScreenCoordinate(Vector2 coordinate)
    {
        var editorGridBox = GetEditorGridBox();
        double ratioX = editorGridBox.Width / EditorGridResolution.X;
        double ratioY = editorGridBox.Height / EditorGridResolution.Y;
        return new Vector2(
                   coordinate.X * ratioX + editorGridBox.Left,
                   coordinate.Y * ratioY + editorGridBox.Top)
               + new Vector2(0.5, 0.5);
    }

    internal Vector2 EditorToOverlayCoordinate(Vector2 coordinate)
    {
        var editor = GetEditorBox();
        var relative = EditorToRelativeCoordinate(coordinate);
        return new Vector2(
            editor.Left + relative.X,
            editor.Top + relative.Y);
    }

    internal Vector2 ScaleByRatio(Vector2 value)
    {
        var editorGridBox = GetEditorGridBox();
        return new Vector2(
            value.X * editorGridBox.Width / EditorGridResolution.X,
            value.Y * editorGridBox.Height / EditorGridResolution.Y);
    }

    internal Vector2 ToDpi(Vector2 coordinate)
    {
        return !dpiSourceAvailable
            ? coordinate
            : new Vector2(
                  coordinate.X / dpiMultiplier.X,
                  coordinate.Y / dpiMultiplier.Y)
              + new Vector2(0.1, 0.1);
    }

    internal Vector2 GetDpiMultiplier()
    {
        return dpiMultiplier;
    }

    internal bool DpiSourceAvailable => dpiSourceAvailable;

    private double FilebarHeight => 24 * dpiMultiplier.Y;

    private double WindowChromeHeight => 24 * dpiMultiplier.Y;

    private bool OsuFillsScreen => fullscreen || letterboxing || osuResolution == new Vector2(screenBox.Right, screenBox.Bottom);

    private Box2 GetOsuWindowBox()
    {
        var chromeAddition = OsuFillsScreen ? Vector2.Zero : new Vector2(2, 2 + WindowChromeHeight);
        return letterboxing
            ? screenBox
            : OsuFillsScreen
                ? new Box2(Vector2.Zero, osuResolution)
                : new Box2(osuWindowPosition, osuWindowPosition + osuResolution + chromeAddition);
    }

    private Box2 GetEditorBox()
    {
        var osuWindow = GetOsuWindowBoxWithoutChrome();
        osuWindow.Top += FilebarHeight;
        if (!letterboxing) return AddBox2(osuWindow, editorBoxOffset);

        var letterboxMultiplier = letterboxingPosition / 200 + new Vector2(0.5, 0.5);
        var blackSpaceSize = new Vector2(osuWindow.Width, osuWindow.Height) - EditorResolution;
        var letterboxOffset = letterboxMultiplier * blackSpaceSize;
        var letterboxOffset2 = (Vector2.One - letterboxMultiplier) * blackSpaceSize;
        osuWindow.Left += letterboxOffset.X;
        osuWindow.Top += letterboxOffset.Y;
        osuWindow.Right -= letterboxOffset2.X;
        osuWindow.Bottom -= letterboxOffset2.Y;
        return AddBox2(osuWindow, editorBoxOffset);
    }

    private Box2 GetOsuWindowBoxWithoutChrome()
    {
        var osuWindow = GetOsuWindowBox();
        if (OsuFillsScreen) return osuWindow;

        osuWindow.Top += 1 + WindowChromeHeight;
        osuWindow.Left += 1;
        osuWindow.Right -= 1;
        osuWindow.Bottom -= 1;
        return osuWindow;
    }

    private Box2 GetEditorGridBox()
    {
        var editor = GetEditorBox();
        double ratio = editor.Height / 480;
        var gridDimensions = EditorGridResolution * ratio;
        var emptySpace = new Vector2(editor.Width, editor.Height) - gridDimensions;
        Vector2 gridOffset = new(emptySpace.X / 2, emptySpace.Y / 4 * 3);
        editor.Left += gridOffset.X;
        editor.Top += gridOffset.Y;
        editor.Right = editor.Left + gridDimensions.X;
        editor.Bottom = editor.Top + gridDimensions.Y;
        return editor;
    }

    private Vector2 EditorToRelativeCoordinate(Vector2 coordinate)
    {
        var editor = GetEditorBox();
        double ratio = editor.Height / 480;
        var gridDimensions = EditorGridResolution * ratio;
        var emptySpace = new Vector2(editor.Width, editor.Height) - gridDimensions;
        Vector2 gridOffset = new(emptySpace.X / 2, emptySpace.Y / 4 * 3);
        var editorGridBox = GetEditorGridBox();
        double ratioX = editorGridBox.Width / EditorGridResolution.X;
        double ratioY = editorGridBox.Height / EditorGridResolution.Y;
        return new Vector2(coordinate.X * ratioX, coordinate.Y * ratioY) + gridOffset;
    }

    private static Box2 AddBox2(Box2 first, Box2 second)
    {
        return new Box2(
            first.Left + second.Left,
            first.Top + second.Top,
            first.Right + second.Right,
            first.Bottom + second.Bottom);
    }
}

/// <summary>Contains the osu! display values read from the stable client configuration.</summary>
/// <param name="Resolution">The configured client resolution in physical pixels.</param>
/// <param name="Fullscreen">Whether osu! uses fullscreen placement.</param>
/// <param name="Letterboxing">Whether osu! preserves the playfield aspect ratio.</param>
/// <param name="LetterboxingPosition">The configured letterbox position.</param>
internal sealed record WindowsGeometryDashboardOsuDisplaySettings(
    Vector2 Resolution,
    bool Fullscreen,
    bool Letterboxing,
    Vector2 LetterboxingPosition)
{
    internal static WindowsGeometryDashboardOsuDisplaySettings Defaults { get; } = new(
        new Vector2(1920, 1080),
        true,
        true,
        new Vector2(0.5, 0.5));
}
