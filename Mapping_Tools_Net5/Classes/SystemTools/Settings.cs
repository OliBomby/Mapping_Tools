using System;
using System.Collections.Generic;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings : BindableBase {
        public List<string[]> RecentMaps { get; set; }
        public Rect? MainWindowRestoreBounds { get; set; }
        public bool MainWindowMaximized { get; set; }

        private string _osuPath;
        public string OsuPath {
            get => _osuPath;
            set => Set(ref _osuPath, value);
        }

        private string _songsPath;
        public string SongsPath {
            get => _songsPath;
            set => Set(ref _songsPath, value);
        }

        private string _backupsPath;
        public string BackupsPath {
            get => _backupsPath;
            set => Set(ref _backupsPath, value);
        }

        private string _osuConfigPath;
        public string OsuConfigPath {
            get => _osuConfigPath;
            set => Set(ref _osuConfigPath, value);
        }

        private bool _makeBackups;
        public bool MakeBackups {
            get => _makeBackups;
            set => Set(ref _makeBackups, value);
        }

        private bool _useEditorReader;
        public bool UseEditorReader {
            get => _useEditorReader;
            set => Set(ref _useEditorReader, value);
        }

        private bool _overrideOsuSave;
        public bool OverrideOsuSave {
            get => _overrideOsuSave;
            set => Set(ref _overrideOsuSave, value);
        }

        private bool _autoReload;
        public bool AutoReload {
            get => _autoReload;
            set => Set(ref _autoReload, value);
        }

        private Hotkey _quickRunHotkey;
        public Hotkey QuickRunHotkey {
            get => _quickRunHotkey;
            set => Set(ref _quickRunHotkey, value);
        }

        private bool _smartQuickRunEnabled;
        public bool SmartQuickRunEnabled {
            get => _smartQuickRunEnabled;
            set => Set(ref _smartQuickRunEnabled, value);
        }

        private string _noneQuickRunTool;
        public string NoneQuickRunTool {
            get => _noneQuickRunTool;
            set => Set(ref _noneQuickRunTool, value);
        }

        private string _singleQuickRunTool;
        public string SingleQuickRunTool {
            get => _singleQuickRunTool;
            set => Set(ref _singleQuickRunTool, value);
        }

        private string _multipleQuickRunTool;
        public string MultipleQuickRunTool {
            get => _multipleQuickRunTool;
            set => Set(ref _multipleQuickRunTool, value);
        }

        private Hotkey _betterSaveHotkey;
        public Hotkey BetterSaveHotkey {
            get => _betterSaveHotkey;
            set => Set(ref _betterSaveHotkey, value);
        }

        private int _maxBackupFiles;
        public int MaxBackupFiles {
            get => _maxBackupFiles;
            set => Set(ref _maxBackupFiles, value);
        }

        private bool _makePeriodicBackups;
        public bool MakePeriodicBackups {
            get => _makePeriodicBackups;
            set => Set(ref _makePeriodicBackups, value);
        }

        private TimeSpan _periodicBackupInterval;
        public TimeSpan PeriodicBackupInterval {
            get => _periodicBackupInterval;
            set => Set(ref _periodicBackupInterval, value);
        }

        private bool _currentBeatmapDefaultFolder;
        public bool CurrentBeatmapDefaultFolder {
            get => _currentBeatmapDefaultFolder;
            set => Set(ref _currentBeatmapDefaultFolder, value);
        }

        private Hotkey _quickUndoHotkey;
        public Hotkey QuickUndoHotkey {
            get => _quickUndoHotkey;
            set => Set(ref _quickUndoHotkey, value);
        }

        /// <summary>
        /// Makes a new Settings objects and initializes default settings.
        /// </summary>
        public Settings() {
            RecentMaps = new List<string[]>();
            MainWindowRestoreBounds = null;
            MainWindowMaximized = false;
            OsuPath = "";
            SongsPath = "";
            BackupsPath = "";
            MakeBackups = true;
            UseEditorReader = true;
            OverrideOsuSave = false;
            AutoReload = true;
            SmartQuickRunEnabled = true;
            NoneQuickRunTool = "<Current Tool>";
            SingleQuickRunTool = "<Current Tool>";
            MultipleQuickRunTool = "<Current Tool>";
            MaxBackupFiles = 1000;
            MakePeriodicBackups = true;
            PeriodicBackupInterval = TimeSpan.FromMinutes(10);
            CurrentBeatmapDefaultFolder = true;
        }

        public void CopyTo(Settings other) {
            foreach (var prop in typeof(Settings).GetProperties()) {
                if (!prop.CanRead || !prop.CanWrite) { continue; }
                prop.SetValue(other, prop.GetValue(this));
            }
        }
    }
}
