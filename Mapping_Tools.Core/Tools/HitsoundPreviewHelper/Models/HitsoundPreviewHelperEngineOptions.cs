using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Core.Tools.HitsoundPreviewHelper.Models;

/// <summary>
///     Stores the framework-independent settings for positional hitsound preview.
/// </summary>
public class HitsoundPreviewHelperEngineOptions
{
    /// <summary>
    ///     Gets or sets the positional rules applied to each selected timeline event.
    /// </summary>
    public List<HitsoundZone> Items { get; set; } = [];
}
