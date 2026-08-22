#nullable disable
namespace Mapping_Tools.Core.Classes.BeatmapHelper {
        /// <summary>Compares serialized hit-object behavior with optional position and time checks.</summary>
    public class HitObjectComparer : IEqualityComparer<HitObject> {
        /// <summary>
        /// Controls whether object and slider-control-point positions participate in equality.
        /// </summary>
        public bool CheckPosition { get; set; }
        /// <summary>
        /// Controls whether start time participates in equality.
        /// </summary>
        public bool CheckTime { get; set; }

        /// <summary>Creates a comparer with configurable positional and time checks.</summary>
        /// <param name="checkPosition">Whether positions and slider control points must match.</param>
        /// <param name="checkTime">Whether start times must match.</param>
        public HitObjectComparer(bool checkPosition = true, bool checkTime = true) {
            CheckPosition = checkPosition;
            CheckTime = checkTime;
        }

        /// <summary>
        /// Compares common sample/combo data and the fields specific to each gameplay object kind.
        /// </summary>
        /// <param name="x">The first hit object.</param>
        /// <param name="y">The second hit object.</param>
        /// <returns><see langword="true"/> when all enabled and type-specific fields match exactly.</returns>
        public bool Equals(HitObject x, HitObject y) {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
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

        /// <summary>
        /// Hashes the object's complete serialized line.
        /// </summary>
        /// <param name="obj">The object to hash.</param>
        /// <returns>The ordinal string hash of <see cref="HitObject.GetLine"/>.</returns>
        public int GetHashCode(HitObject obj) {
            return EqualityComparer<string>.Default.GetHashCode(obj.GetLine());
        }
    }
}
