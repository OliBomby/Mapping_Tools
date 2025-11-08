using System.Text;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class SpinnerEncoder(bool encodeWithFloatPrecision = false) : HitObjectEncoderBase(encodeWithFloatPrecision), IEncoder<Spinner> {
    public string Encode(Spinner obj) {
        var builder = new StringBuilder();

        EncodeSharedProperties(obj, builder);
        builder.Append(',');
        builder.Append(EncodeWithFloatPrecision ? obj.EndTime.ToInvariant() : obj.EndTime.ToRoundInvariant());
        builder.Append(',');
        EncodeExtras(obj, builder);

        return builder.ToString();
    }
}