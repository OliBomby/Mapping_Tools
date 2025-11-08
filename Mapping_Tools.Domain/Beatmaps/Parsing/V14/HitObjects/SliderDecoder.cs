using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class SliderDecoder : IDecoder<Slider> {
    public Slider Decode(string code) {
        var slider = new Slider();
        var values = HitObjectDecodingHelper.SplitLine(code);

        if (HitObjectDecodingHelper.GetHitObjectType(values) != HitObjectType.Slider)
            throw new BeatmapParsingException("This code is not a slider.", code);

        HitObjectDecodingHelper.DecodeSharedProperties(slider, values);

        if (values.Length <= 7)
            throw new BeatmapParsingException("Slider object is missing values.", code);

        var sliderData = values[5].Split('|');

        slider.SliderType = HitObjectDecodingHelper.GetPathType(sliderData);

        var points = new List<Vector2>();
        for (var i = 1; i < sliderData.Length; i++) {
            var spl = sliderData[i].Split(':');
            if (spl.Length == 2) // It has to have 2 coordinates inside
            {
                if (FileFormatHelper.TryParseDouble(spl[0], out var ax) && FileFormatHelper.TryParseDouble(spl[1], out var ay))
                    points.Add(new Vector2(ax, ay));
                else throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", code);
            }
        }

        slider.CurvePoints = points;

        if (FileFormatHelper.TryParseInt(values[6], out var repeat))
            slider.RepeatCount = Math.Max(repeat - 1, 0);
        else throw new BeatmapParsingException("Failed to parse repeat number of slider.", code);

        if (FileFormatHelper.TryParseDouble(values[7], out var pixelLength))
            slider.PixelLength = pixelLength;
        else throw new BeatmapParsingException("Failed to parse pixel length of slider.", code);

        slider.EdgeHitsounds = new List<HitSampleInfo>(slider.SpanCount + 1);
        for (int i = 0; i < slider.SpanCount + 1; i++) {
            slider.EdgeHitsounds.Add(slider.Hitsounds.Clone());
        }

        // Edge hitsounds on 8
        if (values.Length > 8) {
            var split = values[8].Split('|');
            for (var i = 0; i < Math.Min(split.Length, slider.SpanCount + 1); i++) {
                if (FileFormatHelper.TryParseInt(split[i], out var ehs)) {
                    HitObjectDecodingHelper.DecodeHitsounds(slider.EdgeHitsounds[i], ehs);
                }
            }
        }

        // Edge samplesets on 9
        if (values.Length > 9) {
            var split = values[9].Split('|');
            for (var i = 0; i < Math.Min(split.Length, slider.SpanCount + 1); i++) {
                var sssplit = split[i].Split(':');
                if (FileFormatHelper.TryParseInt(sssplit[0], out var ess)) {
                    slider.EdgeHitsounds[i].SampleSet = (SampleSet) ess;
                }
                if (FileFormatHelper.TryParseInt(sssplit[1], out var eas)) {
                    slider.EdgeHitsounds[i].AdditionSet = (SampleSet) eas;
                }
            }
        }

        // Extras on 10
        if (values.Length > 10)
            HitObjectDecodingHelper.DecodeExtras(slider, values[10]);

        return slider;
    }
}