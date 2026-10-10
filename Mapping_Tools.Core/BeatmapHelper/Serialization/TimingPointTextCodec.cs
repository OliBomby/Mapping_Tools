using System.Collections;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

internal static class TimingPointTextCodec
{
    public static TimingPoint Decode(string line)
    {
        string[] values = line.Split(',');
        TimingPoint point = new();

        // Offset, milliseconds per beat, meter, sample set, sample index, volume, inherited, style.
        point.Offset = ParseDouble(values[0], "Failed to parse offset of timing point", line);
        point.MpB = ParseDouble(values[1], "Failed to parse milliseconds per beat of timing point", line);

        if (TryParseInt(values[2], out int meter))
            point.Meter = new TempoSignature(meter);
        else
            throw new BeatmapParsingException("Failed to parse meter of timing point", line);

        if (Enum.TryParse(values[3], out SampleSet sampleSet))
            point.SampleSet = sampleSet;
        else
            throw new BeatmapParsingException("Failed to parse sampleset of timing point", line);

        if (TryParseInt(values[4], out int sampleIndex))
            point.SampleIndex = sampleIndex;
        else
            throw new BeatmapParsingException("Failed to parse sample index of timing point", line);

        point.Volume = ParseDouble(values[5], "Failed to parse volume of timing point", line);
        point.Uninherited = values[6] == "1";

        // The optional style field is a bit field; bits zero and three select kiai and bar-line omission.
        if (values.Length > 7)
        {
            if (TryParseInt(values[7], out int style))
            {
                var flags = new BitArray([style]);
                point.Kiai = flags[0];
                point.OmitFirstBarLine = flags[3];
            }
            else
                throw new BeatmapParsingException("Failed to style of timing point", line);
        }

        return point;
    }

    public static string Encode(TimingPoint point, int formatVersion)
    {
        int style = MathHelper.GetIntFromBitArray(new BitArray([point.Kiai, false, false, point.OmitFirstBarLine]));

        // Version 128 keeps volume precision; older files use the legacy rounded representation.
        string volume = formatVersion >= 128 ? point.Volume.ToInvariant() : point.Volume.ToRoundInvariant();
        return string.Join(
            ",",
            point.Offset.ToInvariant(),
            point.MpB.ToInvariant(),
            point.Meter.TempoNumerator.ToInvariant(),
            point.SampleSet.ToIntInvariant(),
            point.SampleIndex.ToInvariant(),
            volume,
            Convert.ToInt32(point.Uninherited).ToInvariant(),
            style.ToInvariant());
    }

    private static double ParseDouble(string value, string error, string line)
    {
        if (TryParseDouble(value, out double result))
            return result;

        throw new BeatmapParsingException(error, line);
    }
}
