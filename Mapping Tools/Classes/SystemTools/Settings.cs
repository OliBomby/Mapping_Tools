using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools {
    public class Settings {
        public List<String[]> RecentMaps;

        public Settings() {
            RecentMaps = new List<String[]>();
        }

        public bool AddRecentMaps(String path, DateTime date) {
            if( RecentMaps.Count > 4 ) {
                try {
                    RecentMaps.Remove(RecentMaps.First());
                }
                catch(ArgumentOutOfRangeException argEx) {
                    return false;
                }
            }

            RecentMaps.Add(new string[] { path, date.ToShortDateString() });
            return true;
        }
    }
}
