using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.ViewSettings {
    public class HitsoundMakerSettings {
        public string BaseBeatmap;
        public Sample DefaultSample;
        public List<HitsoundLayer> HitsoundLayers;

        public HitsoundMakerSettings(string baseBeatmap, Sample defaultSample, List<HitsoundLayer> hitsoundLayers) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
        }
    }
}
