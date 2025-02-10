using System;
using System.Collections.Generic;
using System.Windows;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings : BindableBase {
        public List<string[]> RecentMaps { get; set; }
        public List<string> FavoriteTools { get; set; }
        public Rect? MainWindowRestoreBounds { get; set; }
        public bool MainWindowMaximized { get; set; }

        private string osuPath;
        public string OsuPath {
            get => osuPath;
            set => Set(ref osuPath, value);
        }

        private string songsPath;
        public string SongsPath {
            get => songsPath;
            set => Set(ref songsPath, value);
        }

        private string backupsPath;
        public string BackupsPath {
            get => backupsPath;
            set => Set(ref backupsPath, value);
        }

        private string osuConfigPath;
        public string OsuConfigPath {
            get => osuConfigPath;
            set => Set(ref osuConfigPath, value);
        }

        private bool makeBackups;
        public bool MakeBackups {
            get => makeBackups;
            set => Set(ref makeBackups, value);
        }

        private bool useEditorReader;
        public bool UseEditorReader {
            get => useEditorReader;
            set => Set(ref useEditorReader, value);
        }

        private bool overrideOsuSave;
        public bool OverrideOsuSave {
            get => overrideOsuSave;
            set => Set(ref overrideOsuSave, value);
        }

        private bool autoReload;
        public bool AutoReload {
            get => autoReload;
            set => Set(ref autoReload, value);
        }

        private bool alwaysQuickRun;
        public bool AlwaysQuickRun {
            get => alwaysQuickRun;
            set => Set(ref alwaysQuickRun, value);
        }

        private Hotkey quickRunHotkey;
        public Hotkey QuickRunHotkey {
            get => quickRunHotkey;
            set => Set(ref quickRunHotkey, value);
        }

        private bool smartQuickRunEnabled;
        public bool SmartQuickRunEnabled {
            get => smartQuickRunEnabled;
            set => Set(ref smartQuickRunEnabled, value);
        }

        private string noneQuickRunTool;
        public string NoneQuickRunTool {
            get => noneQuickRunTool;
            set => Set(ref noneQuickRunTool, value);
        }

        private string singleQuickRunTool;
        public string SingleQuickRunTool {
            get => singleQuickRunTool;
            set => Set(ref singleQuickRunTool, value);
        }

        private string multipleQuickRunTool;
        public string MultipleQuickRunTool {
            get => multipleQuickRunTool;
            set => Set(ref multipleQuickRunTool, value);
        }

        private Hotkey betterSaveHotkey;
        public Hotkey BetterSaveHotkey {
            get => betterSaveHotkey;
            set => Set(ref betterSaveHotkey, value);
        }

        private int maxBackupFiles;
        public int MaxBackupFiles {
            get => maxBackupFiles;
            set => Set(ref maxBackupFiles, value);
        }

        private bool makePeriodicBackups;
        public bool MakePeriodicBackups {
            get => makePeriodicBackups;
            set => Set(ref makePeriodicBackups, value);
        }

        private TimeSpan periodicBackupInterval;
        public TimeSpan PeriodicBackupInterval {
            get => periodicBackupInterval;
            set => Set(ref periodicBackupInterval, value);
        }

        private bool currentBeatmapDefaultFolder;
        public bool CurrentBeatmapDefaultFolder {
            get => currentBeatmapDefaultFolder;
            set => Set(ref currentBeatmapDefaultFolder, value);
        }

        private Hotkey quickUndoHotkey;
        public Hotkey QuickUndoHotkey {
            get => quickUndoHotkey;
            set => Set(ref quickUndoHotkey, value);
        }

        [CanBeNull] private Version skipVersion;
        [CanBeNull]
        public Version SkipVersion {
            get => skipVersion;
            set => Set(ref skipVersion, value);
        }

        /// <summary>
        /// Makes a new Settings objects and initializes default settings.
        /// </summary>
        public Settings() {
            RecentMaps = new List<string[]>();
            FavoriteTools = new List<string>();
            MainWindowRestoreBounds = null;
            MainWindowMaximized = false;
            OsuPath = "";
            SongsPath = "";
            BackupsPath = "";
            MakeBackups = true;
            UseEditorReader = true;
            OverrideOsuSave = false;
            AutoReload = true;
            AlwaysQuickRun = false;
            SmartQuickRunEnabled = true;
            NoneQuickRunTool = "<Current Tool>";
            SingleQuickRunTool = "<Current Tool>";
            MultipleQuickRunTool = "<Current Tool>";
            MaxBackupFiles = 1000;
            MakePeriodicBackups = true;
            PeriodicBackupInterval = TimeSpan.FromMinutes(10);
            CurrentBeatmapDefaultFolder = true;
            SkipVersion = null;
        }

        public void CopyTo(Settings other) {
            foreach (var prop in typeof(Settings).GetProperties()) {
                if (!prop.CanRead || !prop.CanWrite) { continue; }
                prop.SetValue(other, prop.GetValue(this));
            }
        }
    }
}
