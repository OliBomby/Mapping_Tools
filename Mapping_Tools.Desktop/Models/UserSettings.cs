using System;
using System.Collections.Generic;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Mapping_Tools.Desktop.Models;

public partial class UserSettings : ReactiveObject {
    [Reactive]
    private string[] _currentBeatmaps;
    
    [Reactive]
    private List<string[]> _recentMaps;

    [Reactive]
    private List<string> _favoriteTools;

    [Reactive]
    private int[]? _mainWindowRestoreBounds;

    [Reactive]
    private bool _mainWindowMaximized;

    [Reactive]
    private string _osuPath;

    [Reactive]
    private string _songsPath;

    [Reactive]
    private string _backupsPath;

    [Reactive]
    private string _osuConfigPath;

    [Reactive]
    private bool _makeBackups;

    [Reactive]
    private bool _useEditorReader;

    [Reactive]
    private bool _overrideOsuSave;

    [Reactive]
    private bool _autoReload;

    [Reactive]
    private bool _alwaysQuickRun;

    [Reactive]
    private string? _quickRunHotkey;

    [Reactive]
    private bool _smartQuickRunEnabled;

    [Reactive]
    private string _noneQuickRunTool;

    [Reactive]
    private string _singleQuickRunTool;

    [Reactive]
    private string _multipleQuickRunTool;

    [Reactive]
    private string? _betterSaveHotkey;

    [Reactive]
    private int _maxBackupFiles;

    [Reactive]
    private bool _makePeriodicBackups;

    [Reactive]
    private TimeSpan _periodicBackupInterval;

    [Reactive]
    private bool _currentBeatmapDefaultFolder;

    [Reactive]
    private string? _quickUndoHotkey;
    
    [Reactive]
    private Version? _skipVersion;

    /// <summary>
    /// Makes a new Settings objects and initializes default settings.
    /// </summary>
    public UserSettings() {
        _currentBeatmaps = [];
        _recentMaps = [];
        _favoriteTools = [];
        _mainWindowRestoreBounds = null;
        _mainWindowMaximized = false;
        _osuPath = "";
        _songsPath = "";
        _backupsPath = "";
        _osuConfigPath = "";
        _makeBackups = true;
        _useEditorReader = true;
        _overrideOsuSave = false;
        _autoReload = true;
        _alwaysQuickRun = false;
        _smartQuickRunEnabled = true;
        _noneQuickRunTool = "<Current Tool>";
        _singleQuickRunTool = "<Current Tool>";
        _multipleQuickRunTool = "<Current Tool>";
        _maxBackupFiles = 1000;
        _makePeriodicBackups = true;
        _periodicBackupInterval = TimeSpan.FromMinutes(10);
        _currentBeatmapDefaultFolder = true;
        _skipVersion = null;
    }

    public void CopyTo(UserSettings other) {
        foreach (var prop in typeof(UserSettings).GetProperties()) {
            if (!prop.CanRead || !prop.CanWrite) { continue; }
            prop.SetValue(other, prop.GetValue(this));
        }
    }
}