using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.Exceptions;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Decoding.HitObjects.Objects;

public class HitCircleDecoder : IDecoder<HitCircle> {
    public void Decode(HitCircle obj, string code) {
        var values = HitObjectDecodingHelper.SplitLine(code);

        if (HitObjectDecodingHelper.GetHitObjectType(values) != HitObjectType.Circle)
            throw new BeatmapParsingException("This code is not a hit circle.", code);

        HitObjectDecodingHelper.DecodeSharedProperties(obj, values);

        // Extras on 5
        if (values.Length > 5)
            HitObjectDecodingHelper.DecodeExtras(obj, values[5]);
    }

    public HitCircle Decode(string code) {
        var hitCircle = new HitCircle();
        Decode(hitCircle, code);

        return hitCircle;
    }
}