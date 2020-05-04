using Mapping_Tools.Classes.Tools;
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

        public string[] Snaps1 => new[] {"1/1", "1/2", "1/4", "1/8", "1/16"};

        public string[] Snaps2 => new[] {"1/1", "1/3", "1/6", "1/12"};

        public MapCleanerVm() {
            MapCleanerArgs = MapCleanerArgs.BasicResnap;
        }
    }
}
