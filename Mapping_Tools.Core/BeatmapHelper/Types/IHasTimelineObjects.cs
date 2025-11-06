using System.Collections.Generic;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;

namespace Mapping_Tools.Core.BeatmapHelper.Types;

public interface IHasTimelineObjects {
    /// <summary>
    /// Generates timeline objects associated with this object.
    /// </summary>
    /// <returns>The timeline objects.</returns>
    IEnumerable<TimelineObject> GetTimelineObjects();
}