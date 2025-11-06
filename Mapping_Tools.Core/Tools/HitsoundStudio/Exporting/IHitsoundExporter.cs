using System.Collections.Generic;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.HitsoundStudio.Model;

namespace Mapping_Tools.Core.Tools.HitsoundStudio.Exporting;

public interface IHitsoundExporter {
    void ExportHitsounds(ICollection<IHitsoundEvent> hitsounds, Beatmap beatmap);
}