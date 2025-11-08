using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class HitObjectDecoder(
    IDecoder<HitCircle> hitCircleDecoder,
    IDecoder<Slider> sliderDecoder,
    IDecoder<Spinner> spinnerDecoder,
    IDecoder<HoldNote> holdNoteDecoder)
    : IDecoder<HitObject> {
    public HitObjectDecoder() : this(new HitCircleDecoder(), new SliderDecoder(), new SpinnerDecoder(), new HoldNoteDecoder()) { }

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