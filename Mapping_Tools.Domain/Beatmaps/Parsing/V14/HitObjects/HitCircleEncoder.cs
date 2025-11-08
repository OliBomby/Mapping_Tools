using System.Text;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class HitCircleEncoder(bool encodeWithFloatPrecision = false) : HitObjectEncoderBase(encodeWithFloatPrecision), IEncoder<HitCircle> {
    public string Encode(HitCircle obj) {
        var builder = new StringBuilder();

        EncodeSharedProperties(obj, builder);
        builder.Append(',');
        EncodeExtras(obj, builder);

        return builder.ToString();
    }
}