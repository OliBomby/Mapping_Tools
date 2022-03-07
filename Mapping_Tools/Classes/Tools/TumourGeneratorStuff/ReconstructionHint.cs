using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public struct ReconstructionHint {
        public LinkedListNode<PathPoint> Start;
        
        public LinkedListNode<PathPoint> End;

        public List<Vector2> Anchors;
    }
}