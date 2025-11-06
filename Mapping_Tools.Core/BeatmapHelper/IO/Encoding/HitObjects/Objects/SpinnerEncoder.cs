using System.Text;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Encoding.HitObjects.Objects;

public class SpinnerEncoder : HitObjectEncoderBase, IEncoder<Spinner> {

    public SpinnerEncoder(bool encodeWithFloatPrecision = false) : base(encodeWithFloatPrecision) { }

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