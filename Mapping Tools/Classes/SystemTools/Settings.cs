using Mapping_Tools.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings : INotifyPropertyChanged {
        public List<string[]> RecentMaps { get; set; }

        public double? MainWindowWidth { get; set; }
        public double? MainWindowHeight { get; set; }
        public bool MainWindowMaximized { get; set; }

        private string _osuPath;
        public string OsuPath {
            get => _osuPath;
            set {
                if (_osuPath == value) return;
                _osuPath = value;
                OnPropertyChanged();
            }
        }

        private string _songsPath;
        public string SongsPath {
            get => _songsPath;
            set {
                if (_songsPath == value) return;
                _songsPath = value;
                OnPropertyChanged();
            }
        }

        private string _backupsPath;
        public string BackupsPath {
            get => _backupsPath;
            set {
                if (_backupsPath == value) return;
                _backupsPath = value;
                OnPropertyChanged();
            }
        }

        private string _osuConfigPath;
        public string OsuConfigPath {
            get => _osuConfigPath;
            set {
                if (_osuConfigPath == value) return;
                _osuConfigPath = value;
                OnPropertyChanged();
            }
        }

        private bool _makeBackups;
        public bool MakeBackups {
            get => _makeBackups;
            set {
                if (_makeBackups == value) return;
                _makeBackups = value;
                OnPropertyChanged();
            }
        }

        private bool _overrideOsuSave;
        public bool OverrideOsuSave {
            get => _overrideOsuSave;
            set {
                if (_overrideOsuSave == value) return;
                _overrideOsuSave = value;
                OnPropertyChanged();
            }
        }

        private bool _autoReload;
        public bool AutoReload {
            get => _autoReload;
            set {
                if (_autoReload == value) return;
                _autoReload = value;
                OnPropertyChanged();
            }
        }

        private Hotkey _quickRunHotkey;
        public Hotkey QuickRunHotkey {
            get => _quickRunHotkey;
            set {
                if (_quickRunHotkey == value) return;
                _quickRunHotkey = value;
                OnPropertyChanged();
            }
        }

        private Hotkey _betterSaveHotkey;
        public Hotkey BetterSaveHotkey {
            get => _betterSaveHotkey;
            set {
                if (_betterSaveHotkey == value) return;
                _betterSaveHotkey = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            RecentMaps.Insert(0, new string[] { paths, date.ToString(CultureInfo.CurrentCulture)});
        }

        public void CopyTo(Settings other) {
            foreach (var prop in typeof(Settings).GetProperties()) {
                prop.SetValue(other, prop.GetValue(this));
            }
        }
    }
}
