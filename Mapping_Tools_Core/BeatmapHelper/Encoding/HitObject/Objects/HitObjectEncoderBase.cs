using Mapping_Tools_Core.BeatmapHelper.Objects;
using Mapping_Tools_Core.MathUtil;
using System.Collections;
using System.Text;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding.HitObject.Objects {
    public abstract class HitObjectEncoderBase {
        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public bool EncodeWithFloatPrecision { get; }

        protected HitObjectEncoderBase(bool encodeWithFloatPrecision) {
            EncodeWithFloatPrecision = encodeWithFloatPrecision;
        }

        protected void EncodeSharedProperties(BeatmapHelper.HitObject ho, StringBuilder builder) {
            builder.Append(EncodeWithFloatPrecision ? ho.Pos.X.ToInvariant() : ho.Pos.X.ToRoundInvariant());
            builder.Append(',');
            builder.Append(EncodeWithFloatPrecision ? ho.Pos.Y.ToInvariant() : ho.Pos.Y.ToRoundInvariant());
            builder.Append(',');
            builder.Append(EncodeWithFloatPrecision ? ho.StartTime.ToInvariant() : ho.StartTime.ToRoundInvariant());
            builder.Append(',');
            var cs = new BitArray(new[] { ho.ComboSkip });
            var objectType = MathHelper.GetIntFromBitArray(new BitArray(new[]
                {ho is HitCircle, ho is Slider, ho.NewCombo, ho is Spinner, cs[0], cs[1], cs[2], ho is HoldNote}));
            builder.Append(objectType.ToInvariant());
            builder.Append(',');
            var hs = GetHitsounds(ho.Hitsounds);
            builder.Append(hs.ToInvariant());
        }

        protected void EncodeExtras(BeatmapHelper.HitObject ho, StringBuilder builder) {
            if (ho is HoldNote holdNote) {
                builder.Append(EncodeWithFloatPrecision ? holdNote.EndTime.ToInvariant() : holdNote.EndTime.ToRoundInvariant());
                builder.Append(':');
            }
            builder.AppendJoin(':',
                ho.Hitsounds.SampleSet.ToIntInvariant(),
                ho.Hitsounds.AdditionSet.ToIntInvariant(),
                ho.Hitsounds.CustomIndex.ToInvariant(),
                EncodeWithFloatPrecision ? ho.Hitsounds.Volume.ToInvariant() : ho.Hitsounds.Volume.ToRoundInvariant(),
                ho.Hitsounds.Filename);
        }

        protected static int GetHitsounds(HitSampleInfo hitSampleInfo) {
            return MathHelper.GetIntFromBitArray(new BitArray(new[] { hitSampleInfo.Normal, hitSampleInfo.Whistle, hitSampleInfo.Finish, hitSampleInfo.Clap }));
        }
    }
}