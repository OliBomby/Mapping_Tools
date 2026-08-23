using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.MetadataManager;

/// <summary>
///     Stores the metadata, preview, and colour choices applied by Metadata Manager.
///     File paths are retained here as part of the legacy project contract; the
///     application service interprets them only when it executes an operation.
/// </summary>
public class MetadataManagerOptions
{
    /// <summary>Gets or sets the optional beatmap used to import metadata.</summary>
    public string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the vertical-bar-separated target beatmap paths.</summary>
    public string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the Unicode artist name.</summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>Gets or sets the ASCII artist name used in filenames.</summary>
    public string RomanisedArtist { get; set; } = string.Empty;

    /// <summary>Gets or sets the Unicode title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the ASCII title used in filenames.</summary>
    public string RomanisedTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the mapper name.</summary>
    public string BeatmapCreator { get; set; } = string.Empty;

    /// <summary>Gets or sets the source text recorded in the beatmap.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the space-separated beatmap tags.</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>Gets or sets whether duplicate tags are removed before export.</summary>
    public bool DoRemoveDuplicateTags { get; set; } = true;

    /// <summary>Gets or sets whether online beatmap and mapset IDs are reset.</summary>
    public bool ResetIds { get; set; }

    /// <summary>Gets or sets the preview timestamp in milliseconds.</summary>
    public double PreviewTime { get; set; }

    /// <summary>Gets or sets whether the configured combo and special colours are exported.</summary>
    public bool UseComboColours { get; set; } = true;

    /// <summary>Gets or sets the ordered combo-colour palette.</summary>
    public List<ComboColour> ComboColours { get; set; } = [];

    /// <summary>Gets or sets the named special colours.</summary>
    public List<SpecialColour> SpecialColours { get; set; } = [];
}
