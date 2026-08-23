using System;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools;

/// <summary>Converts between screen, editor-grid, and relative playfield coordinates.</summary>
/// <remarks>
/// This class contains only the legacy geometry formulas. Screen discovery, osu! configuration,
/// DPI queries, and process/window access are supplied by the later platform adapter.
/// </remarks>
public sealed class CoordinateConverter
{
    /// <summary>Gets or sets the per-edge adjustment applied to the editor box.</summary>
    public Box2 EditorBoxOffset { get; set; } = new(0, 1, 0, 1);

    /// <summary>Gets the one-half-pixel correction used by legacy snapping.</summary>
    public Vector2 PositionSnapOffset => new(0.5, 0.5);

    /// <summary>Gets or sets the top-left position of the osu! window in screen coordinates.</summary>
    public Vector2 OsuWindowPosition { get; set; } = Vector2.Zero;

    /// <summary>Gets or sets the configured osu! client resolution.</summary>
    public Vector2 OsuResolution { get; set; } = new(1920, 1080);

    /// <summary>Gets or sets whether osu! is configured for fullscreen.</summary>
    public bool Fullscreen { get; set; } = true;

    /// <summary>Gets or sets whether osu! letterboxing is enabled.</summary>
    public bool Letterboxing { get; set; } = true;

    /// <summary>Gets or sets the osu! letterbox position value.</summary>
    public Vector2 LetterboxingPosition { get; set; } = new(0.5, 0.5);

    /// <summary>Gets or sets the screen bounds supplied by the platform adapter.</summary>
    public Box2 ScreenBox { get; set; } = new(0, 0, 1920, 1080);

    /// <summary>Gets or sets the platform DPI scale used for chrome dimensions.</summary>
    public Vector2 DpiMultiplier { get; set; } = Vector2.One;

    /// <summary>Gets or sets whether a platform DPI source was available for the last query.</summary>
    /// <remarks>When false, <see cref="ToDpi"/> preserves the legacy no-window fallback.</remarks>
    public bool DpiSourceAvailable { get; set; }

    /// <summary>Gets the editor client dimensions after the file bar.</summary>
    public Vector2 EditorResolution => OsuResolution - new Vector2(0, FilebarHeight);

    /// <summary>Gets the fixed osu! editor grid resolution in editor pixels.</summary>
    public Vector2 EditorGridResolution => new(512, 384);

    private double FilebarHeight => 24 * DpiMultiplier.Y;
    private double WindowChromeHeight => 24 * DpiMultiplier.Y;

    /// <summary>Gets the configured screen bounds.</summary>
    /// <returns>The screen bounds provided by the platform boundary.</returns>
    public Box2 GetScreenBox() => ScreenBox;

    /// <summary>Gets the platform DPI scale supplied to this converter.</summary>
    /// <returns>The current horizontal and vertical DPI multipliers.</returns>
    public Vector2 GetDpiMultiplier() => DpiMultiplier;

    private bool OsuFillsScreen => Fullscreen || Letterboxing || OsuResolution == new Vector2(ScreenBox.Right, ScreenBox.Bottom);

    /// <summary>Gets the screen rectangle containing the complete osu! window.</summary>
    /// <returns>The window rectangle including any non-fullscreen chrome.</returns>
    public Box2 GetOsuWindowBox()
    {
        Vector2 chromeAddition = OsuFillsScreen ? Vector2.Zero : new Vector2(2, 2 + WindowChromeHeight);
        return Letterboxing ? ScreenBox :
            OsuFillsScreen ? new Box2(Vector2.Zero, OsuResolution) :
            new Box2(OsuWindowPosition, OsuWindowPosition + OsuResolution + chromeAddition);
    }

    /// <summary>Gets the osu! window rectangle after removing non-fullscreen chrome.</summary>
    /// <returns>The content window rectangle.</returns>
    public Box2 GetOsuWindowBoxWithoutChrome()
    {
        Box2 osuWindow = GetOsuWindowBox();
        if (OsuFillsScreen) return osuWindow;
        osuWindow.Top += 1 + WindowChromeHeight;
        osuWindow.Left += 1;
        osuWindow.Right -= 1;
        osuWindow.Bottom -= 1;
        return osuWindow;
    }

    /// <summary>Gets the editor rectangle without menu bar or letterbox black space.</summary>
    /// <returns>The editor content rectangle.</returns>
    public Box2 GetEditorBox()
    {
        Box2 osuWindow = GetOsuWindowBoxWithoutChrome();
        osuWindow.Top += FilebarHeight;
        if (!Letterboxing) return AddBox2(osuWindow, EditorBoxOffset);

        Vector2 letterboxMultiplier = LetterboxingPosition / 200 + new Vector2(0.5, 0.5);
        Vector2 blackSpaceSize = new Vector2(osuWindow.Width, osuWindow.Height) - EditorResolution;
        Vector2 letterboxOffset = letterboxMultiplier * blackSpaceSize;
        Vector2 letterboxOffset2 = (Vector2.One - letterboxMultiplier) * blackSpaceSize;
        osuWindow.Left += letterboxOffset.X;
        osuWindow.Top += letterboxOffset.Y;
        osuWindow.Right -= letterboxOffset2.X;
        osuWindow.Bottom -= letterboxOffset2.Y;
        return AddBox2(osuWindow, EditorBoxOffset);
    }

