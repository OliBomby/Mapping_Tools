using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjectsFromHitObjects : IGenerateRelevantObjects {
        List<IRelevantObject> GetRelevantObjects(List<HitObject> objects);
    }
}
