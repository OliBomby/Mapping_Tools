namespace Mapping_Tools.ApplicationServices.Settings;

/// <summary>
/// Contains the user preferences persisted in the legacy-compatible
/// <c>config.json</c> document.
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Gets or sets recent-map entries as <c>[path, display date]</c> pairs,
    /// ordered from most to least recently opened.
    /// </summary>
    public List<string[]> RecentMaps { get; set; } = [];

    /// <summary>
    /// Gets or sets the tool names pinned as favorites.
    /// </summary>
    public List<string> FavoriteTools { get; set; } = [];

    /// <summary>
    /// Gets or sets the main-window bounds restored when the window is not maximized.
    /// </summary>
    public WindowBounds? MainWindowRestoreBounds { get; set; }

    /// <summary>
    /// Gets or sets whether the main window was maximized at shutdown.
    /// </summary>
    public bool MainWindowMaximized { get; set; }

    /// <summary>
    /// Gets or sets the osu! installation directory.
    /// </summary>
    public string OsuPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the directory containing installed beatmap folders.
    /// </summary>
    public string SongsPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the directory in which Mapping Tools stores map backups.
    /// </summary>
    public string BackupsPath { get; set; } = "";

    /// <summary>
    /// Gets or sets the current user's <c>osu!.{user}.cfg</c> path.
    /// </summary>
    public string OsuConfigPath { get; set; } = "";

    /// <summary>
    /// Gets or sets whether tools create safety backups before modifying maps.
    /// </summary>
    public bool MakeBackups { get; set; } = true;

    /// <summary>
    /// Gets or sets whether operations may use live state exposed by Editor Reader.
    /// </summary>
    public bool UseEditorReader { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Mapping Tools may replace the editor's on-disk save.
    /// </summary>
    public bool OverrideOsuSave { get; set; }

    /// <summary>
    /// Gets or sets whether tools reload their input when the current map changes.
    /// </summary>
    public bool AutoReload { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the QuickRun hotkey invokes the selected tool without
    /// first evaluating smart target rules.
    /// </summary>
    public bool AlwaysQuickRun { get; set; }

    /// <summary>
    /// Gets or sets the global hotkey assigned to QuickRun.
    /// </summary>
    public HotkeySettings? QuickRunHotkey { get; set; }

    /// <summary>
    /// Gets or sets whether QuickRun selects a tool based on the current map count.
    /// </summary>
    public bool SmartQuickRunEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the QuickRun target used when no current beatmap is selected.
    /// </summary>
    public string NoneQuickRunTool { get; set; } = "<Current Tool>";

    /// <summary>
    /// Gets or sets the QuickRun target used for one selected beatmap.
    /// </summary>
    public string SingleQuickRunTool { get; set; } = "<Current Tool>";

    /// <summary>
    /// Gets or sets the QuickRun target used for multiple selected beatmaps.
    /// </summary>
    public string MultipleQuickRunTool { get; set; } = "<Current Tool>";

    /// <summary>
    /// Gets or sets the global hotkey assigned to BetterSave.
    /// </summary>
    public HotkeySettings? BetterSaveHotkey { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of automatic backup files retained.
    /// </summary>
    public int MaxBackupFiles { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether backups are created periodically while editing.
    /// </summary>
    public bool MakePeriodicBackups { get; set; } = true;

    /// <summary>
    /// Gets or sets the elapsed time between periodic backups.
    /// </summary>
    public TimeSpan PeriodicBackupInterval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets whether file pickers default to the current beatmap's directory.
    /// </summary>
    public bool CurrentBeatmapDefaultFolder { get; set; } = true;

    /// <summary>
    /// Gets or sets the global hotkey assigned to QuickUndo.
    /// </summary>
    public HotkeySettings? QuickUndoHotkey { get; set; }

    /// <summary>
    /// Gets or sets the updater version the user chose to skip, or
    /// <see langword="null"/> when no version is skipped.
    /// </summary>
    public string? SkipVersion { get; set; }
}

/// <summary>
/// Stores a frontend-neutral keyboard key and modifier combination using the
/// numeric values preserved by the legacy settings format.
/// </summary>
/// <param name="Key">The numeric key value.</param>
/// <param name="Modifiers">The bitwise combination of modifier values.</param>
public sealed record HotkeySettings(int Key, int Modifiers);

/// <summary>
/// Stores a window's normal-state position and size in device-independent pixels.
/// </summary>
/// <param name="X">The horizontal position of the left edge.</param>
/// <param name="Y">The vertical position of the top edge.</param>
/// <param name="Width">The window width.</param>
/// <param name="Height">The window height.</param>
public sealed record WindowBounds(double X, double Y, double Width, double Height);
