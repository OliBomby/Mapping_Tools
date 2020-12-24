using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes {
    public interface IHitsoundLayer {
        /// <summary>
        /// Contains all the times that this hitsound should play.
        /// </summary>
        SortedSet<double> Times { get; set; }

        SampleSet SampleSet { get; set; }

        Hitsound Hitsound { get; set; }

        int Priority { get; set; }

        ILayerSourceRef LayerSourceRef { get; set; }

        ISampleGeneratingArgs SampleGeneratingArgs { get; set; }

        /// <summary>
        /// Replaces <see cref="Times"/> with the times of all
        /// hitsound layers in the collection with matching
        /// <see cref="LayerSourceRef"/>.
        /// </summary>
        /// <param name="layers">The hitsound layers to reload from</param>
        void Reload(IEnumerable<IHitsoundLayer> layers);
    }
}