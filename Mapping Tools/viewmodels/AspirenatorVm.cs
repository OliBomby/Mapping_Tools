using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class AspirenatorVm : BindableBase {
        #region Properties

        [JsonIgnore]
        public string[] Paths;

        [JsonIgnore]
        public bool Quick;

        private double _leniency;
        public double Leniency {
            get => _leniency;
            set => Set(ref _leniency, value);
        }

        private bool _doZeroSliders;
        public bool DoZeroSliders {
            get => _doZeroSliders;
            set => Set(ref _doZeroSliders, value);
        }

        private bool _fixZeroSliders;
        public bool FixZeroSliders {
            get => _fixZeroSliders;
            set => Set(ref _fixZeroSliders, value);
        }

        private bool _doBugSliders;
        public bool DoBugSliders {
            get => _doBugSliders;
            set => Set(ref _doBugSliders, value);
        }

        #endregion

        public AspirenatorVm() {
            Leniency = 1;
            DoZeroSliders = true;
            FixZeroSliders = false;
            DoBugSliders = true;
        }
    }
}