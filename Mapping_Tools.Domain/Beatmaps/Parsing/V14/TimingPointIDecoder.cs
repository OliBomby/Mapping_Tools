using System.Collections;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.Timings;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

/// <summary>
/// Decoder for timing points.
/// </summary>
public class TimingPointIDecoder : IDecoderWithDefaults<TimingPoint> {
    public TimingPoint Decode(string code, IDictionary<string, object> defaultValues) {
        var tp = new TimingPoint();
        string[] values = code.Split(',');

        // Default sample volume gets ignored if the split length is exactly 2
        // That is just how osu! does things
        bool ignoreDefaultVolume = values.Length == 2;

        if (FileFormatHelper.TryParseDouble(values[0].Trim(), out double offset))
            tp.Offset = offset;
        else throw new BeatmapParsingException("Failed to parse offset of timing point", code);

        if (FileFormatHelper.TryParseDouble(values[1].Trim(), out double mpb))
            tp.MpB = mpb;
        else throw new BeatmapParsingException("Failed to parse milliseconds per beat of timing point", code);

        if (values.Length > 2) {
            if (values[2][0] == '0') {
                tp.Meter = new TempoSignature(4);
            } else {
                if (FileFormatHelper.TryParseInt(values[2], out int meter))
                    tp.Meter = new TempoSignature(meter);
                else throw new BeatmapParsingException("Failed to parse meter of timing point", code);
            }

            // osu! always expects a sampleset if a meter is given
            if (Enum.TryParse(values[3], out SampleSet ss))
                tp.SampleSet = ss;
            else throw new BeatmapParsingException("Failed to parse sampleset of timing point", code);
        } else {
            tp.Meter = new TempoSignature(4);
            tp.SampleSet = defaultValues.TryGetValue("SampleSet", out object? value) ? (SampleSet)value : SampleSet.Normal;
        }

        if (values.Length > 4) {
            if (FileFormatHelper.TryParseInt(values[4], out int ind))
                tp.SampleIndex = ind;
            else throw new BeatmapParsingException("Failed to parse sample index of timing point", code);
        } else {
            tp.SampleIndex = 0;
        }

        if (values.Length > 5) {
            if (FileFormatHelper.TryParseDouble(values[5], out double vol))
                tp.Volume = vol;
            else throw new BeatmapParsingException("Failed to parse volume of timing point", code);
        } else {
            tp.Volume = !ignoreDefaultVolume && defaultValues.TryGetValue("SampleVolume", out object? value) ? (double)value : 100;
        }

        if (values.Length > 6) {
            tp.Uninherited = values[6][0] == '1';
        } else {
            tp.Uninherited = true;
        }

        if (values.Length > 7) {
            if (FileFormatHelper.TryParseInt(values[7], out int style)) {
                BitArray b = new BitArray([style]);
                tp.Kiai = b[0];
                tp.OmitFirstBarLine = b[3];
            }
            else throw new BeatmapParsingException("Failed to style of timing point", code);
        } else {
            tp.Kiai = false;
            tp.OmitFirstBarLine = false;
        }

        return tp;
    }
}