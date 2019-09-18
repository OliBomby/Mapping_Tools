using Mapping_Tools.Classes.BeatmapHelper;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGeneratePointsFromHitObjects {
        List<RelevantPoint> GetRelevantObjects(List<HitObject> objects);
    }
}
