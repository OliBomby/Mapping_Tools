using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.BeatmapHelper {
    class HitObjectComparer : IEqualityComparer<HitObject> {
        public bool Equals(HitObject x, HitObject y) {
            return x.GetLine() == y.GetLine();
        }

        public int GetHashCode(HitObject obj) {
            return EqualityComparer<string>.Default.GetHashCode(obj.GetLine());
        }
    }
}
