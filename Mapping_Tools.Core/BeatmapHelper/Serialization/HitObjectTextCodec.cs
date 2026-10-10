using System.Text;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

internal static class HitObjectTextCodec
{
    public static HitObject Decode(string line, int formatVersion)
    {
        // Examples:
        // 74,183,57308,2,0,B|70:236,1,53.9999983520508,4|0,0:3|0:0,0:0:0:0:
        // 295,347,57458,5,2,0:0:0:0:
        // Mania:
        // 128,192,78,1,0,0:0:0:0:
        // 213,192,78,128,0,378:0:0:0:0:
        string[] values = line.Split(',');

        if (values.Length <= 4)
            throw new BeatmapParsingException("Hit object is missing values.", line);

        HitObject hitObject = new();

        if (TryParseDouble(values[0], out double x) && TryParseDouble(values[1], out double y))
            hitObject.Pos = new Vector2(x, y);
        else
            throw new BeatmapParsingException("Failed to parse coordinate of hit object.", line);

        // Let the end position be the same as the start position before changed later for sliders.
        hitObject.EndPos = hitObject.Pos;

        hitObject.Time = ParseDouble(values[2], "Failed to parse time of hit object.", line);

        if (TryParseInt(values[3], out int type))
            hitObject.ObjectType = type;
        else
            throw new BeatmapParsingException("Failed to parse type of hit object.", line);

        if (TryParseInt(values[4], out int hitsounds))
            hitObject.Hitsounds = hitsounds;
        else
            throw new BeatmapParsingException("Failed to parse hitsound of hit object.", line);

        // Sliders remove extras and edge data when they do not use hitsounds.
        if (hitObject.IsSlider)
        {
            if (values.Length <= 7)
                throw new BeatmapParsingException("Slider object is missing values.", line);

            string[] sliderData = values[5].Split('|');
            var points = new List<PathControlPoint>
            {
                new(Vector2.Zero, TryGetPathType(sliderData[0], out PathType firstType) ? firstType : PathType.Catmull),
            };
            PathType? pendingType = null;

            for (int tokenIndex = 0; tokenIndex < sliderData.Length; tokenIndex++)
            {
                string value = sliderData[tokenIndex];
                if (TryGetPathType(value, out PathType segmentType))
                {
                    if (tokenIndex > 0)
                        pendingType = segmentType;

                    continue;
                }

                string[] split = value.Split(':');

                // A path anchor needs exactly two coordinates.
                if (split.Length != 2)
                    continue;

                if (TryParseDouble(split[0], out double anchorX) && TryParseDouble(split[1], out double anchorY))
                    points.Add(new PathControlPoint(new Vector2(anchorX, anchorY) - hitObject.Pos, pendingType));
                else
                    throw new BeatmapParsingException("Failed to parse coordinate of slider anchor.", line);

                pendingType = null;
            }

            // Older formats represent path-type boundaries as repeated anchors. Fold those markers into the path types.
            if (formatVersion < 128)
            {
                var normalized = new List<PathControlPoint>();
                PathType activeType = points[0].Type ?? firstType;

                for (int index = 0; index < points.Count; index++)
                {
                    var point = points[index];
                    if (point.Type.HasValue)
                        activeType = point.Type.Value;

                    if (index > 0 && index < points.Count - 1 && normalized[^1].Position == point.Position &&
                        !normalized[^1].Type.HasValue)
                    {
                        normalized[^1].Type = activeType;
                        continue;
                    }

                    if (index < points.Count - 1 && normalized.Count > 0 &&
                        normalized[^1].Position == point.Position)
                    {
                        point.Type ??= activeType;
                    }

                    normalized.Add(point);
                }

                points = normalized;
            }

            hitObject.ControlPoints = points;

            if (TryParseInt(values[6], out int repeat))
                hitObject.Repeat = repeat;
            else
                throw new BeatmapParsingException("Failed to parse repeat number of slider.", line);

            hitObject.PixelLength = ParseDouble(values[7], "Failed to parse pixel length of slider.", line);

            // Edge hitsounds are stored in field 8.
            hitObject.EdgeHitsounds = new List<int>(hitObject.Repeat + 1);
            if (values.Length > 8)
            {
                string[] split = values[8].Split('|');
                for (int i = 0; i < Math.Min(split.Length, hitObject.Repeat + 1); i++)
                {
                    hitObject.EdgeHitsounds.Add(
                        TryParseInt(split[i], out int edgeHitsound) ? edgeHitsound : hitsounds);
                }
            }

            for (int i = hitObject.EdgeHitsounds.Count; i < hitObject.Repeat + 1; i++)
                hitObject.EdgeHitsounds.Add(hitsounds);

            // Edge sample sets are stored in field 9.
            hitObject.EdgeSampleSets = new List<SampleSet>(hitObject.Repeat + 1);
            hitObject.EdgeAdditionSets = new List<SampleSet>(hitObject.Repeat + 1);
            if (values.Length > 9)
            {
                string[] split = values[9].Split('|');
                for (int i = 0; i < Math.Min(split.Length, hitObject.Repeat + 1); i++)
                {
                    string[] pair = split[i].Split(':');
                    hitObject.EdgeSampleSets.Add(
                        TryParseInt(pair[0], out int sampleSet) ? (SampleSet)sampleSet : SampleSet.None);
                    hitObject.EdgeAdditionSets.Add(
                        TryParseInt(pair[1], out int additionSet) ? (SampleSet)additionSet : SampleSet.None);
                }
            }

            for (int i = hitObject.EdgeSampleSets.Count; i < hitObject.Repeat + 1; i++)
                hitObject.EdgeSampleSets.Add(SampleSet.None);

            for (int i = hitObject.EdgeAdditionSets.Count; i < hitObject.Repeat + 1; i++)
                hitObject.EdgeAdditionSets.Add(SampleSet.None);

            // Slider extras are stored in field 10.
            if (values.Length > 10)
                hitObject.SetExtras(values[10]);
            else
                hitObject.SetExtras();
        }
        else if (hitObject.IsSpinner)
        {
            if (values.Length <= 5)
                throw new BeatmapParsingException("Spinner object is missing values.", line);

            hitObject.EndTime = ParseDouble(values[5], "Failed to parse end time of spinner.", line);
            hitObject.TemporalLength = hitObject.EndTime - hitObject.Time;

            // Spinner extras are stored in field 6.
            if (values.Length > 6)
                hitObject.SetExtras(values[6]);
            else
                hitObject.SetExtras();
        }
        else
        {
            // Circles and hold notes have no slider path or repeat count.
            hitObject.Repeat = 0;
            hitObject.EndTime = hitObject.Time;
            hitObject.TemporalLength = 0;

            // Circle and hold-note extras are stored in field 5.
            if (values.Length > 5)
                hitObject.SetExtras(values[5]);
            else
                hitObject.SetExtras();
        }

        return hitObject;
    }

