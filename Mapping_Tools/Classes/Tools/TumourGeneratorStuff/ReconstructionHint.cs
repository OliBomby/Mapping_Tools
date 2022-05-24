using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public struct ReconstructionHint {
        public LinkedListNode<PathPoint> Start;
        
        public LinkedListNode<PathPoint> End;

        /// <summary>
        /// Should not be used if empty or null.
        /// </summary>
        public List<Vector2> Anchors;

        /// <summary>
        /// Recalculation layer.
        /// If this layer is higher than the overlapping hints, then this hint may replace them,
        /// otherwise the hints must be voided.
        /// </summary>
        public int Layer;

        public ReconstructionHint(LinkedListNode<PathPoint> start, LinkedListNode<PathPoint> end, List<Vector2> anchors, int layer) {
            Start = start;
            End = end;
            Anchors = anchors;
            Layer = layer;
        }
    }
}