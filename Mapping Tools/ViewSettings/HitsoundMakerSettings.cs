using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.ViewSettings {
    public class HitsoundMakerSettings {
        public string BaseBeatmap { get; set; }
        public Sample DefaultSample { get; set; }
        public ObservableCollection<HitsoundLayer> HitsoundLayers { get; set; }

        public HitsoundMakerSettings() {
            HitsoundLayers = new ObservableCollection<HitsoundLayer>();
        }

        public HitsoundMakerSettings(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
        }
    }
}
