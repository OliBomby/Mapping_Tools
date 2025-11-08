using System.Text;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;

public class SliderEncoder(bool encodeWithFloatPrecision = false) : HitObjectEncoderBase(encodeWithFloatPrecision), IEncoder<Slider> {
    public string Encode(Slider obj) {
        var builder = new StringBuilder();

        EncodeSharedProperties(obj, builder);
        builder.Append(',');
        builder.Append(GetPathTypeString(obj.SliderType));
        foreach (var p in obj.CurvePoints) {
            builder.Append(
                $"|{(EncodeWithFloatPrecision ? p.X.ToInvariant() : p.X.ToRoundInvariant())}:{(EncodeWithFloatPrecision ? p.Y.ToInvariant() : p.Y.ToRoundInvariant())}");
        }
        builder.Append(',');
        builder.Append(obj.SpanCount.ToInvariant());
        builder.Append(',');
        builder.Append(obj.PixelLength.ToInvariant());

        if (obj.NeedSliderExtras()) {
            // Edge hitsounds, samplesets and extras
            builder.Append(',');
            builder.AppendJoin('|', obj.EdgeHitsounds.Select(p => GetHitsounds(p).ToInvariant()));
            builder.Append(',');
            builder.AppendJoin('|', obj.EdgeHitsounds.Select(p => $"{p.SampleSet.ToIntInvariant()}:{p.AdditionSet.ToIntInvariant()}"));
            builder.Append(',');
            EncodeExtras(obj, builder);
        }

        return builder.ToString();
    }

    private static string GetPathTypeString(PathType sliderType) {
        return sliderType switch {
            PathType.Linear => "L",
            PathType.PerfectCurve => "P",
            PathType.Catmull => "C",
            PathType.Bezier => "B",
            _ => throw new ArgumentOutOfRangeException(nameof(sliderType))
        };
    }
}