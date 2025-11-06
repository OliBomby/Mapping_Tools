using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.Exceptions;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Decoding.HitObjects.Objects;

public class SpinnerDecoder : IDecoder<Spinner> {
    public void Decode(Spinner obj, string code) {
        var values = HitObjectDecodingHelper.SplitLine(code);

        if (HitObjectDecodingHelper.GetHitObjectType(values) != HitObjectType.Spinner)
            throw new BeatmapParsingException("This code is not a spinner.", code);

        HitObjectDecodingHelper.DecodeSharedProperties(obj, values);

        if (values.Length <= 5)
            throw new BeatmapParsingException("Spinner object is missing values.", code);

        if (FileFormatHelper.TryParseDouble(values[5], out var et))
            obj.SetEndTime(et);
        else throw new BeatmapParsingException("Failed to parse end time of spinner.", code);

        // Extras on 6
        if (values.Length > 6)
            HitObjectDecodingHelper.DecodeExtras(obj, values[6]);
    }

    public Spinner Decode(string code) {
        var spinner = new Spinner();
        Decode(spinner, code);

        return spinner;
    }
}