using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools_Core.Tools.RhythmGuide {
    public class RhythmGuideArgs : IRhythmGuideArgs {
        public IEnumerable<Beatmap> InputBeatmaps { get; set; }
        public bool NcEverything { get; set; }
        public SelectionMode SelectionMode { get; set; }
        public IBeatDivisor[] BeatDivisors { get; set; }
    }
}