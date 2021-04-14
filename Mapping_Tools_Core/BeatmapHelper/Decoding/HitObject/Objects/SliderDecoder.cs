using System;
using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Objects;
using Mapping_Tools_Core.Exceptions;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper.Decoding.HitObject.Objects {
    public class SliderDecoder : IDecoder<Slider> {
        public void Decode(Slider obj, string code) {
            var values = HitObjectDecodingHelper.SplitLine(code);

            if (HitObjectDecodingHelper.GetHitObjectType(values) != HitObjectType.Slider)
                throw new BeatmapParsingException("This code is not a slider.", code);

            HitObjectDecodingHelper.DecodeSharedProperties(obj, values);

            if (values.Length <= 7)
                throw new BeatmapParsingException("Slider object is missing values.", code);

            var sliderData = values[5].Split('|');

            obj.SliderType = HitObjectDecodingHelper.GetPathType(sliderData);

            var points = new List<Vector2>();
            for (var i = 1; i < sliderData.Length; i++) {
                var spl = sliderData[i].Split(':');
                if (spl.Length == 2) // It has to have 2 coordinates inside
                {
                    if (InputParsers.TryParseDouble(spl[0], out var ax) && InputParsers.TryParseDouble(spl[1], out var ay))
                        points.Add(new Vector2(ax, ay));
                    else throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", code);
                }
            }

            obj.CurvePoints = points;

            if (InputParsers.TryParseInt(values[6], out var repeat))
                obj.RepeatCount = Math.Max(repeat - 1, 0);
            else throw new BeatmapParsingException("Failed to parse repeat number of slider.", code);

            if (InputParsers.TryParseDouble(values[7], out var pixelLength))
                obj.PixelLength = pixelLength;
            else throw new BeatmapParsingException("Failed to parse pixel length of slider.", code);

            obj.EdgeHitsounds = new List<HitSampleInfo>(obj.SpanCount + 1);
            for (int i = 0; i < obj.SpanCount + 1; i++) {
                obj.EdgeHitsounds.Add(obj.Hitsounds.Clone());
            }

            // Edge hitsounds on 8
            if (values.Length > 8) {
                var split = values[8].Split('|');
                for (var i = 0; i < Math.Min(split.Length, obj.SpanCount + 1); i++) {
                    if (InputParsers.TryParseInt(split[i], out var ehs)) {
                        HitObjectDecodingHelper.DecodeHitsounds(obj.EdgeHitsounds[i], ehs);
                    }
                }
            }

            // Edge samplesets on 9
            if (values.Length > 9) {
                var split = values[9].Split('|');
                for (var i = 0; i < Math.Min(split.Length, obj.SpanCount + 1); i++) {
                    var sssplit = split[i].Split(':');
                    if (InputParsers.TryParseInt(sssplit[0], out var ess)) {
                        obj.EdgeHitsounds[i].SampleSet = (SampleSet) ess;
                    }
                    if (InputParsers.TryParseInt(sssplit[1], out var eas)) {
                        obj.EdgeHitsounds[i].AdditionSet = (SampleSet) eas;
                    }
                }
            }

            // Extras on 10
            if (values.Length > 10)
                HitObjectDecodingHelper.DecodeExtras(obj, values[10]);
        }

        public Slider DecodeNew(string code) {
            var slider = new Slider();
            Decode(slider, code);

            return slider;
        }
    }
}