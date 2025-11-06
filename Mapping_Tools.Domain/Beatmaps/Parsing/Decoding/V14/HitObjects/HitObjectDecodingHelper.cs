using System.Collections;
using Mapping_Tools.Domain.Beatmaps.Enums;
using Mapping_Tools.Domain.Beatmaps.HitObjects;
using Mapping_Tools.Domain.Beatmaps.HitObjects.Objects;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.Decoding.HitObjects;

public static class HitObjectDecodingHelper {
    public static void DecodeSharedProperties(HitObject hitObject, string[] values) {
        if (values.Length < 5)
            throw new BeatmapParsingException("Hit object is missing values.", JoinLine(values));

        if (FileFormatHelper.TryParseDouble(values[0], out var x) && FileFormatHelper.TryParseDouble(values[1], out var y))
            hitObject.Pos = new Vector2(x, y);
        else throw new BeatmapParsingException("Failed to parse coordinate of hit object.", JoinLine(values));

        if (FileFormatHelper.TryParseDouble(values[2], out var t))
            hitObject.StartTime = t;
        else throw new BeatmapParsingException("Failed to parse time of hit object.", JoinLine(values));

        if (FileFormatHelper.TryParseInt(values[3], out var type)) {
            var b = new BitArray(new[] { type });
            hitObject.NewCombo = b[2];
            hitObject.ComboSkip = MathHelper.GetIntFromBitArray(new BitArray(new[] { b[4], b[5], b[6] }));
        } else throw new BeatmapParsingException("Failed to parse type of hit object.", string.Join(',', values));

        if (FileFormatHelper.TryParseInt(values[4], out var hitsounds))
            DecodeHitsounds(hitObject.Hitsounds, hitsounds);
        else throw new BeatmapParsingException("Failed to parse hitsound of hit object.", JoinLine(values));
    }

    public static void DecodeHitsounds(HitSampleInfo hitSampleInfo, int hitsounds) {
        var b = new BitArray(new[] { hitsounds });
        hitSampleInfo.Normal = b[0];
        hitSampleInfo.Whistle = b[1];
        hitSampleInfo.Finish = b[2];
        hitSampleInfo.Clap = b[3];
    }

    public static void DecodeExtras(HitObject hitObject, string extras) {
        if (string.IsNullOrWhiteSpace(extras))
            return;

        // Extras has an extra value at the start if it's a hold note
        var split = extras.Split(':');
        var i = 0;
        if (hitObject is HoldNote holdNote) {
            if (FileFormatHelper.TryParseDouble(split[i++], out var et))
                holdNote.SetEndTime(et);
            else throw new BeatmapParsingException("Failed to parse end time of hold note.", extras);
        }

        if (i >= split.Length)
            return;

        if (FileFormatHelper.TryParseInt(split[i++], out var ss))
            hitObject.Hitsounds.SampleSet = (SampleSet)ss;
        else throw new BeatmapParsingException("Failed to parse sample set of hit object.", extras);

        if (FileFormatHelper.TryParseInt(split[i++], out var ass))
            hitObject.Hitsounds.AdditionSet = (SampleSet)ass;
        else throw new BeatmapParsingException("Failed to parse additional sample set of hit object.", extras);

        if (i >= split.Length)
            return;

        if (FileFormatHelper.TryParseInt(split[i++], out var ci))
            hitObject.Hitsounds.CustomIndex = ci;
        else throw new BeatmapParsingException("Failed to parse custom index of hit object.", extras);

        if (i >= split.Length)
            return;

        if (FileFormatHelper.TryParseDouble(split[i++], out var vol))
            hitObject.Hitsounds.Volume = vol;
        else throw new BeatmapParsingException("Failed to parse volume of hit object.", extras);

        hitObject.Hitsounds.Filename = split[i];
    }

    public static HitObjectType GetHitObjectType(string[] values) {
        if (!FileFormatHelper.TryParseInt(values[3], out var type))
            throw new BeatmapParsingException("Failed to parse type of hit object.", JoinLine(values));

        var b = new BitArray(new[] { type });
        if (b[0]) {
            return HitObjectType.Circle;
        }
        if (b[1]) {
            return HitObjectType.Slider;
        }
        if (b[3]) {
            return HitObjectType.Spinner;
        }
        if (b[7]) {
            return HitObjectType.HoldNote;
        }

        return HitObjectType.Circle;
    }

    public static PathType GetPathType(string[] sliderData) {
        for (var i = sliderData.Length - 1; i >= 0; i--) {
            // Iterating in reverse to get the last valid letter
            var letter =
                sliderData[i].Any() ? sliderData[i][0] : '0'; // 0 is not a letter so it will get ignored
            if (char.IsLetter(letter))
                switch (letter) {
                    case 'L':
                        return PathType.Linear;
                    case 'B':
                        return PathType.Bezier;
                    case 'P':
                        return PathType.PerfectCurve;
                    case 'C':
                        return PathType.Catmull;
                }
        }

        // If there is no valid letter it will literally default to catmull
        return PathType.Catmull;
    }

    public static string[] SplitLine(string line) {
        return line.Split(',');
    }

    public static string JoinLine(string[] values) {
        return string.Join(',', values);
    }
}