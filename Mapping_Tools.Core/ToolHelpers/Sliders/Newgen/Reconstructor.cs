using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;

/// <summary>
///     Reconstructs the anchors of a complete slider out of a <see cref="PathWithHints" />.
/// </summary>
public class Reconstructor
{
    /// <summary>
    ///     Gets the strategy used to synthesize Bézier anchors for intervals without reusable hints.
    /// </summary>
    public PathGenerator2 PathGenerator { get; init; } = new();

    /// <summary>
    ///     Controls whether reconstruction returns the sampled points as a linear path for inspection.
    /// </summary>
    public bool DebugConstruction { get; set; }

    /// <summary>
    ///     Reuses valid hinted segments and generates the gaps as independently typed path segments.
    /// </summary>
    /// <param name="pathWithHints">The edited sampled path and its original-segment hints.</param>
    /// <param name="preserveUnmodifiedLastSegment">Keeps an untouched truncated curved final hint intact so the caller can apply the final pixel-length constraint.</param>
    /// <returns>Reconstructed path control points in absolute coordinates.</returns>
    public List<PathControlPoint> Reconstruct(PathWithHints pathWithHints, bool preserveUnmodifiedLastSegment = false)
    {
        if (DebugConstruction)
            return pathWithHints.Path.Select((point, index) => new PathControlPoint(point.Pos, index == 0 ? PathType.Linear : null)).ToList();

        var hints = ConstructHints(pathWithHints.Path, pathWithHints.ReconstructionHints);

        var controlPoints = new List<PathControlPoint>();
        foreach (var hint in hints)
        {
            Vector2[] segmentAnchors;
            PathType segmentType = hint.ControlPoints?.FirstOrDefault()?.Type ?? PathType.Bezier;
            List<PathControlPoint> segmentPoints;

            if (hint.ControlPoints is null || hint.ControlPoints.Count == 0)
            {
                // Null segment, should have been reconstructed from points by ConstructHints...
                segmentAnchors = [hint.Start.Value.Pos, hint.End.Value.Pos];
                segmentPoints =
                [
                    new PathControlPoint(segmentAnchors[0], segmentType),
                    new PathControlPoint(segmentAnchors[1]),
                ];
            }
            else
            {
                bool keepFullLastSegment = preserveUnmodifiedLastSegment
                    && hint.ControlPoints.Count > 2
                    && segmentType != PathType.Linear
                    && hint.End == pathWithHints.Path.Last
                    && Precision.AlmostEquals(hint.StartP, 0)
                    && !Precision.AlmostEquals(hint.EndP, 1)
                    && IsUnmodified(hint.Start, hint.End);

                if (keepFullLastSegment)
                {
                    segmentPoints = hint.ControlPoints.Select(point => point.Copy()).ToList();
                    segmentType = segmentPoints[0].Type ?? PathType.Bezier;
                    segmentAnchors = segmentPoints.Select(point => point.Position).ToArray();
                }
                else
                {
                    // Cut hint anchors to completion
                    var cutAnchors = CutAnchors(hint.ControlPoints, hint.StartP, hint.EndP);
                    segmentType = cutAnchors[0].Type ?? PathType.Bezier;

                    // Add hint anchors
                    segmentAnchors = TransformAnchors(cutAnchors.Select(point => point.Position).ToList(), hint.Start.Value.Pos, hint.End.Value.Pos,
                        MathHelper.LerpAngle(hint.Start.Value.PreAngle, hint.End.Value.PostAngle, 0.5));
                    segmentPoints = cutAnchors;
                }
            }

            if (controlPoints.Count > 0 && controlPoints[^1].Position == segmentAnchors[0])
            {
                controlPoints[^1].Type = segmentType;
                for (int index = 1; index < segmentAnchors.Length; index++)
                    controlPoints.Add(new PathControlPoint(segmentAnchors[index], segmentPoints[index].Type));
            }
            else
            {
                for (int index = 0; index < segmentAnchors.Length; index++)
                    controlPoints.Add(new PathControlPoint(segmentAnchors[index], index == 0 ? segmentType : segmentPoints[index].Type));
            }
        }

        return controlPoints;
    }

    private static bool IsUnmodified(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end)
    {
        var current = start;
        while (current is not null)
        {
            if (current.Value.Pos != current.Value.OgPos) return false;
            if (current == end) return true;
            current = current.Next;
        }

        return false;
    }

