using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class HitsoundLayer : IHitsoundLayer {
        public string Name { get; set; }
        public SortedSet<double> Times { get; set; }
        public SampleSet SampleSet { get; set; }
        public Hitsound Hitsound { get; set; }
        public int Priority { get; set; }
        public ILayerSourceRef LayerSourceRef { get; set; }
        public ISampleGeneratingArgs SampleGeneratingArgs { get; set; }

        public HitsoundLayer() : this(new double[0], new SampleGeneratingArgs()) {}

        public HitsoundLayer(IEnumerable<double> times) : this(times, new SampleGeneratingArgs()) {}

        public HitsoundLayer(ISampleGeneratingArgs sampleGeneratingArgs) : this(new double[0], sampleGeneratingArgs) {}

        public HitsoundLayer(IEnumerable<double> times, ISampleGeneratingArgs sampleGeneratingArgs) {
            Times = new SortedSet<double>(times);
            SampleGeneratingArgs = sampleGeneratingArgs;
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