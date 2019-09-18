using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateLinesFromRelevantObjects {
        List<RelevantLine> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles);
    }
}
