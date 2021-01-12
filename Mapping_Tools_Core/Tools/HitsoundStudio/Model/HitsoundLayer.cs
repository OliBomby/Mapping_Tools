using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class HitsoundLayer : IHitsoundLayer {
        public SortedSet<double> Times { get; set; }
        public SampleSet SampleSet { get; set; }
        public Hitsound Hitsound { get; set; }
        public int Priority { get; set; }
        public ILayerSourceRef LayerSourceRef { get; set; }
        public ISampleGeneratingArgs SampleGeneratingArgs { get; set; }

        public HitsoundLayer() {
            Times = new SortedSet<double>();
            SampleGeneratingArgs = new SampleGeneratingArgs();
        }

        public HitsoundLayer(IEnumerable<double> times, ILayerSourceRef layerSourceRef) {
            Times = new SortedSet<double>(times);
            LayerSourceRef = layerSourceRef;
            SampleGeneratingArgs = new SampleGeneratingArgs();
        }

        public void Reload(IEnumerable<IHitsoundLayer> layers) {
            if (LayerSourceRef == null) {
                return;
            }

            IEnumerable<IHitsoundLayer> sameLayer = layers.Where(o => LayerSourceRef.ReloadCompatible(o.LayerSourceRef));
            Times = new SortedSet<double>(sameLayer.SelectMany(o => o.Times));
        }
    }
}