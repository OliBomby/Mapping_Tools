using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class TumourGeneratorVm : BindableBase {

        #region Properties

        public GraphState GraphState { get; set; }

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        [JsonIgnore]
        public bool Reload { get; set; }

        #endregion

        public TumourGeneratorVm() {
            Quick = false;
        }
    }
}