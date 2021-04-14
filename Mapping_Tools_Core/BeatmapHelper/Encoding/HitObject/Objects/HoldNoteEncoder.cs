using System.Text;
using Mapping_Tools_Core.BeatmapHelper.Objects;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding.HitObject.Objects {
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
}