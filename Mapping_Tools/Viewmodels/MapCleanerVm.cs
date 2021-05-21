using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.Tools.MapCleanerStuff;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    /// <summary>
    /// Map Cleaner View Model
    /// </summary>
    public class MapCleanerVm {
        [JsonIgnore]
        public string[] Paths { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        public MapCleanerArgs MapCleanerArgs { get; set; }

        public MapCleanerVm() {
            MapCleanerArgs = MapCleanerArgs.BasicResnap;
        }
    }
}
