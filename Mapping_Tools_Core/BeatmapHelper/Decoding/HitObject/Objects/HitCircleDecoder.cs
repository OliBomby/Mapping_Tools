using Mapping_Tools_Core.BeatmapHelper.Objects;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding.HitObject.Objects {
    public class HitCircleDecoder : IDecoder<HitCircle> {
        public void Decode(HitCircle obj, string code) {
            var values = HitObjectDecodingHelper.SplitLine(code);

            HitObjectDecodingHelper.DecodeSharedProperties(obj, values);

            // Extras on 5
            if (values.Length > 5)
                HitObjectDecodingHelper.DecodeExtras(obj, values[5]);
        }

        public HitCircle DecodeNew(string code) {
            var hitCircle = new HitCircle();
            Decode(hitCircle, code);

            return hitCircle;
        }
    }
}