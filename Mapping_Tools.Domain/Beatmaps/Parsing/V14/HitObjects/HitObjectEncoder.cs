using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class HitObjectEncoder(
    IEncoder<HitCircle> hitCircleEncoder,
    IEncoder<Slider> sliderEncoder,
    IEncoder<Spinner> spinnerEncoder,
    IEncoder<HoldNote> holdNoteEncoder)
    : IEncoder<HitObject> {
    public HitObjectEncoder(bool encodeWithFloatPrecision = false) : this(
        new HitCircleEncoder(encodeWithFloatPrecision),
        new SliderEncoder(encodeWithFloatPrecision),
        new SpinnerEncoder(encodeWithFloatPrecision),
        new HoldNoteEncoder(encodeWithFloatPrecision)) { }

    public string Encode(HitObject obj) {
        return obj switch {
            HitCircle hitCircle => hitCircleEncoder.Encode(hitCircle),
            Slider slider => sliderEncoder.Encode(slider),
            Spinner spinner => spinnerEncoder.Encode(spinner),
            HoldNote holdNote => holdNoteEncoder.Encode(holdNote),
            _ => throw new ArgumentOutOfRangeException(nameof(obj))
        };
    }
}