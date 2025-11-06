using System.Collections.Generic;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.RhythmGuide;

public class RhythmGuideArgs : IRhythmGuideArgs {
    public IEnumerable<Beatmap> InputBeatmaps { get; set; }
    public bool NcEverything { get; set; }
    public SelectionMode SelectionMode { get; set; }
    public IBeatDivisor[] BeatDivisors { get; set; }
}