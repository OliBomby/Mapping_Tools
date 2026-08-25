namespace Mapping_Tools.Application.Tools.MapsetMerger.Models;

/// <summary>
///     The serializable Mapset Merger project state, retaining the former WPF
///     property names for automatic recovery and project compatibility.
/// </summary>
public class MapsetMergerServiceOptions
{
    /// <summary>Gets or sets whether the first external storyboard is copied into every beatmap.</summary>
    public bool MoveSbToBeatmap { get; set; }

    /// <summary>Gets or sets the destination directory for merged files.</summary>
    public string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the source mapsets in their merge order.</summary>
    public List<MapsetItem> Mapsets { get; set; } = [];

    /// <summary>One persisted source mapset entry.</summary>
    public sealed class MapsetItem
    {
        /// <summary>Gets or sets the output folder and reference prefix.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the source mapset directory.</summary>
        public string Path { get; set; } = string.Empty;
    }
}
