using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;

namespace Mapping_Tools_Core.BeatmapHelper.Encoding {
    public class TimingPointEncoder : IEncoder<TimingPoint> {
        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public readonly bool EncodeWithFloatPrecision;

        public TimingPointEncoder(bool encodeWithFloatPrecision = false) {
            EncodeWithFloatPrecision = encodeWithFloatPrecision;
        }

        public string Encode(TimingPoint obj) {
            int style = MathHelper.GetIntFromBitArray(new BitArray(new[] { obj.Kiai, false, false, obj.OmitFirstBarLine }));
            return $"{(EncodeWithFloatPrecision ? obj.Offset.ToInvariant() : obj.Offset.ToRoundInvariant())},{obj.MpB.ToInvariant()},{obj.Meter.TempoNumerator.ToInvariant()},{obj.SampleSet.ToIntInvariant()},{obj.SampleIndex.ToInvariant()},{(EncodeWithFloatPrecision ? obj.Volume.ToInvariant() : obj.Volume.ToRoundInvariant())},{Convert.ToInt32(obj.Uninherited).ToInvariant()},{style.ToInvariant()}";
        }
    }
}