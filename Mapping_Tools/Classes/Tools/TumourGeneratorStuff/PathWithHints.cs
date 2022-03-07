using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    /// <summary>
    /// Its a path with reconstruction hints.
    /// </summary>
    public class PathWithHints {
        public LinkedList<PathPoint> Path { get; set; }

        /// <summary>
        /// Invariant: Non-overlapping and sorted
        /// </summary>
        public LinkedList<ReconstructionHint> ReconstructionHints { get; set; }
    }
}