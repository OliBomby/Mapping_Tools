using System.Text;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Encoding.HitObjects.Objects;

public class HitCircleEncoder : HitObjectEncoderBase, IEncoder<HitCircle> {

    public HitCircleEncoder(bool encodeWithFloatPrecision = false) : base(encodeWithFloatPrecision) { }

    public string Encode(HitCircle obj) {
        var builder = new StringBuilder();

        EncodeSharedProperties(obj, builder);
        builder.Append(',');
        EncodeExtras(obj, builder);

        return builder.ToString();
    }
}