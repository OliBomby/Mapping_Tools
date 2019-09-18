using Mapping_Tools.Classes.BeatmapHelper;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateCirclesFromHitObjects {
        List<RelevantCircle> GetRelevantObjects(List<HitObject> objects);
    }
}
