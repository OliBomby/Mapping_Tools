using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.Exceptions;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Decoding.HitObjects.Objects;

public class HoldNoteDecoder : IDecoder<HoldNote> {
    public void Decode(HoldNote obj, string code) {
        var values = HitObjectDecodingHelper.SplitLine(code);

        if (HitObjectDecodingHelper.GetHitObjectType(values) != HitObjectType.HoldNote)
            throw new BeatmapParsingException("This code is not a hold note.", code);

        HitObjectDecodingHelper.DecodeSharedProperties(obj, values);

        // Extras on 5
        if (values.Length > 5) {
            // This will also assign the end time
            HitObjectDecodingHelper.DecodeExtras(obj, values[5]);
        }
    }

    public HoldNote Decode(string code) {
        var holdNote = new HoldNote();
        Decode(holdNote, code);

        return holdNote;
    }
}