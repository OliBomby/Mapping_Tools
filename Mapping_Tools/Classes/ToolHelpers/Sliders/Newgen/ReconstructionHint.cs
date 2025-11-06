using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;

public struct ReconstructionHint {
    /// <summary>
    /// The start node in the point path.
    /// </summary>
    public readonly LinkedListNode<PathPoint> Start;

    /// <summary>
    /// The end node in the point path.
    /// </summary>
    public readonly LinkedListNode<PathPoint> End;

    /// <summary>
    /// Recalculation layer.
    /// If this layer is higher than the overlapping hints, then this hint may replace them,
    /// otherwise the hints must be voided.
    /// </summary>
    public readonly int Layer;

    /// <summary>
    /// Should not be used if empty or null.
    /// </summary>
    public readonly List<Vector2> Anchors;

    /// <summary>
    /// The path type path described by the anchors.
    /// </summary>
    public readonly PathType PathType;

    /// <summary>
    /// Completion at which to start in the path of the anchors.
    /// 0 means no margin.
    /// </summary>
    public readonly double StartP;

    /// <summary>
    /// Completion at which to end in the path of the anchors.
    /// 1 means use all available length.
    /// </summary>
    public readonly double EndP;

    /// <summary>
    /// The relation [0,1] -> [0,1] between cumulative length on the curve and cumulative length on the hint path.
    /// If null, this relation is assumed to be linear.
    /// </summary>
    public readonly Func<double, double> DistFunc;

    public ReconstructionHint(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int layer, List<Vector2> anchors,
        PathType pathType = PathType.Bezier, double startP = 0, double endP = 1, Func<double, double> distFunc = null) {
        Start = start;
        End = end;
        Anchors = anchors;
        Layer = layer;
        PathType = pathType;
        StartP = startP;
        EndP = endP;
        DistFunc = distFunc;
    }

    public ReconstructionHint Cut(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, double startP = 0, double endP = 1) {
        return new ReconstructionHint(start, end, Layer, Anchors, PathType, startP, endP, DistFunc);
    }

    public ReconstructionHint SetDistFunc(Func<double, double> distFunc) {
        return new ReconstructionHint(Start, End, Layer, Anchors, PathType, StartP, EndP, distFunc);
    }
}