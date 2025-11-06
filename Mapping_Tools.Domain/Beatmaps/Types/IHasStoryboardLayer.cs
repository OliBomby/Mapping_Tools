using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Types;

/// <summary>
/// Indicates that a type has a storyboard layer.
/// </summary>
public interface IHasStoryboardLayer {
    /// <summary>
    /// The storyboard layer this object belongs to.
    /// </summary>
    StoryboardLayer Layer { get; set; }
}