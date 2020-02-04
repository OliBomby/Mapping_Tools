using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using System.Collections.ObjectModel;

namespace Mapping_Tools.Viewmodels {
    public class HitsoundStudioVm : BindableBase {
        private string _baseBeatmap;
        public string BaseBeatmap {
            get => _baseBeatmap;
            set => Set(ref _baseBeatmap, value);
        }

        private Sample _defaultSample;
        public Sample DefaultSample {
            get => _defaultSample;
            set => Set(ref _defaultSample, value);
        }

        private string _exportFolder;
        public string ExportFolder {
            get => _exportFolder;
            set => Set(ref _exportFolder, value);
        }

        private bool _showResults;
        public bool ShowResults {
            get => _showResults;
            set => Set(ref _showResults, value);
        }

        public ObservableCollection<HitsoundLayer> HitsoundLayers { get; set; }

        public string EditTimes { get; set; }

        public HitsoundStudioVm() {
            BaseBeatmap = "";
            DefaultSample = new Sample { Priority = int.MaxValue};
            HitsoundLayers = new ObservableCollection<HitsoundLayer>();
        }

        public HitsoundStudioVm(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
        }
    }
}
