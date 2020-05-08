using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class AspirenatorVm : BindableBase {
        #region Properties

        [JsonIgnore]
        public string[] Paths;

        [JsonIgnore]
        public bool Quick;

        #endregion

        public AspirenatorVm() {
        }
    }
}