using System;
using System.Collections.Generic;
using Avalonia;

namespace Mapping_Tools.Desktop.Models;

public record UserSettings(
    List<string> RecentMaps,
    List<string> FavoriteTools,
    TimeSpan PeriodicBackupInterval,
    PixelRect? MainWindowRestoreBounds = null,
    bool MainWindowMaximized = false,
    string OsuPath = "",
    string SongsPath = "",
    string BackupsPath = "",
    string OsuConfigPath = "",
    bool MakeBackups = true,
    bool UseEditorReader = true,
    bool OverrideOsuSave = false,
    bool AutoReload = true,
    bool AlwaysQuickRun = false,
    string? QuickRunHotkey = null,
    bool SmartQuickRunEnabled = true,
    string NoneQuickRunTool = "<Current Tool>",
    string SingleQuickRunTool = "<Current Tool>",
    string MultipleQuickRunTool = "<Current Tool>",
    string? BetterSaveHotkey = null,
    int MaxBackupFiles = 1000,
    bool MakePeriodicBackups = true,
    bool CurrentBeatmapDefaultFolder = true,
    string? QuickUndoHotkey = null,
    Version? SkipVersion = null
)
{
    public UserSettings() : this(
        RecentMaps: [],
        FavoriteTools: [],
        PeriodicBackupInterval: TimeSpan.FromMinutes(10)
    ) { }
}