using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjectsFromRelevantPoints : IGenerateRelevantObjects {
        List<IRelevantObject> GetRelevantObjects(List<RelevantPoint> objects);
    }
}
