using System;
using System.Collections;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.TimingStuff;
using Mapping_Tools.Core.BeatmapHelper.Types;
using Mapping_Tools.Core.Exceptions;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Decoding;

/// <summary>
/// Decoder for timing points.
/// </summary>
public class TimingPointDecoder : IDecoder<TimingPoint>, IConfigurableTimingPointDecoder {
    /// <inheritdoc/>
    public double DefaultVolume { get; set; }

    /// <inheritdoc/>
    public SampleSet DefaultSampleSet { get; set; }

    /// <summary>
    /// Creates a new timing point decoder with default fallback values.
    /// </summary>
    /// <param name="defaultVolume">Fallback value for timing point volume.</param>
    /// <param name="defaultSampleSet">Fallback value for timing point sample set.</param>
    public TimingPointDecoder(double defaultVolume = 100, SampleSet defaultSampleSet = SampleSet.Normal) {
        DefaultVolume = defaultVolume;
        DefaultSampleSet = defaultSampleSet;
    }

    /// <summary>
    /// Decodes the timing point string and writes it to the given object.
    /// </summary>
    /// <param name="obj">The timing point to write the data to.</param>
    /// <param name="code">The encoded timing point string.</param>
    public void Decode(TimingPoint obj, string code) {
        string[] values = code.Split(',');

        // Default sample volume gets ignored if the split length is exactly 2
        // That is just how osu! does things
        bool ignoreDefaultVolume = values.Length == 2;

        if (FileFormatHelper.TryParseDouble(values[0].Trim(), out double offset))
            obj.Offset = offset;
        else throw new BeatmapParsingException("Failed to parse offset of timing point", code);

        if (FileFormatHelper.TryParseDouble(values[1].Trim(), out double mpb))
            obj.MpB = mpb;
        else throw new BeatmapParsingException("Failed to parse milliseconds per beat of timing point", code);

        if (values.Length > 2) {
            if (values[2][0] == '0') {
                obj.Meter = new TempoSignature(4);
            } else {
                if (FileFormatHelper.TryParseInt(values[2], out int meter))
                    obj.Meter = new TempoSignature(meter);
                else throw new BeatmapParsingException("Failed to parse meter of timing point", code);
            }

            if (Enum.TryParse(values[3], out SampleSet ss))
                obj.SampleSet = ss;
            else throw new BeatmapParsingException("Failed to parse sampleset of timing point", code);
        } else {
            obj.Meter = new TempoSignature(4);
            obj.SampleSet = DefaultSampleSet;
        }

        if (values.Length > 4) {
            if (FileFormatHelper.TryParseInt(values[4], out int ind))
                obj.SampleIndex = ind;
            else throw new BeatmapParsingException("Failed to parse sample index of timing point", code);
        } else {
            obj.SampleIndex = 0;
        }

        if (values.Length > 5) {
            if (FileFormatHelper.TryParseDouble(values[5], out double vol))
                obj.Volume = vol;
            else throw new BeatmapParsingException("Failed to parse volume of timing point", code);
        } else {
            obj.Volume = ignoreDefaultVolume ? 100 : DefaultVolume;
        }

        if (values.Length > 6) {
            obj.Uninherited = values[6][0] == '1';
        } else {
            obj.Uninherited = true;
        }

        if (values.Length > 7) {
            if (FileFormatHelper.TryParseInt(values[7], out int style)) {
                BitArray b = new BitArray(new[] {style});
                obj.Kiai = b[0];
                obj.OmitFirstBarLine = b[3];
            }
            else throw new BeatmapParsingException("Failed to style of timing point", code);
        } else {
            obj.Kiai = false;
            obj.OmitFirstBarLine = false;
        }
    }

    /// <summary>
    /// Creates a new timing point from the encoded string.
    /// </summary>
    /// <param name="code">The encoded timing point to decode.</param>
    /// <returns>The new timing point with the data from the code.</returns>
    public TimingPoint Decode(string code) {
        var tp = new TimingPoint();
        Decode(tp, code);

        return tp;
    }
}