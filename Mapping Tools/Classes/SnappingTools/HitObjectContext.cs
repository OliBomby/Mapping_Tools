using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools {
    public class HitObjectContext : RelevantObjectContext {
        public List<RelevantHitObject> HitObjects {
            set => Objects = value.Cast<IRelevantObject>().ToList();
            get => Objects.Cast<RelevantHitObject>().ToList();
        }

        public bool FindSimilar(RelevantHitObject hitObject, double acceptableDifference, out RelevantHitObject similarObject) {
            similarObject = HitObjects.First(o => hitObject.Difference(o) < acceptableDifference);
            return similarObject != null;
        }
    }
}