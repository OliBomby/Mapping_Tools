using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class RadialDesignerVm : BindableBase {
        #region Properties

        [JsonIgnore]
        public string[] Paths {
            get; set;
        }

        [JsonIgnore]
        public bool Quick {
            get; set;
        }

        // Add any properties you might need in the future

        #endregion

        public RadialDesignerVm() {
            // Initialize default values if necessary
        }

        // Add any methods or enums if needed in the future
    }
}