    /// <summary>Gets the screen rectangle corresponding to the osu! editor grid.</summary>
    /// <returns>The grid rectangle from (0,0) to (512,384).</returns>
    public Box2 GetEditorGridBox()
    {
        Box2 editor = GetEditorBox();
        // Screen pixels per osu pixel
        double ratio = editor.Height / 480;
        Vector2 gridDimensions = EditorGridResolution * ratio;
        Vector2 emptySpace = new Vector2(editor.Width, editor.Height) - gridDimensions;
        Vector2 gridOffset = new(emptySpace.X / 2, emptySpace.Y / 4 * 3);
        editor.Left += gridOffset.X;
        editor.Top += gridOffset.Y;
        editor.Right = editor.Left + gridDimensions.X;
        editor.Bottom = editor.Top + gridDimensions.Y;
        return editor;
    }

    /// <summary>Converts a screen coordinate to an editor-grid coordinate.</summary>
    /// <param name="coord">The screen coordinate.</param>
    /// <returns>The editor coordinate.</returns>
    public Vector2 ScreenToEditorCoordinate(Vector2 coord)
    {
        Box2 editorGridBox = GetEditorGridBox();
        double ratioX = editorGridBox.Width / EditorGridResolution.X;
        double ratioY = editorGridBox.Height / EditorGridResolution.Y;
        return new Vector2((coord.X - PositionSnapOffset.X - editorGridBox.Left) / ratioX, (coord.Y - PositionSnapOffset.Y - editorGridBox.Top) / ratioY);
    }

    /// <summary>Converts an editor-grid coordinate to a screen coordinate.</summary>
    /// <param name="coord">The editor coordinate.</param>
    /// <returns>The screen coordinate.</returns>
    public Vector2 EditorToScreenCoordinate(Vector2 coord)
    {
        Box2 editorGridBox = GetEditorGridBox();
        double ratioX = editorGridBox.Width / EditorGridResolution.X;
        double ratioY = editorGridBox.Height / EditorGridResolution.Y;
        return new Vector2(coord.X * ratioX + editorGridBox.Left, coord.Y * ratioY + editorGridBox.Top) + PositionSnapOffset;
    }

    /// <summary>Converts an editor-grid coordinate to its offset within the editor rectangle.</summary>
    /// <param name="coord">The editor coordinate.</param>
    /// <returns>The editor-relative screen offset.</returns>
    public Vector2 EditorToRelativeCoordinate(Vector2 coord)
    {
        Box2 editor = GetEditorBox();
        // Screen pixels per osu pixel
        double ratio = editor.Height / 480;
        Vector2 gridDimensions = EditorGridResolution * ratio;
        Vector2 emptySpace = new Vector2(editor.Width, editor.Height) - gridDimensions;
        Vector2 gridOffset = new(emptySpace.X / 2, emptySpace.Y / 4 * 3);
        Box2 editorGridBox = GetEditorGridBox();
        double ratioX = editorGridBox.Width / EditorGridResolution.X;
        double ratioY = editorGridBox.Height / EditorGridResolution.Y;
        return new Vector2(coord.X * ratioX, coord.Y * ratioY) + gridOffset;
    }

    /// <summary>Scales an editor-space vector by the current grid-to-screen ratio.</summary>
    /// <param name="thing">The editor-space vector.</param>
    /// <returns>The screen-scaled vector.</returns>
    public Vector2 ScaleByRatio(Vector2 thing)
    {
        Box2 editorGridBox = GetEditorGridBox();
        return new Vector2(thing.X * editorGridBox.Width / EditorGridResolution.X, thing.Y * editorGridBox.Height / EditorGridResolution.Y);
    }

    /// <summary>Converts a physical coordinate using the supplied DPI multiplier and legacy offset.</summary>
    /// <param name="coord">The physical coordinate.</param>
    /// <returns>The logical coordinate with the legacy one-tenth adjustment.</returns>
    public Vector2 ToDpi(Vector2 coord) => !DpiSourceAvailable
        ? coord
        : new Vector2(coord.X / DpiMultiplier.X, coord.Y / DpiMultiplier.Y) + new Vector2(0.1, 0.1);

    /// <inheritdoc/>
    public override string ToString() => $"{ScreenBox}, {OsuWindowPosition}, {OsuResolution}, {Fullscreen}, {Letterboxing}, {LetterboxingPosition}";

    private static Box2 AddBox2(Box2 thisBox2, Box2 otherBox2) => new(thisBox2.Left + otherBox2.Left, thisBox2.Top + otherBox2.Top, thisBox2.Right + otherBox2.Right, thisBox2.Bottom + otherBox2.Bottom);
}
