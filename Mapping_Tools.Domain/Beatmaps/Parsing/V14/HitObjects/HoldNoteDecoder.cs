using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

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