using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjectsFromRelevantObjects {
        List<IRelevantObject> GetRelevantObjects(List<IRelevantObject> objects);
    }
}
