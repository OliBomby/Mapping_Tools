using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    class HitObjectComparer : IEqualityComparer<HitObject> {
        public bool Equals(HitObject x, HitObject y) {
            return x.PixelLength == y.PixelLength &&
                x.Time == y.Time &&
                x.ObjectType == y.ObjectType &&
                x.EndTime == y.EndTime &&
                x.Hitsounds == y.Hitsounds &&
                x.Repeat == y.Repeat &&
                x.SliderType == y.SliderType &&
                x.CurvePoints.SequenceEqual(y.CurvePoints) &&
                x.EdgeHitsounds.SequenceEqual(y.EdgeHitsounds) &&
                x.EdgeSampleSets.SequenceEqual(y.EdgeSampleSets) &&
                x.EdgeAdditionSets.SequenceEqual(y.EdgeAdditionSets) &&
                x.Pos == y.Pos &&
                x.Extras == y.Extras;
        }

        public int GetHashCode(HitObject obj) {
            return EqualityComparer<string>.Default.GetHashCode(obj.GetLine());
        }
    }
}
