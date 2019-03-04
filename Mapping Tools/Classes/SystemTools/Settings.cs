using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings {
        public List<String[]> RecentMaps;

        public Settings() {
            RecentMaps = new List<String[]>();
        }

        public void AddRecentMaps(String path, DateTime date) {
            RecentMaps.RemoveAll(o => o[0] == path);
            if( RecentMaps.Count > 4 ) {
                try {
                    RecentMaps.Remove(RecentMaps.First());
                }
                catch(ArgumentOutOfRangeException argEx) {
                }
            }
            RecentMaps.Add(new string[] { path, date.ToString()});
        }
    }
}
