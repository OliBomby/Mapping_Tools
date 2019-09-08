using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjectsFromRelevantPoints {
        List<IRelevantObject> GetRelevantObjects(List<RelevantPoint> objects);
    }
}
