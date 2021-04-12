namespace Mapping_Tools_Core.BeatmapHelper.Encoding.HitObject {
    public class HitObjectEncoder : IEncoder<BeatmapHelper.HitObject> {
        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public readonly bool EncodeWithFloatPrecision;

        public HitObjectEncoder(bool encodeWithFloatPrecision = false) {
            EncodeWithFloatPrecision = encodeWithFloatPrecision;
        }

        public string Encode(BeatmapHelper.HitObject obj) {
            throw new System.NotImplementedException();
        }
    }
}