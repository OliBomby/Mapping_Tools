using System;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    /// <summary>
    /// Specific to a single <see cref="IHitsoundLayer"/>.
    /// Describes the relation between the <see cref="IHitsoundLayer"/> and its source material.
    /// </summary>
    public interface ILayerSourceRef : IEquatable<ILayerSourceRef> {
        /// <summary>
        /// Gets the <see cref="ILayerImportArgs"/> necessary to import
        /// at least this hitsound layer.
        /// </summary>
        /// <returns></returns>
        ILayerImportArgs GetLayerImportArgs();

        /// <summary>
        /// Checks if the provided <see cref="ILayerSourceRef"/>
        /// is equivalent or a subset of this.
        /// </summary>
        /// <param name="other">The source ref to check</param>
        /// <returns></returns>
        bool ReloadCompatible(ILayerSourceRef other);
    }
}