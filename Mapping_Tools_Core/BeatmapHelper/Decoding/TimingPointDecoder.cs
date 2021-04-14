using System;
using System.Collections;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding {
    public class TimingPointDecoder : IDecoder<TimingPoint> {
        public void Decode(TimingPoint obj, string code) {
            string[] values = code.Split(',');

            if (InputParsers.TryParseDouble(values[0], out double offset))
                obj.Offset = offset;
            else throw new BeatmapParsingException("Failed to parse offset of timing point", code);

            if (InputParsers.TryParseDouble(values[1], out double mpb))
                obj.MpB = mpb;
            else throw new BeatmapParsingException("Failed to parse milliseconds per beat of timing point", code);

            if (InputParsers.TryParseInt(values[2], out int meter))
                obj.Meter = new TempoSignature(meter);
            else throw new BeatmapParsingException("Failed to parse meter of timing point", code);

            if (Enum.TryParse(values[3], out SampleSet ss))
                obj.SampleSet = ss;
            else throw new BeatmapParsingException("Failed to parse sampleset of timing point", code);

            if (InputParsers.TryParseInt(values[4], out int ind))
                obj.SampleIndex = ind;
            else throw new BeatmapParsingException("Failed to parse sample index of timing point", code);

            if (InputParsers.TryParseDouble(values[5], out double vol))
                obj.Volume = vol;
            else throw new BeatmapParsingException("Failed to parse volume of timing point", code);

            obj.Uninherited = values[6] == "1";

            if (values.Length <= 7) return;
            if (InputParsers.TryParseInt(values[7], out int style)) {
                BitArray b = new BitArray(new int[] { style });
                obj.Kiai = b[0];
                obj.OmitFirstBarLine = b[3];
            } else throw new BeatmapParsingException("Failed to style of timing point", code);
        }

        public TimingPoint DecodeNew(string code) {
            var tp = new TimingPoint();
            Decode(tp, code);

            return tp;
        }
    }
}