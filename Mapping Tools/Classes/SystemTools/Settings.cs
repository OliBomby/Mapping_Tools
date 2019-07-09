using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings : INotifyPropertyChanged {
        public List<String[]> RecentMaps { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public Settings() {
            RecentMaps = new List<String[]>();
            OsuPath = "";
            SongsPath = "";
            BackupsPath = "";
            MakeBackups = true;
        }

        public void AddRecentMaps(String path, DateTime date) {
            RecentMaps.RemoveAll(o => o[0] == path);
            if( RecentMaps.Count > 4 ) {
                try {
                    RecentMaps.Remove(RecentMaps.Last());
                }
                catch (ArgumentOutOfRangeException) {
                }
            }
            RecentMaps.Insert(0, new string[] { path, date.ToString()});
        }

        public void CopyTo(Settings other) {
            foreach (var prop in typeof(Settings).GetProperties()) {
                prop.SetValue(other, prop.GetValue(this));
            }
        }
    }
}
