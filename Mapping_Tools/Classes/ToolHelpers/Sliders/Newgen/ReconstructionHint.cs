using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen {
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
        /// Pixel length that must be cut from the start of the path.
        /// </summary>
        public readonly double StartMargin;

        /// <summary>
        /// Pixel lenth that must be cut from the end of the path.
        /// </summary>
        public readonly double EndMargin;

        public ReconstructionHint(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, int layer, List<Vector2> anchors,
            PathType pathType = PathType.Bezier, double startMargin = 0, double endMargin = 0) {
            Start = start;
            End = end;
            Anchors = anchors;
            Layer = layer;
            PathType = pathType;
            StartMargin = startMargin;
            EndMargin = endMargin;
        }
    }
}