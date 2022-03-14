using System.Collections.ObjectModel;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options;
using Mapping_Tools.Components.Graph;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class TumourGeneratorVm : BindableBase {
        #region Properties

        private ObservableCollection<TumourLayer> _tumourLayers;
        public ObservableCollection<TumourLayer> TumourLayers {
            get => _tumourLayers;
            set => Set(ref _tumourLayers, value);
        }

        private bool _justMiddleAnchors;
        public bool JustMiddleAnchors {
            get => _justMiddleAnchors;
            set => Set(ref _justMiddleAnchors, value);
        }

        [JsonIgnore]
        public string Path { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        [JsonIgnore]
        public bool Reload { get; set; }

        #endregion

        public TumourGeneratorVm() {
            TumourLayers = new ObservableCollection<TumourLayer>();
            JustMiddleAnchors = false;
        }
    }
}