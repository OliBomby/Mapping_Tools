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

        private int copies;
        public int Copies {
            get => copies;
            set => Set(ref copies, value);
        }

        private double distance;
        public double Distance {
            get => distance;
            set => Set(ref distance, value);
        }

        private double localRotation;
        public double LocalRotation {
            get => localRotation;
            set => Set(ref localRotation, value);
        }

        private double globalRotation;
        public double GlobalRotation {
            get => globalRotation;
            set => Set(ref globalRotation, value);
        }

        #endregion

        public RadialDesignerVm() {
            Copies = 1;
            Distance = 0;
            LocalRotation = 0;
            GlobalRotation = 0;
        }
    }
}
