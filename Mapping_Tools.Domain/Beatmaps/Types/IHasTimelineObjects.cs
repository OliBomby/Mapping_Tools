using Mapping_Tools.Domain.Beatmaps.TimelineStuff;

namespace Mapping_Tools.Domain.Beatmaps.Types;

public interface IHasTimelineObjects {
    /// <summary>
    /// Generates timeline objects associated with this object.
    /// </summary>
    /// <returns>The timeline objects.</returns>
    IEnumerable<TimelineObject> GetTimelineObjects();
}