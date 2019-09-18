using Mapping_Tools.Classes.BeatmapHelper;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateLinesFromHitObjects {
        List<RelevantLine> GetRelevantObjects(List<HitObject> objects);
    }
}
