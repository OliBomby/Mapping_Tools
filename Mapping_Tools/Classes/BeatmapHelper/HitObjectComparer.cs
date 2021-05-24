using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class HitObjectComparer : IEqualityComparer<HitObject> {
        public bool CheckIsSelected { get; set; }
        public bool CheckPosition { get; set; }
        public bool CheckTime { get; set; }

        public HitObjectComparer(bool checkIsSelected = false, bool checkPosition = true, bool checkTime = true) {
            CheckIsSelected = checkIsSelected;
            CheckPosition = checkPosition;
            CheckTime = checkTime;
        }

        public bool Equals(HitObject x, HitObject y) {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            if (CheckIsSelected && x.IsSelected != y.IsSelected)
                return false;
            if (CheckPosition && x.Pos != y.Pos)
                return false;
            if (CheckTime && x.Time != y.Time)
                return false;
            if (!(x.Hitsounds == y.Hitsounds &&
                  x.Filename == y.Filename &&
                  x.SampleVolume == y.SampleVolume &&
                  x.CustomIndex == y.CustomIndex &&
                  x.AdditionSet == y.AdditionSet &&
                  x.SampleSet == y.SampleSet &&
                  x.NewCombo == y.NewCombo &&
                  x.ComboSkip == y.ComboSkip))
                return false;
            if (x.IsCircle && y.IsCircle) {
                return true;
            }
            if (x.IsSlider && y.IsSlider) {
                return x.SliderType == y.SliderType &&
                       (!CheckPosition || x.CurvePoints.SequenceEqual(y.CurvePoints)) &&
                    x.Repeat == y.Repeat &&
                    x.PixelLength == y.PixelLength &&
                    x.EdgeHitsounds.SequenceEqual(y.EdgeHitsounds) &&
                    x.EdgeSampleSets.SequenceEqual(y.EdgeSampleSets) &&
                    x.EdgeAdditionSets.SequenceEqual(y.EdgeAdditionSets);
            }
            if (x.IsSpinner && y.IsSpinner) {
                return x.EndTime == y.EndTime;
            }
            if (x.IsHoldNote && y.IsHoldNote) {
                return x.EndTime == y.EndTime;
            }

            return false;
        }

        public int GetHashCode(HitObject obj) {
            return EqualityComparer<string>.Default.GetHashCode(obj.GetLine());
        }
    }
}
