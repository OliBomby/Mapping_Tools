using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Desktop.Settings.Models;

/// <summary>
///     Adds desktop-shell state and desktop-only interaction preferences to the
///     application settings shared with the Avalonia frontend.
/// </summary>
public sealed class DesktopApplicationSettings : ApplicationSettings
{
    /// <summary>
    ///     Lists tool names pinned into the desktop shell's favorites section.
    /// </summary>
    public List<string> FavoriteTools { get; set; } = [];

    /// <summary>
    ///     Defines the desktop main window's last non-maximized position and size.
    /// </summary>
    public WindowBounds? MainWindowRestoreBounds { get; set; }

    /// <summary>
    ///     Indicates that the next desktop session should restore the main window maximized.
    /// </summary>
    public bool MainWindowMaximized { get; set; }

    /// <summary>
    ///     Makes an ordinary desktop tool Run action use that feature's quick path.
    /// </summary>
    public bool AlwaysQuickRun { get; set; }

    /// <summary>
    ///     Defines the desktop-wide key combination assigned to QuickRun.
    /// </summary>
    public HotkeySettings? QuickRunHotkey { get; set; }

    /// <summary>
    ///     Defines the desktop-wide key combination assigned to BetterSave.
    /// </summary>
    public HotkeySettings? BetterSaveHotkey { get; set; }

    /// <summary>
    ///     Enables desktop BetterSave to replace the editor's on-disk save when
    ///     reconciling live state.
    /// </summary>
    public bool OverrideOsuSave { get; set; }

    /// <summary>
    ///     Selects the light or dark palette applied by the desktop frontend.
    /// </summary>
    public ApplicationTheme Theme { get; set; } = ApplicationTheme.Dark;

    /// <summary>
    ///     Defines the desktop-wide key combination assigned to QuickUndo.
    /// </summary>
    public HotkeySettings? QuickUndoHotkey { get; set; }
}
