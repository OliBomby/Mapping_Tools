using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjectsFromRelevantLines {
        List<IRelevantObject> GetRelevantObjects(List<RelevantLine> objects);
    }
}
