using System.Collections.Generic;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes.LayerSourceRef;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes {
    public interface IHitsoundLayer {
        /// <summary>
        /// Contains all the times that this hitsound should play.
        /// </summary>
        [NotNull]
        SortedSet<double> Times { get; set; }

        SampleSet SampleSet { get; set; }

        Hitsound Hitsound { get; set; }

        int Priority { get; set; }

        [CanBeNull]
        ILayerSourceRef LayerSourceRef { get; set; }

        [NotNull]
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