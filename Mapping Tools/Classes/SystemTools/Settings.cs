using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings : INotifyPropertyChanged {
        public List<string[]> RecentMaps { get; set; }

        public double? MainWindowWidth { get; set; }
        public double? MainWindowHeight { get; set; }
        public bool MainWindowMaximized { get; set; }

        private string osuPath;
        public string OsuPath {
            get { return osuPath; }
            set {
                if (osuPath != value) {
                    osuPath = value;
                    NotifyPropertyChanged("OsuPath");
                }
            }
        }

        private string songsPath;
        public string SongsPath {
            get { return songsPath; }
            set {
                if (songsPath != value) {
                    songsPath = value;
                    NotifyPropertyChanged("SongsPath");
                }
            }
        }

        private string backupsPath;
        public string BackupsPath {
            get { return backupsPath; }
            set {
                if (backupsPath != value) {
                    backupsPath = value;
                    NotifyPropertyChanged("BackupsPath");
                }
            }
        }

        private bool makeBackups;
        public bool MakeBackups {
            get { return makeBackups; }
            set {
                if (makeBackups != value) {
                    makeBackups = value;
                    NotifyPropertyChanged("MakeBackups");
                }
            }
        }

        private bool overrideOsuSave;
        public bool OverrideOsuSave {
            get { return overrideOsuSave; }
            set {
                if (overrideOsuSave != value) {
                    overrideOsuSave = value;
                    NotifyPropertyChanged("OverrideOsuSave");
                }
            }
        }

        private bool autoReload;
        public bool AutoReload {
            get { return autoReload; }
            set {
                if (autoReload != value) {
                    autoReload = value;
                    NotifyPropertyChanged("AutoReload");
                }
            }
        }

        private Hotkey quickRunHotkey;
        public Hotkey QuickRunHotkey {
            get { return quickRunHotkey; }
            set {
                if (quickRunHotkey != value) {
                    quickRunHotkey = value;
                    NotifyPropertyChanged("QuickRunHotkey");
                }
            }
        }

        private Hotkey betterSaveHotkey;
        public Hotkey BetterSaveHotkey {
            get { return betterSaveHotkey; }
            set {
                if (betterSaveHotkey != value) {
                    betterSaveHotkey = value;
                    NotifyPropertyChanged("BetterSaveHotkey");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public Settings() {
            RecentMaps = new List<string[]>();
            MainWindowWidth = null;
            MainWindowHeight = null;
            MainWindowMaximized = false;
            OsuPath = "";
            SongsPath = "";
            BackupsPath = "";
            MakeBackups = true;
            OverrideOsuSave = false;
            AutoReload = true;
        }

        public void AddRecentMap(string paths, DateTime date) {
            RecentMaps.RemoveAll(o => o[0] == paths);
            if( RecentMaps.Count > 19 ) {
                try {
                    RecentMaps.Remove(RecentMaps.Last());
                }
                catch (ArgumentOutOfRangeException) {
                }
            }
            RecentMaps.Insert(0, new string[] { paths, date.ToString()});
        }

        public void CopyTo(Settings other) {
            foreach (var prop in typeof(Settings).GetProperties()) {
                prop.SetValue(other, prop.GetValue(this));
            }
        }
    }
}