    private static List<PathControlPoint> CutAnchors(List<PathControlPoint> source, double startP, double endP)
    {
        var controlPoints = source.Select(point => point.Copy()).ToList();
        PathType pathType = controlPoints[0].Type ?? PathType.Bezier;
        if (Precision.AlmostEquals(startP, 0) && Precision.AlmostEquals(endP, 1))
            return controlPoints;

        double fullLength = new SliderPath(controlPoints.ToArray()).Distance;
        double newLengthStart = (1 - startP) * fullLength;
        double newLengthEnd = (endP - startP) * fullLength;

        if (!Precision.AlmostEquals(startP, 0))
        {
            var reversed = controlPoints.Select(point => point.Copy()).ToList();
            reversed.Reverse();
            reversed[0].Type = pathType;
            reversed[^1].Type = null;
            var sliderPath = new SliderPath(reversed.ToArray(), newLengthStart);
            var moved = SliderPathUtil.MoveAnchorsToLength(sliderPath, fullLength, newLengthStart);
            PathType newPathType = moved[0].Type ?? pathType;
            controlPoints = moved;
            pathType = newPathType;
            fullLength = newLengthStart;
            controlPoints.Reverse();
            controlPoints[0].Type = pathType;
            controlPoints[^1].Type = null;
        }

        if (!Precision.AlmostEquals(endP, 1))
        {
            var sliderPath = new SliderPath(controlPoints.ToArray(), newLengthEnd);
            var moved = SliderPathUtil.MoveAnchorsToLength(sliderPath, fullLength, newLengthEnd);
            controlPoints = moved;
        }

        return controlPoints;
    }

    private static Vector2[] TransformAnchors(IReadOnlyList<Vector2> anchors, Vector2 start, Vector2 end, double theta)
    {
        var hintStartPos = anchors[0];
        var hintDir = anchors[^1] - hintStartPos;
        var segmentDir = end - start;
        Matrix2 transform;

        if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON && segmentDir.LengthSquared < Precision.DOUBLE_EPSILON)
            transform = Matrix2.CreateRotation(-theta);
        else if (hintDir.LengthSquared < Precision.DOUBLE_EPSILON)
            transform = Matrix2.CreateRotation(segmentDir.Theta);
        else if (segmentDir.LengthSquared < Precision.DOUBLE_EPSILON)
            transform = Matrix2.CreateRotation(hintDir.Theta - theta);
        else
            // Scale along the axis of hintDir
            //transform = Matrix2.CreateRotation(-segmentDir.Theta);
            //transform = Matrix2.Mult(transform, Matrix2.CreateScale(segmentDir.Length / hintDir.Length, 1));
            //transform = Matrix2.Mult(transform, Matrix2.CreateRotation(hintDir.Theta));
            transform = Matrix2.CreateRotation(hintDir.Theta - segmentDir.Theta) * (segmentDir.Length / hintDir.Length);

        // Transform all the anchors and put them into an array
        var transformedAnchors = new Vector2[anchors.Count];
        for (int i = 0; i < anchors.Count; i++)
            if (i == 0)
                transformedAnchors[i] = start;
            else if (i == anchors.Count - 1)
                transformedAnchors[i] = end;
            else
                transformedAnchors[i] = Matrix2.Mult(transform, anchors[i] - hintStartPos) + start;

        return transformedAnchors;
    }

    /// <summary>
    ///     Constructs full hints for a path.
    ///     Accurate angle and distances must be calculated on the path beforehand.
    /// </summary>
    /// <param name="path">The path to construct hints for</param>
    /// <param name="existingHints">Existing hints to be used instead of generated hints if they are more efficient.</param>
    public List<ReconstructionHint> ConstructHints(LinkedList<PathPoint> path, IReadOnlyList<ReconstructionHint>? existingHints = null)
    {
        var hints = existingHints is null
            ? new List<ReconstructionHint>()
            : new List<ReconstructionHint>(existingHints.Where(o => o.ControlPoints is not null));

        int layer = existingHints is null || existingHints.Count == 0 ? 0 : existingHints.Max(hint => hint.Layer) + 1;
        var current = path.First;
        int nextHint = 0;
        ReconstructionHint? currentHint = null;
        LinkedListNode<PathPoint>? segmentStart = null;

        // Loop through path and find all segments which are either an existing hint or a gap between two hints
        // Also split on all red points
        while (current is not null)
        {
            if (segmentStart is not null
                && (
                    currentHint.HasValue && current == currentHint.Value.End
                    || nextHint < hints.Count && current == hints[nextHint].Start
                    || !currentHint.HasValue && current.Value.Red
                    || current.Next is null))
            {
                // End of hint and hint active or its the start of hint and there is no hint active
                // Create between start and this
                var segmentEnd = current;

                // Construct a hint
                var constructedHint = ConstructHint(segmentStart, segmentEnd, layer);

                if (currentHint is { } activeHint)
                {
                    // Keep the better hint
                    if (activeHint.ControlPoints is not null
                        && constructedHint.ControlPoints is not null
                        && activeHint.ControlPoints.Count > constructedHint.ControlPoints.Count)
                        hints[nextHint - 1] = constructedHint;
                }
                else
                {
                    // Insert the constructed hint
                    hints.Insert(nextHint++, constructedHint);
                }

                currentHint = null;
                segmentStart = null;
            }

            if (segmentStart is null)
            {
                segmentStart = current;

                if (nextHint < hints.Count && current == hints[nextHint].Start) currentHint = hints[nextHint++];
            }

            current = current.Next;
        }

        return hints;
    }

    private ReconstructionHint ConstructHint(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int layer)
    {
        var controlPoints = PathGenerator.GeneratePath(start, end);
        return new ReconstructionHint(start, end, layer, controlPoints);
    }
}
