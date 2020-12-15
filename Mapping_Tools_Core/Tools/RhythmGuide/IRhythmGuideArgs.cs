using System.Collections.Generic;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools_Core.Tools.RhythmGuide {
    public interface IRhythmGuideArgs {
        /// <summary>
        /// The beatmaps to import from.
        /// </summary>
        IEnumerable<Beatmap> InputBeatmaps { get; }

        /// <summary>
        /// If each object should have a new combo.
        /// </summary>
        bool NcEverything { get; }

        /// <summary>
        /// Selection for which objects to put in the rhythm guide.
        /// </summary>
        SelectionMode SelectionMode { get; }

        /// <summary>
        /// The beat divisors for resnapping in the rhythm guide.
        /// </summary>
        IBeatDivisor[] BeatDivisors { get; }
    }
}