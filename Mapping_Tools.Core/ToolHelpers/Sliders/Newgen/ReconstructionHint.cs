using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;

/// <summary>
///     Preserves an original slider segment so an edited sampled path can reuse its anchors where possible.
/// </summary>
public struct ReconstructionHint
{
    /// <summary>
    ///     The start node in the point path.
    /// </summary>
    public readonly LinkedListNode<PathPoint> Start;

    /// <summary>
    ///     The end node in the point path.
    /// </summary>
    public readonly LinkedListNode<PathPoint> End;

    /// <summary>
    ///     Recalculation layer.
    ///     If this layer is higher than the overlapping hints, then this hint may replace them,
    ///     otherwise the hints must be voided.
    /// </summary>
    public readonly int Layer;

    /// <summary>
    ///     Should not be used if empty or null.
    /// </summary>
    public readonly List<Vector2> Anchors;

    /// <summary>
    ///     The path type path described by the anchors.
    /// </summary>
    public readonly PathType PathType;

    /// <summary>
    ///     Completion at which to start in the path of the anchors.
    ///     0 means no margin.
    /// </summary>
    public readonly double StartP;

    /// <summary>
    ///     Completion at which to end in the path of the anchors.
    ///     1 means use all available length.
    /// </summary>
    public readonly double EndP;

    /// <summary>
    ///     The relation [0,1] -> [0,1] between cumulative length on the curve and cumulative length on the hint path.
    ///     If null, this relation is assumed to be linear.
    /// </summary>
    public readonly Func<double, double> DistFunc;

    /// <summary>
    ///     Associates a path interval with source anchors, precedence, and completion mapping.
    /// </summary>
    /// <param name="start">The first sampled-path node covered by the hint.</param>
    /// <param name="end">The final sampled-path node covered by the hint.</param>
    /// <param name="layer">The layer.</param>
    /// <param name="anchors">Original control points to reuse, or null for a void interval.</param>
    /// <param name="pathType">The path type.</param>
    /// <param name="startP">The start p.</param>
    /// <param name="endP">The end p.</param>
    /// <param name="distFunc">The dist func.</param>
    public ReconstructionHint(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int layer, List<Vector2> anchors,
        PathType pathType = PathType.Bezier, double startP = 0, double endP = 1, Func<double, double> distFunc = null)
    {
        Start = start;
        End = end;
        Anchors = anchors;
        Layer = layer;
        PathType = pathType;
        StartP = startP;
        EndP = endP;
        DistFunc = distFunc;
    }

    /// <summary>
    ///     Returns a copy restricted to a smaller point and source-completion interval.
    /// </summary>
    /// <param name="start">The new first covered node.</param>
    /// <param name="end">The new final covered node.</param>
    /// <param name="startP">The start p.</param>
    /// <param name="endP">The end p.</param>
    /// <returns>The restricted hint with the same anchors, type, layer, and distance mapping.</returns>
    public ReconstructionHint Cut(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, double startP = 0, double endP = 1)
    {
        return new ReconstructionHint(start, end, Layer, Anchors, PathType, startP, endP, DistFunc);
    }

    /// <summary>
    ///     Returns a copy with a new curve-to-hint distance mapping.
    /// </summary>
    /// <param name="distFunc">The dist func.</param>
    /// <returns>The updated immutable hint value.</returns>
    public ReconstructionHint SetDistFunc(Func<double, double> distFunc)
    {
        return new ReconstructionHint(Start, End, Layer, Anchors, PathType, StartP, EndP, distFunc);
    }
}
