using System;
using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes.LayerImportArgs {
    public interface ILayerImportArgs : IEquatable<ILayerImportArgs> {
        /// <summary>
        /// Imports hitsound layers using these arguments.
        /// </summary>
        /// <returns>The imported hitsound layers</returns>
        IEnumerable<IHitsoundLayer> Import();
    }
}