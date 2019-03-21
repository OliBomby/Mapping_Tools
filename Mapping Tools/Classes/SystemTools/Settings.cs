using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings {
        public List<String[]> RecentMaps;
        public string OsuPath;
        public string SongsPath;

        public Settings() {
            RecentMaps = new List<String[]>();
            OsuPath = "";
            SongsPath = "";
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
    }
}
