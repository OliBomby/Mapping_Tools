using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class HitCircleDecoder : IDecoder<HitCircle> {
    public HitCircle Decode(string code) {
        var hitCircle = new HitCircle();
        var values = HitObjectDecodingHelper.SplitLine(code);

        if (HitObjectDecodingHelper.GetHitObjectType(values) != HitObjectType.Circle)
            throw new BeatmapParsingException("This code is not a hit circle.", code);

        HitObjectDecodingHelper.DecodeSharedProperties(hitCircle, values);

        // Extras on 5
        if (values.Length > 5)
            HitObjectDecodingHelper.DecodeExtras(hitCircle, values[5]);

        return hitCircle;
    }
}