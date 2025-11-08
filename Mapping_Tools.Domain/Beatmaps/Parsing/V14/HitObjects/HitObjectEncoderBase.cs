using System.Collections;
using System.Text;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public abstract class HitObjectEncoderBase(bool encodeWithFloatPrecision) {
    /// <summary>
    /// When true, all coordinates and times will be serialized without rounding.
    /// </summary>
    public bool EncodeWithFloatPrecision { get; } = encodeWithFloatPrecision;

    protected void EncodeSharedProperties(HitObject ho, StringBuilder builder) {
        builder.Append(EncodeWithFloatPrecision ? ho.Pos.X.ToInvariant() : ho.Pos.X.ToRoundInvariant());
        builder.Append(',');
        builder.Append(EncodeWithFloatPrecision ? ho.Pos.Y.ToInvariant() : ho.Pos.Y.ToRoundInvariant());
        builder.Append(',');
        builder.Append(EncodeWithFloatPrecision ? ho.StartTime.ToInvariant() : ho.StartTime.ToRoundInvariant());
        builder.Append(',');
        var cs = new BitArray([ho.ComboSkip]);
        var objectType = MathHelper.GetIntFromBitArray(new BitArray([ho is HitCircle, ho is Slider, ho.NewCombo, ho is Spinner, cs[0], cs[1], cs[2], ho is HoldNote]));
        builder.Append(objectType.ToInvariant());
        builder.Append(',');
        var hs = GetHitsounds(ho.Hitsounds);
        builder.Append(hs.ToInvariant());
    }

    protected void EncodeExtras(HitObject ho, StringBuilder builder) {
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
        return MathHelper.GetIntFromBitArray(new BitArray([hitSampleInfo.Normal, hitSampleInfo.Whistle, hitSampleInfo.Finish, hitSampleInfo.Clap]));
    }
}