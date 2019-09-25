using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes {
    public interface IGeneratePointsFromHitObjects {
        List<RelevantPoint> GetRelevantObjects(List<HitObject> objects);
    }
}
