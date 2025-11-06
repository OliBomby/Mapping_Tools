using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.HitObjects.Objects;
using Mapping_Tools.Domain.Beatmaps.Parsing.Decoding.HitObjects.Objects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.Decoding.HitObjects;

public class HitObjectDecoder : IDecoder<HitObject> {
    private readonly IDecoder<HitCircle> hitCircleDecoder;
    private readonly IDecoder<Slider> sliderDecoder;
    private readonly IDecoder<Spinner> spinnerDecoder;
    private readonly IDecoder<HoldNote> holdNoteDecoder;

    public HitObjectDecoder() : this(new HitCircleDecoder(), new SliderDecoder(), new SpinnerDecoder(), new HoldNoteDecoder()) { }

    public HitObjectDecoder(
        IDecoder<HitCircle> hitCircleDecoder,
        IDecoder<Slider> sliderDecoder,
        IDecoder<Spinner> spinnerDecoder,
        IDecoder<HoldNote> holdNoteDecoder) {
        this.hitCircleDecoder = hitCircleDecoder;
        this.sliderDecoder = sliderDecoder;
        this.spinnerDecoder = spinnerDecoder;
        this.holdNoteDecoder = holdNoteDecoder;
    }

    public HitObject Decode(string code) {
        var values = HitObjectDecodingHelper.SplitLine(code);
        var type = HitObjectDecodingHelper.GetHitObjectType(values);
        return type switch {
            HitObjectType.Circle => hitCircleDecoder.Decode(code),
            HitObjectType.Slider => sliderDecoder.Decode(code),
            HitObjectType.Spinner => spinnerDecoder.Decode(code),
            HitObjectType.HoldNote => holdNoteDecoder.Decode(code),
            _ => throw new BeatmapParsingException("Unrecognized hit object type.", code)
        };
    }
}