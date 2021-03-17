using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Exporting {
    public interface IHitsoundExporter {
        void ExportHitsounds(ICollection<IHitsoundEvent> hitsounds, Beatmap beatmap);
    }
}