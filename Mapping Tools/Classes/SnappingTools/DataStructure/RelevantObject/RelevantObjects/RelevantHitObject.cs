using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
    public class RelevantHitObject : RelevantObject {
        public HitObject HitObject;

        public override bool IsSelected {
            get => HitObject.IsSelected;
            set => HitObject.IsSelected = value;
        }

        public RelevantHitObject(HitObject hitObject) {
            HitObject = hitObject;
        }

        public double Difference(RelevantHitObject other) {
            if (HitObject.ObjectType != other.HitObject.ObjectType) {
                return double.PositiveInfinity;
            }

            if (HitObject.SliderType != other.HitObject.SliderType) {
                return double.PositiveInfinity;
            }

            if (HitObject.CurvePoints.Count != other.HitObject.CurvePoints.Count) {
                return double.PositiveInfinity;
            }

            var differences = new List<double> {Vector2.DistanceSquared(HitObject.Pos, other.HitObject.Pos)};
            differences.AddRange(HitObject.CurvePoints.Select((t, i) => Vector2.DistanceSquared(t, other.HitObject.CurvePoints[i])));

            return differences.Sum() / differences.Count;
        }

        public override double DistanceTo(IRelevantObject relevantObject) {
            if (!(relevantObject is RelevantHitObject relevantHitObject)) {
                return double.PositiveInfinity;
            }

            if (HitObject.GetHitObjectType() != relevantHitObject.HitObject.GetHitObjectType()) {
                return double.PositiveInfinity;
            }

            return Vector2.Distance(HitObject.Pos, relevantHitObject.HitObject.Pos);
        }
    }
}
