using Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.HitObjects;

public class HitObjectComparer(bool checkIsSelected = false, bool checkPosition = true, bool checkTime = true) : IEqualityComparer<HitObject> {
    private readonly HitObjectEncoder _hitObjectEncoder = new();

    public bool CheckIsSelected { get; set; } = checkIsSelected;
    public bool CheckPosition { get; set; } = checkPosition;
    public bool CheckTime { get; set; } = checkTime;

    public bool Equals(HitObject? x, HitObject? y) {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;
        if (CheckIsSelected && x.IsSelected != y.IsSelected)
            return false;
        if (CheckPosition && x.Pos != y.Pos)
            return false;
        if (CheckTime && Math.Abs(x.StartTime - y.StartTime) > Precision.DoubleEpsilon)
            return false;
        if (!(x.Hitsounds.Equals(y.Hitsounds) &&
              x.NewCombo == y.NewCombo &&
              x.ComboSkip == y.ComboSkip))
            return false;
        return x switch {
            HitCircle _ when y is HitCircle => true,
            Slider sliderX when y is Slider sliderY => sliderX.SliderType == sliderY.SliderType &&
                                                       (!CheckPosition ||
                                                        sliderX.CurvePoints.SequenceEqual(sliderY.CurvePoints)) &&
                                                       sliderX.RepeatCount == sliderY.RepeatCount &&
                                                       Math.Abs(sliderX.PixelLength - sliderY.PixelLength) < Precision.DoubleEpsilon &&
                                                       sliderX.EdgeHitsounds.SequenceEqual(sliderY.EdgeHitsounds),
            Spinner spinnerX when y is Spinner spinnerY => Math.Abs(spinnerX.EndTime - spinnerY.EndTime) < Precision.DoubleEpsilon,
            HoldNote holdNoteX when y is HoldNote holdNoteY => Math.Abs(holdNoteX.EndTime - holdNoteY.EndTime) < Precision.DoubleEpsilon,
            _ => false,
        };

        // Types dont match
    }

    public int GetHashCode(HitObject obj) {
        return EqualityComparer<string>.Default.GetHashCode(_hitObjectEncoder.Encode(obj));
    }
}