    public static string Encode(HitObject hitObject, int formatVersion, bool? fullPrecisionOverride = null)
    {
        bool fullPrecision = fullPrecisionOverride ?? formatVersion >= 128;
        string coordinate(double value) => fullPrecision ? value.ToInvariant() : value.ToRoundInvariant();

        var values = new List<string>
        {
            coordinate(hitObject.Pos.X),
            coordinate(hitObject.Pos.Y),
            coordinate(hitObject.Time),
            hitObject.ObjectType.ToInvariant(),
            hitObject.Hitsounds.ToInvariant(),
        };

        if (hitObject.IsSlider)
        {
            var builder = new StringBuilder();
            bool mixedTypes = hitObject.ControlPoints.Any(
                point => point.Type.HasValue && point.Type != hitObject.ControlPoints[0].Type);

            if (formatVersion < 128 && (mixedTypes || hitObject.ControlPoints[0].Type == PathType.BSpline))
            {
                var bezier = BezierConverter.ConvertToBezierAnchors(hitObject.ControlPoints);
                builder.Append('B');

                for (int index = 1; index < bezier.Count; index++)
                {
                    var point = bezier[index];
                    Vector2 position = point.Position + hitObject.Pos;
                    string anchor = $"|{coordinate(position.X)}:{coordinate(position.Y)}";
                    builder.Append(anchor);

                    if (point.Type.HasValue && index < bezier.Count - 1)
                        builder.Append(anchor);
                }
            }
            else
            {
                builder.Append(GetPathTypeString(hitObject.ControlPoints.FirstOrDefault()?.Type ?? PathType.Linear));

                for (int index = 1; index < hitObject.ControlPoints.Count; index++)
                {
                    var point = hitObject.ControlPoints[index];
                    Vector2 position = point.Position + hitObject.Pos;

                    if (point.Type.HasValue && formatVersion >= 128)
                        builder.Append('|').Append(GetPathTypeString(point.Type.Value));

                    builder.Append($"|{coordinate(position.X)}:{coordinate(position.Y)}");

                    bool previousTypedAtSamePosition = hitObject.ControlPoints[index - 1].Type.HasValue &&
                        hitObject.ControlPoints[index - 1].Position == point.Position;
                    if (point.Type.HasValue && formatVersion < 128 &&
                        index < hitObject.ControlPoints.Count - 1 && !previousTypedAtSamePosition)
                    {
                        builder.Append($"|{coordinate(position.X)}:{coordinate(position.Y)}");
                    }
                }
            }

            values.Add(builder.ToString());
            values.Add(hitObject.Repeat.ToInvariant());
            values.Add(hitObject.PixelLength.ToInvariant());

            if (hitObject.SliderExtras)
            {
                // Slider extras contain edge hitsounds, edge sample sets, and the trailing sample override.
                values.Add(string.Join("|", hitObject.EdgeHitsounds.Select(value => value.ToInvariant())));

                var edgeSamples = new StringBuilder();
                for (int i = 0; i < hitObject.EdgeSampleSets.Count; i++)
                {
                    edgeSamples.Append(
                        $"|{hitObject.EdgeSampleSets[i].ToIntInvariant()}:{hitObject.EdgeAdditionSets[i].ToIntInvariant()}");
                }

                edgeSamples.Remove(0, 1);
                values.Add(edgeSamples.ToString());
                values.Add(EncodeExtras(hitObject, fullPrecision));
            }
        }
        else if (hitObject.IsSpinner)
        {
            values.Add(coordinate(hitObject.EndTime));
            values.Add(EncodeExtras(hitObject, fullPrecision));
        }
        else
        {
            // Circles and hold notes share the extras field; hold notes prepend their end time.
            values.Add(EncodeExtras(hitObject, fullPrecision));
        }

        return string.Join(",", values);
    }

