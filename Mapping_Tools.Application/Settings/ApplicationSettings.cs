using Mapping_Tools.ApplicationServices.Workspace;

namespace Mapping_Tools.ApplicationServices.Settings;

/// <summary>
/// Contains the user preferences persisted in the legacy-compatible
/// <c>config.json</c> document.
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Maintains recent-map entries from most to least recently selected while
    /// Infrastructure preserves their legacy two-element JSON-array representation.
    /// </summary>
    public List<RecentBeatmap> RecentMaps { get; set; } = [];

    /// <summary>
    /// Lists tool names pinned into the shell's favorites section.
    /// </summary>
    public List<string> FavoriteTools { get; set; } = [];

    /// <summary>
    /// Defines the main window's last non-maximized position and size.
    /// </summary>
    public WindowBounds? MainWindowRestoreBounds { get; set; }

    /// <summary>
    /// Indicates that the next session should restore the main window maximized.
    /// </summary>
    public bool MainWindowMaximized { get; set; }

    /// <summary>
    /// Identifies the directory containing the osu! executable and user configuration.
    /// </summary>
    public string OsuPath { get; set; } = "";

    /// <summary>
    /// Identifies osu!'s configured beatmap library, normally the <c>Songs</c> directory.
    /// </summary>
    public string SongsPath { get; set; } = "";

    /// <summary>
    /// Identifies the destination for automatic and user-requested map backups.
    /// </summary>
    public string BackupsPath { get; set; } = "";

    /// <summary>
    /// Identifies the current user's <c>osu!.{user}.cfg</c> file used to discover the beatmap library.
    /// </summary>
    public string OsuConfigPath { get; set; } = "";

    /// <summary>
    /// Requires destructive tools to create safety backups before modifying maps.
    /// </summary>
    public bool MakeBackups { get; set; } = true;

    /// <summary>
    /// Allows operations to prefer unsaved editor state exposed by Editor Reader.
    /// </summary>
    public bool UseEditorReader { get; set; } = true;

    /// <summary>
    /// Allows Mapping Tools to replace the editor's on-disk save when reconciling live state.
    /// </summary>
    public bool OverrideOsuSave { get; set; }

    /// <summary>
    /// Requests that active tools reload their input after the workspace selection changes.
    /// </summary>
    public bool AutoReload { get; set; } = true;

    /// <summary>
    /// Makes an ordinary tool Run action use that feature's quick path; global
    /// shortcut routing remains controlled by <see cref="SmartQuickRunEnabled"/>.
    /// </summary>
    public bool AlwaysQuickRun { get; set; }

    /// <summary>
    /// Defines the global key combination assigned to QuickRun.
    /// </summary>
    public HotkeySettings? QuickRunHotkey { get; set; }

    /// <summary>
    /// Enables selection-count-based QuickRun target resolution.
    /// </summary>
    public bool SmartQuickRunEnabled { get; set; } = true;

    /// <summary>
    /// Names the QuickRun target used when osu! has no selected hit objects.
    /// </summary>
    public string NoneQuickRunTool { get; set; } = "<Current Tool>";

    /// <summary>
    /// Names the QuickRun target used when osu! has exactly one selected hit object.
    /// </summary>
    public string SingleQuickRunTool { get; set; } = "<Current Tool>";

    /// <summary>
    /// Names the QuickRun target used when osu! has multiple selected hit objects.
    /// </summary>
    public string MultipleQuickRunTool { get; set; } = "<Current Tool>";

    /// <summary>
    /// Defines the global key combination assigned to BetterSave.
    /// </summary>
    public HotkeySettings? BetterSaveHotkey { get; set; }

    /// <summary>
    /// Limits the retained automatic backup set before older files are pruned.
    /// </summary>
    public int MaxBackupFiles { get; set; } = 1000;

    /// <summary>
    /// Enables timer-driven backups while an osu! editing session is active.
    /// </summary>
    public bool MakePeriodicBackups { get; set; } = true;

    /// <summary>
    /// Defines the elapsed time between timer-driven backup attempts.
    /// </summary>
    public TimeSpan PeriodicBackupInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Makes beatmap pickers start beside the first selected map, falling back
    /// to the configured Songs directory.
    /// </summary>
    public bool CurrentBeatmapDefaultFolder { get; set; } = true;

    /// <summary>
    /// Selects the light or dark application palette independently of the
    /// operating-system theme.
    /// </summary>
    public ApplicationTheme Theme { get; set; } = ApplicationTheme.Dark;

    /// <summary>
    /// Defines the global key combination assigned to QuickUndo.
    /// </summary>
    public HotkeySettings? QuickUndoHotkey { get; set; }

    /// <summary>
    /// Identifies the updater version the user chose to skip, or
    /// <see langword="null"/> when no version is skipped.
    /// </summary>
    public string? SkipVersion { get; set; }
}

/// <summary>
/// Preserves the numeric WPF <c>Key</c> and <c>ModifierKeys</c> values written
/// by legacy settings while platform adapters translate them to native input.
/// </summary>
/// <param name="Key">The persisted WPF key-enum value; zero disables the binding.</param>
/// <param name="Modifiers">Persisted Alt, Control, Shift, and Windows flag bits.</param>
public sealed record HotkeySettings(int Key, int Modifiers);

/// <summary>
/// Captures a window's normal-state position and size in device-independent pixels.
/// </summary>
/// <param name="X">The horizontal position of the left edge.</param>
/// <param name="Y">The vertical position of the top edge.</param>
/// <param name="Width">The window width.</param>
/// <param name="Height">The window height.</param>
public sealed record WindowBounds(double X, double Y, double Width, double Height);

/// <summary>
/// Identifies the persisted application palette selected in Preferences.
/// </summary>
public enum ApplicationTheme
{
    /// <summary>
    /// Uses dark surfaces with light foreground content.
    /// </summary>
    Dark,

    /// <summary>
    /// Uses light surfaces with dark foreground content.
    /// </summary>
    Light
}
