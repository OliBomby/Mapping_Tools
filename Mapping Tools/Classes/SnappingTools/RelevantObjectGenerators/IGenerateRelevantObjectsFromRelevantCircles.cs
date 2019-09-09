using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjectsFromRelevantCircles {
        List<IRelevantObject> GetRelevantObjects(List<RelevantCircle> objects);
    }
}