    private static string EncodeExtras(HitObject value, bool fullPrecision)
    {
        if (value.IsHoldNote)
        {
            string endTime = fullPrecision ? value.EndTime.ToInvariant() : value.EndTime.ToRoundInvariant();
            return string.Join(
                ":",
                endTime,
                value.SampleSet.ToIntInvariant(),
                value.AdditionSet.ToIntInvariant(),
                value.CustomIndex.ToInvariant(),
                value.SampleVolume.ToRoundInvariant(),
                value.Filename);
        }

        return string.Join(
            ":",
            value.SampleSet.ToIntInvariant(),
            value.AdditionSet.ToIntInvariant(),
            value.CustomIndex.ToInvariant(),
            value.SampleVolume.ToRoundInvariant(),
            value.Filename);
    }

    private static double ParseDouble(string value, string error, string line)
    {
        if (TryParseDouble(value, out double result))
            return result;

        throw new BeatmapParsingException(error, line);
    }

    private static bool TryGetPathType(string token, out PathType type)
    {
        type = PathType.Catmull;
        if (token.Length == 0 || !char.IsLetter(token[0]))
            return false;

        switch (token[0])
        {
            case 'L':
                type = PathType.Linear;
                return true;
            case 'B':
                type = token.Length > 1 && int.TryParse(token[1..], out int degree) && degree > 0
                    ? PathType.BSpline
                    : PathType.Bezier;
                return true;
            case 'P':
                type = PathType.PerfectCurve;
                return true;
            case 'C':
                type = PathType.Catmull;
                return true;
            default:
                return true;
        }
    }

    private static string GetPathTypeString(PathType pathType) => pathType switch
    {
        PathType.Linear => "L",
        PathType.PerfectCurve => "P",
        PathType.Catmull => "C",
        PathType.Bezier => "B",
        PathType.BSpline => "B4",
        _ => throw new ArgumentOutOfRangeException(nameof(pathType)),
    };
}
