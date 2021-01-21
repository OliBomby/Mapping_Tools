using System.Collections.Generic;
using Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public class HitsoundsLayerImportArgs : IHitsoundsLayerImportArgs {
        public HitsoundsLayerImportArgs(string path, bool discriminateVolumes, bool detectDuplicateSamples) {
            Path = path;
            DiscriminateVolumes = discriminateVolumes;
            DetectDuplicateSamples = detectDuplicateSamples;
        }

        public string Path { get; }
        public bool DiscriminateVolumes { get; }
        public bool DetectDuplicateSamples { get; }

        public bool Equals(ILayerImportArgs other) {
            return other is IHitsoundsLayerImportArgs o &&
                   Path == o.Path &&
                   DiscriminateVolumes == o.DiscriminateVolumes &&
                   DetectDuplicateSamples == o.DetectDuplicateSamples;
        }
    }
}