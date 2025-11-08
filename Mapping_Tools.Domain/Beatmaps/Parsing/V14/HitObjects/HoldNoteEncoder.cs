using System.Text;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class HoldNoteEncoder(bool encodeWithFloatPrecision = false) : HitObjectEncoderBase(encodeWithFloatPrecision), IEncoder<HoldNote> {
    public string Encode(HoldNote obj) {
        var builder = new StringBuilder();

        EncodeSharedProperties(obj, builder);
        builder.Append(',');
        // This encodes the end time aswell
        EncodeExtras(obj, builder);

        return builder.ToString();
    }
}