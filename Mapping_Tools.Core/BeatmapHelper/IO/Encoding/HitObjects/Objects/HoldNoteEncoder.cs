using System.Text;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Encoding.HitObjects.Objects;

public class HoldNoteEncoder : HitObjectEncoderBase, IEncoder<HoldNote> {

    public HoldNoteEncoder(bool encodeWithFloatPrecision = false) : base(encodeWithFloatPrecision) { }

    public string Encode(HoldNote obj) {
        var builder = new StringBuilder();

        EncodeSharedProperties(obj, builder);
        builder.Append(',');
        // This encodes the end time aswell
        EncodeExtras(obj, builder);

        return builder.ToString();
    }
}