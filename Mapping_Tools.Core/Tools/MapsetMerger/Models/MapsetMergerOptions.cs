namespace Mapping_Tools.Core.Tools.MapsetMerger.Models;

/// <summary>
///     Describes the non-visual option that changes how storyboard content is
///     represented in merged beatmaps.
/// </summary>
public class MapsetMergerOptions
{
    /// <summary>
    ///     Gets or sets whether the first external storyboard is copied into every
    ///     beatmap instead of being emitted as a separate <c>.osb</c> file.
    /// </summary>
    public bool MoveSbToBeatmap { get; set; }
}

