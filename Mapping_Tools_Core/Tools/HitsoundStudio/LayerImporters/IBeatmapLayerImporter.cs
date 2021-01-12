using System.Collections.Generic;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters {
    public interface IBeatmapLayerImporter<in T> where T : ILayerImportArgs {
        /// <summary>
        /// Imports hitsound layers using the arguments.
        /// </summary>
        /// <returns>The imported hitsound layers</returns>
        IEnumerable<IHitsoundLayer> Import(T args);
    }
}