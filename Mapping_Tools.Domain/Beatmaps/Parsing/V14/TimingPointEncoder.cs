using System.Collections;
using Mapping_Tools.Domain.Beatmaps.Timings;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class TimingPointEncoder(bool encodeWithFloatPrecision = false) : IEncoder<TimingPoint> {
    /// <summary>
    /// When true, all coordinates and times will be serialized without rounding.
    /// </summary>
    public readonly bool EncodeWithFloatPrecision = encodeWithFloatPrecision;

    public string Encode(TimingPoint obj) {
        int style = MathHelper.GetIntFromBitArray(new BitArray([obj.Kiai, false, false, obj.OmitFirstBarLine]));
        return $"{(EncodeWithFloatPrecision ? obj.Offset.ToInvariant() : obj.Offset.ToRoundInvariant())},{obj.MpB.ToInvariant()},{obj.Meter.TempoNumerator.ToInvariant()},{obj.SampleSet.ToIntInvariant()},{obj.SampleIndex.ToInvariant()},{(EncodeWithFloatPrecision ? obj.Volume.ToInvariant() : obj.Volume.ToRoundInvariant())},{Convert.ToInt32(obj.Uninherited).ToInvariant()},{style.ToInvariant()}";
    }
}