namespace Mapping_Tools.ApplicationServices.Settings;

public sealed class ApplicationSettings
{
    public List<string[]> RecentMaps { get; set; } = [];

    public List<string> FavoriteTools { get; set; } = [];

    public WindowBounds? MainWindowRestoreBounds { get; set; }

    public bool MainWindowMaximized { get; set; }

    public string OsuPath { get; set; } = "";

    public string SongsPath { get; set; } = "";

    public string BackupsPath { get; set; } = "";

    public string OsuConfigPath { get; set; } = "";

    public bool MakeBackups { get; set; } = true;

    public bool UseEditorReader { get; set; } = true;

    public bool OverrideOsuSave { get; set; }

    public bool AutoReload { get; set; } = true;

    public bool AlwaysQuickRun { get; set; }

    public HotkeySettings? QuickRunHotkey { get; set; }

    public bool SmartQuickRunEnabled { get; set; } = true;

    public string NoneQuickRunTool { get; set; } = "<Current Tool>";

    public string SingleQuickRunTool { get; set; } = "<Current Tool>";

    public string MultipleQuickRunTool { get; set; } = "<Current Tool>";

    public HotkeySettings? BetterSaveHotkey { get; set; }

    public int MaxBackupFiles { get; set; } = 1000;

    public bool MakePeriodicBackups { get; set; } = true;

    public TimeSpan PeriodicBackupInterval { get; set; } = TimeSpan.FromMinutes(10);

    public bool CurrentBeatmapDefaultFolder { get; set; } = true;

    public HotkeySettings? QuickUndoHotkey { get; set; }

    public string? SkipVersion { get; set; }
}

public sealed record HotkeySettings(int Key, int Modifiers);

public sealed record WindowBounds(double X, double Y, double Width, double Height);
