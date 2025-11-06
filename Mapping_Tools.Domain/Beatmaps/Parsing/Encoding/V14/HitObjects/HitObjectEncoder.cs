using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.HitObjects.Objects;
using Mapping_Tools.Domain.Beatmaps.Parsing.Encoding.HitObjects.Objects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.Encoding.HitObjects;

public class HitObjectEncoder : IEncoder<HitObject> {
    private readonly IEncoder<HitCircle> hitCircleEncoder;
    private readonly IEncoder<Slider> sliderEncoder;
    private readonly IEncoder<Spinner> spinnerEncoder;
    private readonly IEncoder<HoldNote> holdNoteEncoder;

    public HitObjectEncoder(bool encodeWithFloatPrecision = false) : this(
        new HitCircleEncoder(encodeWithFloatPrecision),
        new SliderEncoder(encodeWithFloatPrecision),
        new SpinnerEncoder(encodeWithFloatPrecision),
        new HoldNoteEncoder(encodeWithFloatPrecision)) { }

    public HitObjectEncoder(
        IEncoder<HitCircle> hitCircleEncoder,
        IEncoder<Slider> sliderEncoder,
        IEncoder<Spinner> spinnerEncoder,
        IEncoder<HoldNote> holdNoteEncoder) {
        this.hitCircleEncoder = hitCircleEncoder;
        this.sliderEncoder = sliderEncoder;
        this.spinnerEncoder = spinnerEncoder;
        this.holdNoteEncoder = holdNoteEncoder;
    }

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