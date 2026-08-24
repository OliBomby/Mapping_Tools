using Mapping_Tools.Core.Tools.MapsetMerger.Models;

namespace Mapping_Tools.Application.Tools.MapsetMerger.Models;

/// <summary>
///     The serializable Mapset Merger project state, retaining the former WPF
///     property names for automatic recovery and project compatibility.
/// </summary>
public sealed class MapsetMergerProject : MapsetMergerOptions
{
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

