using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;

/// <summary>
///     A slider anchor. A non-null type starts a new path segment at this anchor.
/// </summary>
public sealed class PathControlPoint
{
    /// <summary>The position in the containing path's coordinate system.</summary>
    public Vector2 Position { get; set; }

    /// <summary>The type of the segment starting here, or null to continue the previous segment.</summary>
    public PathType? Type { get; set; }

    /// <summary>Creates a slider path control point.</summary>
    /// <param name="position">Position in the containing path's coordinate system.</param>
    /// <param name="type">Type of a new segment, when this point starts one.</param>
    public PathControlPoint(Vector2 position, PathType? type = null)
    {
        Position = position;
        Type = type;
    }

    /// <summary>Creates an independent copy of this control point.</summary>
    /// <returns>The copied point.</returns>
    public PathControlPoint Copy() => new(Position, Type);

    /// <summary>Parses legacy absolute anchors, replacing repeated segment boundaries with typed points.</summary>
    /// <param name="positions">Absolute positions including the first anchor.</param>
    /// <param name="origin">The slider position used to normalize the anchors.</param>
    /// <param name="type">The legacy path type shared by all segments.</param>
    /// <returns>Relative control points with explicit segment types.</returns>
    public static List<PathControlPoint> FromLegacyPositions(IReadOnlyList<Vector2> positions, Vector2 origin, PathType type)
    {
        var result = new List<PathControlPoint>();
        for (int index = 0; index < positions.Count; index++)
        {
            var position = positions[index] - origin;
            if (index > 0 && index < positions.Count - 1 && result[^1].Position == position &&
                !result[^1].Type.HasValue)
            {
                result[^1].Type = type;
                continue;
            }

            result.Add(new PathControlPoint(position,
                index == 0 || index < positions.Count - 1 && result.Count > 0 && result[^1].Position == position
                    ? type : null));
        }

        return result;
    }
}
